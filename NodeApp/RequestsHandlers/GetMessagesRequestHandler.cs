/** 
  *    This file is part of Y messenger.
  *
  *    Y messenger is free software: you can redistribute it and/or modify
  *    it under the terms of the GNU Affero Public License as published by
  *    the Free Software Foundation, either version 3 of the License, or
  *    (at your option) any later version.
  *
  *    Y messenger is distributed in the hope that it will be useful,
  *    but WITHOUT ANY WARRANTY; without even the implied warranty of
  *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  *    GNU Affero Public License for more details.
  *
  *    You should have received a copy of the GNU Affero Public License
  *    along with Y messenger.  If not, see <https://www.gnu.org/licenses/>.
  */
using NodeApp.Converters;
using NodeApp.CacheStorageClasses;
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetMessagesRequestHandler : IRequestHandler
    {
        private readonly GetMessagesRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IConnectionsService connectionsService;
        private readonly ILoadMessagesService loadMessagesService;
        private readonly ICreateMessagesService createMessagesService;
        private readonly IAttachmentsService attachmentsService;
        private readonly ILoadChatsService loadChatsService;
        private readonly IConversationsService conversationsService;
        private readonly INodeRequestSender nodeRequestSender;
        private const byte LIMIT = 30;
        public GetMessagesRequestHandler(Request request,
                                         ClientConnection clientConnection,
                                         IConnectionsService connectionsService,
                                         ILoadMessagesService loadMessagesService,
                                         ICreateMessagesService createMessagesService,
                                         IAttachmentsService attachmentsService,
                                         ILoadChatsService loadChatsService,
                                         IConversationsService conversationsService,
                                         INodeRequestSender nodeRequestSender)
        {
            this.request = (GetMessagesRequest)request;
            this.clientConnection = clientConnection;
            this.connectionsService = connectionsService;
            this.loadMessagesService = loadMessagesService;
            this.createMessagesService = createMessagesService;
            this.attachmentsService = attachmentsService;
            this.loadChatsService = loadChatsService;
            this.conversationsService = conversationsService;
            this.nodeRequestSender = nodeRequestSender;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                IEnumerable<MessageDto> result = default;
                switch (request.ConversationType)
                {
                    case ConversationType.Dialog:
                        {
                            result = await GetDialogMessagesAsync().ConfigureAwait(false);
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            result = await GetChatMessagesAsync().ConfigureAwait(false);
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            result = await GetChannelMessagesAsync().ConfigureAwait(false);
                        }
                        break;
                }
                if (result.Count() < LIMIT && request.MessagesId.IsNullOrEmpty() && request.AttachmentsTypes.IsNullOrEmpty())
                {
                    long nodeId = await conversationsService.GetConversationNodeIdAsync(request.ConversationType, request.ConversationId).ConfigureAwait(false);
                    NodeConnection connection = connectionsService.GetNodeConnection(nodeId);
                    var otherNodeMessages = await nodeRequestSender.GetMessagesAsync(
                        connection,
                        request.ConversationId,
                        request.ConversationType,
                        request.NavigationMessageId,
                        request.AttachmentsTypes,
                        request.Direction.GetValueOrDefault(),
                        LIMIT).ConfigureAwait(false);
                    if (otherNodeMessages.Where(message => !message.Deleted).Count() > result.Count())
                    {
                        await MetricsHelper.Instance.SetCrossNodeApiInvolvedAsync(request.RequestId).ConfigureAwait(false);
                        var messagesVm = MessageConverter.GetMessagesVm(otherNodeMessages, clientConnection.UserId);
                        foreach (var message in messagesVm)
                        {
                            if (message.Attachments != null)
                            {
                                await attachmentsService.DownloadAttachmentsPayloadAsync(message.Attachments, connection).ConfigureAwait(false);
                                bool isValid = await attachmentsService.ValidateAttachmentsAsync(message.Attachments, message, clientConnection.UserId, true).ConfigureAwait(false);
                            }
                        }
                        var savedMessages = await createMessagesService.SaveMessagesAsync(otherNodeMessages, clientConnection.UserId.Value).ConfigureAwait(false);
                        result = savedMessages;
                        if (savedMessages.Any())
                        {
                            var localMessage = await loadMessagesService.GetLastValidConversationMessage(request.ConversationType, request.ConversationId).ConfigureAwait(false);
                            var lastMessage = savedMessages.OrderByDescending(opt => opt.SendingTime).FirstOrDefault();
                            if (localMessage == null || localMessage.SendingTime <= lastMessage.SendingTime)
                            {
                                await UsersConversationsCacheService.Instance
                                    .NewMessageUpdateUsersConversationsAsync(MessageConverter.GetMessageVm(lastMessage, clientConnection.UserId)).ConfigureAwait(false);
                            }
                        }
                    }
                }
                return new MessagesResponse(request.RequestId, MessageConverter.GetMessagesVm(result, clientConnection.UserId.GetValueOrDefault()));
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "An error ocurred while getting messages.", ErrorCode.GetMessagesProblem);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
            {
                throw new UnauthorizedUserException();
            }

            if (!clientConnection.Confirmed)
            {
                throw new PermissionDeniedException("User is not confirmed.");
            }

            return request.ConversationId != 0;
        }

        private async Task<IEnumerable<MessageDto>> GetDialogMessagesAsync()
        {
            if (request.MessagesId == null)
            {
                return await loadMessagesService.GetDialogMessagesAsync(
                    request.ConversationId,
                    clientConnection.UserId.GetValueOrDefault(),
                    request.NavigationMessageId,
                    request.AttachmentsTypes,
                    request.Direction.GetValueOrDefault(true),
                    LIMIT).ConfigureAwait(false);
            }
            else
            {
                return await loadMessagesService.GetMessagesByIdAsync(
                    request.MessagesId,
                    ConversationType.Dialog,
                    request.ConversationId,
                    clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<MessageDto>> GetChatMessagesAsync()
        {
            List<MessageDto> messages = new List<MessageDto>();
            ChatUserDto currentChatUser = (await loadChatsService.GetChatUsersAsync(new long[] { clientConnection.UserId.GetValueOrDefault() }, request.ConversationId).ConfigureAwait(false)).FirstOrDefault();
            if (request.MessagesId == null)
            {
                messages = await loadMessagesService.GetChatMessagesAsync(
                    request.ConversationId,
                    clientConnection.UserId.GetValueOrDefault(),
                    request.NavigationMessageId,
                    request.AttachmentsTypes,
                    request.Direction.GetValueOrDefault(true),
                    LIMIT).ConfigureAwait(false);
            }
            else
            {
                messages = await loadMessagesService.GetMessagesByIdAsync(
                    request.MessagesId,
                    ConversationType.Chat,
                    request.ConversationId,
                    clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            }
            if (currentChatUser.LastReadedGlobalMessageId != null)
            {
                var lastMessage = (await loadMessagesService.GetMessagesByIdAsync(
                    new List<Guid> { currentChatUser.LastReadedGlobalMessageId.GetValueOrDefault() },
                    ConversationType.Chat,
                    request.ConversationId,
                    clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false)).FirstOrDefault();
                if (lastMessage != null)
                {
                    messages.ForEach(message =>
                    {
                        if (message.SenderId != clientConnection.UserId.GetValueOrDefault())
                        {
                            if (message.SendingTime > lastMessage.SendingTime)
                            {
                                message.Read = false;
                            }
                            else
                            {
                                message.Read = true;
                            }
                        }
                    });
                }
            }
            return messages;
        }

        private async Task<IEnumerable<MessageDto>> GetChannelMessagesAsync()
        {
            List<MessageDto> messages = new List<MessageDto>();
            if (request.MessagesId == null || !request.MessagesId.Any())
            {
                messages = await loadMessagesService.GetChannelMessagesAsync(
                    request.ConversationId,
                    clientConnection.UserId.GetValueOrDefault(),
                    request.NavigationMessageId,
                    request.AttachmentsTypes,
                    request.Direction.GetValueOrDefault(true),
                    LIMIT).ConfigureAwait(false);
            }
            else
            {
                messages = (await loadMessagesService.GetMessagesByIdAsync(
                    request.MessagesId,
                    ConversationType.Channel,
                    request.ConversationId,
                    clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false)).ToList();
            }
            messages.ForEach(message => message.SenderId = null);
            return messages;
        }
    }
}
