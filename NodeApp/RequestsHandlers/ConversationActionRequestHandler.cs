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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class ConversationActionRequestHandler : IRequestHandler
    {
        private readonly ConversationActionRequest request;
        private readonly ClientConnection clientConnection;        
        private readonly IConversationsService conversationsService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly ILoadDialogsService loadDialogsService;
        private readonly ISystemMessagesService systemMessagesService;
        public ConversationActionRequestHandler(
            Request request,
            ClientConnection clientConnection,
            IConversationsService conversationsService,
            IConversationsNoticeService conversationsNoticeService,
            INodeNoticeService nodeNoticeService,
            ILoadDialogsService loadDialogsService,
            ISystemMessagesService systemMessagesService)
        {
            this.request = (ConversationActionRequest) request;
            this.clientConnection = clientConnection;
            this.conversationsService = conversationsService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.nodeNoticeService = nodeNoticeService;
            this.loadDialogsService = loadDialogsService;
            this.systemMessagesService = systemMessagesService;
        }
        public async Task<Response> CreateResponseAsync()
        {
            if (!await conversationsService.IsUserInConversationAsync(request.ConversationType, request.ConversationId, clientConnection.UserId.Value))
            {
                return new ResultResponse(request.RequestId, "The user is not in conversation.", ErrorCode.PermissionDenied);
            }
            var nodesIds = await conversationsService.GetConversationNodesIdsAsync(request.ConversationType, request.ConversationId).ConfigureAwait(false);
            if (nodesIds.Count > 1)
            {
                long? dialogUserId = null;
                if (request.ConversationType == ConversationType.Dialog)
                {
                    var users = await loadDialogsService.GetDialogUsersAsync(request.ConversationId).ConfigureAwait(false);
                    dialogUserId = users.FirstOrDefault(user => user.Id != clientConnection.UserId).Id;
                }
                nodeNoticeService.SendConverationActionNodeNoticeAsync(clientConnection.UserId.Value, dialogUserId, request.ConversationId, request.ConversationType, request.Action, nodesIds);
            }
            if (request.Action != ConversationAction.Screenshot)
            {
                conversationsNoticeService.SendConversationActionNoticeAsync(clientConnection.UserId.Value, request.ConversationType, request.ConversationId, request.Action);
            }
            if (request.ConversationType != ConversationType.Channel && request.Action == ConversationAction.Screenshot)
            {
                var systemMessageInfo = SystemMessageInfoFactory.CreateScreenshotMessageInfo(clientConnection.UserId.Value);
                var message = await systemMessagesService.CreateMessageAsync(request.ConversationType, request.ConversationId, systemMessageInfo).ConfigureAwait(false);
                conversationsNoticeService.SendSystemMessageNoticeAsync(message);
            }
            return new ResultResponse(request.RequestId);
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
                throw new UnauthorizedUserException();
            if (!clientConnection.Confirmed)
                throw new PermissionDeniedException();
            return true;
        }
    }
}
