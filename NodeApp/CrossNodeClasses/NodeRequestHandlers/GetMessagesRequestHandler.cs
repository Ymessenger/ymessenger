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
using NodeApp.CrossNodeClasses.Requests;
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeRequestHandlers
{
    public class GetMessagesRequestHandler : ICommunicationHandler
    {
        private readonly GetMessagesNodeRequest request;
        private readonly NodeConnection current;
        private readonly ILoadMessagesService loadMessagesService;
        private readonly IConversationsService conversationsService;

        public GetMessagesRequestHandler(NodeRequest request, NodeConnection current, ILoadMessagesService loadMessagesService, IConversationsService conversationsService)
        {
            this.request = (GetMessagesNodeRequest)request;
            this.current = current;
            this.loadMessagesService = loadMessagesService;
            this.conversationsService = conversationsService;
        }

        public async Task HandleAsync()
        {
            try
            {
                List<long> nodesIds = await conversationsService.GetConversationNodesIdsAsync(request.ConversationType, request.ConversationId).ConfigureAwait(false);
                if (!nodesIds.Contains(current.Node.Id))
                {
                    NodeWebSocketCommunicationManager.SendResponse(new ResultNodeResponse(request.RequestId, ErrorCode.PermissionDenied, "No rights to load messages."), current);
                    return;
                }
                List<MessageDto> messages = await loadMessagesService.GetMessagesAsync(
                    request.ConversationId,
                    request.ConversationType,
                    request.Direction.GetValueOrDefault(true),
                    request.MessageId,
                    request.AttachmentsTypes,
                    request.Length).ConfigureAwait(false);
                MessagesNodeResponse response = new MessagesNodeResponse(request.RequestId, messages);
                NodeWebSocketCommunicationManager.SendResponse(response, current);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                NodeWebSocketCommunicationManager.SendResponse(new ResultNodeResponse(request.RequestId, ErrorCode.UnknownError), current);
            }
        }

        public bool IsObjectValid()
        {
            return request.ConversationId != 0
                && current.Node != null;
        }
    }
}