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
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Threading.Tasks;

namespace NodeApp
{
    public class MuteConversationRequestHandler : IRequestHandler
    {
        private readonly MuteConversationRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IConversationsService conversationsService;

        public MuteConversationRequestHandler(Request request, ClientConnection clientConnection, IConversationsService conversationsService)
        {
            this.request = (MuteConversationRequest)request;
            this.clientConnection = clientConnection;
            this.conversationsService = conversationsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                await conversationsService.MuteConversationAsync(
                    request.ConversationType, request.ConversationId, clientConnection.UserId).ConfigureAwait(false);
                UsersConversationsCacheService.Instance.ConversationMutedUpdateConversations(
                    request.ConversationId, request.ConversationType, clientConnection.UserId.Value);
                return new ResultResponse(request.RequestId);
            }
            catch (PermissionDeniedException)
            {
                return new ResultResponse(request.RequestId, "The user does not have access to the conversation.", ObjectsLibrary.Enums.ErrorCode.PermissionDenied);
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

            return true;
        }
    }
}