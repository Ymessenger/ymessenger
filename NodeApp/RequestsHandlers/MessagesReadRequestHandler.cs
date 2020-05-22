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
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Converters;
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
    public class MessagesReadRequestHandler : IRequestHandler
    {
        private readonly MessagesReadRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IUpdateMessagesService updateMessagesService;
        private readonly ILoadDialogsService loadDialogsService;

        public MessagesReadRequestHandler(
            Request request,
            ClientConnection clientConnection,
            INodeNoticeService nodeNoticeService,
            IConversationsNoticeService conversationsNoticeService,
            IUpdateMessagesService updateMessagesService,
            ILoadDialogsService loadDialogsService)
        {
            this.request = (MessagesReadRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.updateMessagesService = updateMessagesService;
            this.loadDialogsService = loadDialogsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                switch (request.ConversationType)
                {
                    case ConversationType.Dialog:
                        {
                            return await DialogMessagesReadAsync().ConfigureAwait(false);
                        }
                    case ConversationType.Chat:
                        {
                            return await ChatMessagesReadAsync().ConfigureAwait(false);
                        }
                    case ConversationType.Channel:
                        {
                            return await ChannelMessagesReadAsync().ConfigureAwait(false);
                        }
                    default:
                        {
                            var errorObject = new
                            {
                                ConversationType = "Unknown conversation type."
                            };
                            return new ResultResponse(request.RequestId, ObjectSerializer.ObjectToJson(errorObject), ErrorCode.WrongArgumentError);
                        }
                }
            }
            catch (WrongArgumentException)
            {
                return new ResultResponse(request.RequestId, "Error marking messages as read.", ErrorCode.ReadMessageProblem);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Error marking messages as read.", ErrorCode.ReadMessageProblem);
            }
        }
        private async Task<Response> DialogMessagesReadAsync()
        {
            List<MessageDto> readedMessages = await updateMessagesService.DialogMessagesReadAsync(
                    request.MessagesId, request.ConversationId, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            if (readedMessages != null && readedMessages.Any())
            {
                List<UserVm> users = new List<UserVm>(await loadDialogsService.GetDialogUsersAsync(request.ConversationId).ConfigureAwait(false));
                conversationsNoticeService.SendMessagesReadedNoticeAsync(
                    readedMessages,
                    readedMessages.FirstOrDefault(message => message.ConversationId != request.ConversationId)?.ConversationId ?? request.ConversationId,
                    ConversationType.Dialog,
                    readedMessages.FirstOrDefault().SenderId.GetValueOrDefault());
                nodeNoticeService.SendDialogMessagesReadNoticeAsync(
                    request.MessagesId.ToList(),
                    users.FirstOrDefault(opt => opt.Id == clientConnection.UserId.GetValueOrDefault()),
                    users.FirstOrDefault(opt => opt.Id != clientConnection.UserId.GetValueOrDefault()));
                UsersConversationsCacheService.Instance.MessagesReadedUpdateConversations(
                    MessageConverter.GetMessagesVm(readedMessages, clientConnection.UserId.GetValueOrDefault()),
                    request.ConversationId,
                    ConversationType.Dialog);
            }
            return new ResultResponse(request.RequestId);
        }

        private async Task<Response> ChatMessagesReadAsync()
        {
            List<MessageDto> messages = await updateMessagesService.SetMessagesReadAsync(
                    request.MessagesId,
                    request.ConversationId,
                    ConversationType.Chat,
                    clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            conversationsNoticeService.SendMessagesReadedNoticeAsync(messages, request.ConversationId, ConversationType.Chat, clientConnection.UserId.GetValueOrDefault());
            nodeNoticeService.SendChatMessagesReadNodeNoticeAsync(messages, request.ConversationId, clientConnection.UserId.GetValueOrDefault());
            UsersConversationsCacheService.Instance.MessagesReadedUpdateConversations(
                MessageConverter.GetMessagesVm(messages, clientConnection.UserId.GetValueOrDefault()),
                request.ConversationId,
                ConversationType.Chat);
            return new ResultResponse(request.RequestId);
        }

        private async Task<Response> ChannelMessagesReadAsync()
        {
            List<MessageDto> readedMessages = await updateMessagesService.SetMessagesReadAsync(
                    request.MessagesId,
                    request.ConversationId,
                    ConversationType.Channel,
                    clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            conversationsNoticeService.SendMessagesReadedNoticeAsync(
                readedMessages, request.ConversationId, ConversationType.Channel, clientConnection.UserId.GetValueOrDefault(), clientConnection);
            UsersConversationsCacheService.Instance.MessagesReadedUpdateConversations(
                MessageConverter.GetMessagesVm(readedMessages, clientConnection.UserId),
                request.ConversationId,
                ConversationType.Channel);
            return new ResultResponse(request.RequestId);
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

            if (request.ConversationId == 0)
            {
                return false;
            }

            if (request.MessagesId == null || !request.MessagesId.Any() || request.MessagesId.Count() > 100)
            {
                return false;
            }

            return true;
        }
    }
}