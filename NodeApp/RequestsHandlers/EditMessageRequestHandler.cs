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
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    class EditMessageRequestHandler : IRequestHandler
    {
        private readonly EditMessageRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IUpdateMessagesService updateMessagesService;
        private readonly IAttachmentsService attachmentsService;

        public EditMessageRequestHandler(
            Request request,
            ClientConnection clientConnection,
            INodeNoticeService nodeNoticeService,
            IConversationsNoticeService conversationsNoticeService,
            IUpdateMessagesService updateMessagesService,
            IAttachmentsService attachmentsService)
        {
            this.request = (EditMessageRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.updateMessagesService = updateMessagesService;
            this.attachmentsService = attachmentsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                if (request.Message.Attachments != null && request.Message.Attachments.Any())
                {
                    var isValid = await attachmentsService.CheckEditedMessageAttachmentsAsync(
                        request.Message, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                    if (!isValid)
                    {
                        throw new InvalidAttachmentsException();
                    }
                }
                MessageDto edited = await updateMessagesService.EditMessageAsync(
                    MessageConverter.GetMessageDto(request.Message),
                    clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                conversationsNoticeService.SendMessagesUpdatedNoticeAsync(
                    request.Message.ConversationId.GetValueOrDefault(),
                    request.Message.ConversationType,
                    new List<MessageDto> { edited },
                    clientConnection.UserId.GetValueOrDefault(),
                    false,
                    clientConnection);
                nodeNoticeService.SendMessagesUpdateNodeNoticeAsync(
                    new List<MessageDto> { edited },
                    edited.ConversationId,
                    edited.ConversationType,
                    clientConnection.UserId.GetValueOrDefault());
                UsersConversationsCacheService.Instance.MessageEditedUpdateConversations(edited);
                return new MessagesResponse(request.RequestId,
                    new List<MessageVm>
                    {
                        MessageConverter.GetMessageVm(edited, clientConnection.UserId)
                    });
            }
            catch (InvalidAttachmentsException ex)
            {
                Logger.WriteLog(ex);
                return new ResultResponse(request.RequestId, "Invalid attachments.", ErrorCode.InvalidAttachment);
            }
            catch (ObjectDoesNotExistsException ex)
            {
                Logger.WriteLog(ex);
                return new ResultResponse(request.RequestId, "Message not found.", ErrorCode.ObjectDoesNotExists);
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

            if (request.Message == null)
            {
                return false;
            }

            if ((request.Message.ConversationType == ConversationType.Dialog || request.Message.ConversationType == ConversationType.Chat)
                && request.Message.SenderId != clientConnection.UserId)
            {
                return false;
            }
            return !string.IsNullOrWhiteSpace(request.Message.Text) || request.Message.Attachments != null && request.Message.Attachments.Any();
        }
    }
}