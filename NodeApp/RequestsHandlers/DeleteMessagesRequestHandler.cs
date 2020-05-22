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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class DeleteMessagesRequestHandler : IRequestHandler
    {
        private readonly DeleteMessagesRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IDeleteMessagesService deleteMessagesService;
        private readonly ILoadChatsService loadChatsService;
        private readonly IPendingMessagesService pendingMessagesService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly ILoadDialogsService loadDialogsService;
        public DeleteMessagesRequestHandler(
            Request request,
            ClientConnection clientConnection,
            INodeNoticeService nodeNoticeService,
            IConversationsNoticeService conversationsNoticeService,
            IDeleteMessagesService deleteMessagesService,
            ILoadChatsService loadChatsService,
            IPendingMessagesService pendingMessagesService,
            ILoadChannelsService loadChannelsService,
            ILoadDialogsService loadDialogsService)
        {
            this.request = (DeleteMessagesRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.deleteMessagesService = deleteMessagesService;
            this.loadChatsService = loadChatsService;
            this.pendingMessagesService = pendingMessagesService;
            this.loadChannelsService = loadChannelsService;
            this.loadDialogsService = loadDialogsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                List<MessageDto> deletedMessages = null;
                switch (request.ConversationType)
                {
                    case ConversationType.Dialog:
                        {
                            deletedMessages = new List<MessageDto>(await DeleteDialogMessagesAsync().ConfigureAwait(false));
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            deletedMessages = new List<MessageDto>(await DeleteChatMessagesAsync().ConfigureAwait(false));
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            deletedMessages = new List<MessageDto>(await DeleteChannelMessagesAsync().ConfigureAwait(false));
                        }
                        break;
                }
                if (deletedMessages != null)
                {
                    nodeNoticeService.SendMessagesDeletedNodeNoticeAsync(
                        request.ConversationId,
                        request.ConversationType,
                        MessageConverter.GetMessagesVm(deletedMessages.ToList(), clientConnection.UserId.GetValueOrDefault()),
                        clientConnection.UserId.GetValueOrDefault());
                    MessageInfo messageInfo = new MessageInfo(
                        request.ConversationId,
                        request.ConversationType,
                        deletedMessages.Select(opt => opt.GlobalId).Distinct());
                    return new UpdatedMessagesResponse(request.RequestId, null, null, null, null, null, messageInfo, null);
                }
                else
                {
                    return new UpdatedMessagesResponse(request.RequestId, null, null, null, null, null, Array.Empty<MessageInfo>(), null);
                }
            }
            catch (DeleteMessagesException ex)
            {
                Logger.WriteLog(ex, request);
                var isRemoved = await pendingMessagesService.RemovePendingMessageByMessagesIds(request.MessagesIds).ConfigureAwait(false);
                if (isRemoved)
                {
                    MessageInfo messageInfo = new MessageInfo(request.ConversationId, request.ConversationType, request.MessagesIds);
                    return new UpdatedMessagesResponse(request.RequestId, null, null, null, null, null, messageInfo, null);
                }
                return new ResultResponse(request.RequestId, "Messages not found or user does not have access to messages.", ErrorCode.DeleteMessagesProblem);
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

            return request.ConversationId != 0 && request.MessagesIds != null && request.MessagesIds.Any();
        }

        private async Task<IEnumerable<MessageDto>> DeleteChatMessagesAsync()
        {
            List<MessageDto> deletedMessages = await deleteMessagesService.DeleteMessagesInfoAsync(
                request.ConversationId,
                ConversationType.Chat,
                request.MessagesIds,
                clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            conversationsNoticeService.SendMessagesUpdatedNoticeAsync(
                request.ConversationId,
                ConversationType.Chat,
                deletedMessages,
                clientConnection.UserId.GetValueOrDefault(),
                true,
                clientConnection);
            UsersConversationsCacheService.Instance.UpdateUsersChatsAsync(
                await loadChatsService.GetChatUsersIdAsync(request.ConversationId).ConfigureAwait(false));
            return deletedMessages;
        }

        private async Task<IEnumerable<MessageDto>> DeleteDialogMessagesAsync()
        {
            List<MessageDto> deletedMessages = await deleteMessagesService.DeleteMessagesInfoAsync(
                    request.ConversationId,
                    ConversationType.Dialog,
                    request.MessagesIds,
                    clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            List<UserVm> dialogUsers = await loadDialogsService.GetDialogUsersAsync(request.ConversationId).ConfigureAwait(false);
            conversationsNoticeService.SendMessagesUpdatedNoticeAsync(
                request.ConversationId,
                ConversationType.Dialog,
                deletedMessages,
                clientConnection.UserId.GetValueOrDefault(),
                true,
                clientConnection);
            if (dialogUsers.Count() == 2)
            {
                UsersConversationsCacheService.Instance.UpdateUsersDialogsAsync(
                    dialogUsers.ElementAt(0).Id.GetValueOrDefault(),
                    dialogUsers.ElementAt(1).Id.GetValueOrDefault());
            }
            else
            {
                UsersConversationsCacheService.Instance.UpdateUsersDialogsAsync(
                    dialogUsers.ElementAt(0).Id.GetValueOrDefault(),
                    dialogUsers.ElementAt(0).Id.GetValueOrDefault());
            }

            return deletedMessages;
        }

        private async Task<IEnumerable<MessageDto>> DeleteChannelMessagesAsync()
        {
            List<MessageDto> deletedMessages = await deleteMessagesService.DeleteMessagesInfoAsync(
                request.ConversationId,
                ConversationType.Channel,
                request.MessagesIds,
                clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            conversationsNoticeService.SendMessagesUpdatedNoticeAsync(
                request.ConversationId,
                ConversationType.Channel,
                deletedMessages,
                clientConnection.UserId.GetValueOrDefault(),
                true,
                clientConnection);
            UsersConversationsCacheService.Instance.UpdateUsersChannelsAsync(
                await loadChannelsService.GetChannelUsersIdAsync(request.ConversationId).ConfigureAwait(false));
            return deletedMessages;
        }
    }
}