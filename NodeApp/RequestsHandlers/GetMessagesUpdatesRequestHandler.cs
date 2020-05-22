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
    public class GetMessagesUpdatesRequestHandler : IRequestHandler
    {
        private readonly GetMessagesUpdatesRequest request;
        private readonly ClientConnection clientConnection;
        private readonly ILoadMessagesService loadMessagesService;
        private const short UPDATES_LIMIT = 1000;

        public GetMessagesUpdatesRequestHandler(Request request, ClientConnection clientConnection, ILoadMessagesService loadMessagesService)
        {
            this.request = (GetMessagesUpdatesRequest)request;
            this.clientConnection = clientConnection;
            this.loadMessagesService = loadMessagesService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<MessageDto> updatedMessages = await loadMessagesService.GetUserUpdatedMessagesAsync(
                clientConnection.UserId.GetValueOrDefault(),
                request.Start.GetValueOrDefault(),
                request.ConversationId,
                request.ConversationType,
                request.MessageId)
                .ConfigureAwait(false);
            if (!updatedMessages.Any())
            {
                return new UpdatedMessagesResponse(request.RequestId, null, null, null, null, null, Array.Empty<MessageInfo>(), null);
            }
            IEnumerable<IGrouping<ConversationType, MessageDto>> groupingMessages;
            List<MessageInfo> messageInfo = new List<MessageInfo>();
            List<MessageVm> editedMessages = new List<MessageVm>();
            long startUpdatedTime = updatedMessages.FirstOrDefault().UpdatedAt.GetValueOrDefault();
            long? endUpdatedTime = null;
            long? endConversationId = null;
            Guid? endMessageId = null;
            ConversationType? endConversationType = null;
            if (updatedMessages.Count() < UPDATES_LIMIT)
            {
                groupingMessages = updatedMessages.GroupBy(opt => opt.ConversationType);
            }
            else
            {
                List<MessageDto> partOfMessages = updatedMessages.Take(UPDATES_LIMIT).ToList();
                MessageDto lastMessage = partOfMessages.LastOrDefault();
                groupingMessages = partOfMessages.GroupBy(opt => opt.ConversationType);
                endUpdatedTime = lastMessage.UpdatedAt;
                endConversationId = lastMessage.ConversationId;
                endConversationType = lastMessage.ConversationType;
                endMessageId = lastMessage.GlobalId;
            }
            foreach (var group in groupingMessages)
            {
                var conversationGroupingMessages = group.GroupBy(opt => opt.ConversationId);
                foreach (var messages in conversationGroupingMessages)
                {
                    var deletedMessagesId = messages.Where(opt => opt.Deleted)?.Select(opt => opt.GlobalId);
                    if (deletedMessagesId != null && deletedMessagesId.Any())
                    {
                        messageInfo.Add(
                            new MessageInfo(
                                messages.Key,
                                group.Key,
                                deletedMessagesId));
                    }
                    var edited = MessageConverter.GetMessagesVm(
                        messages.Where(opt => !opt.Deleted),
                        clientConnection.UserId.GetValueOrDefault());
                    if (edited != null && edited.Any())
                    {
                        editedMessages.AddRange(edited);
                    }
                }
            }
            return new UpdatedMessagesResponse(request.RequestId, startUpdatedTime, endUpdatedTime, endConversationId, endConversationType, endMessageId, messageInfo, editedMessages);
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

            return (request.MessageId != null && request.ConversationId != null && request.ConversationType != null)
                || (request.MessageId == null && request.ConversationId == null && request.ConversationType == null);
        }
    }
}