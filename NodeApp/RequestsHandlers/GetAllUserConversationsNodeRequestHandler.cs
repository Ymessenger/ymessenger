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
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetAllUserConversationsNodeRequestHandler : IRequestHandler
    {
        private readonly ClientConnection clientConnection;
        private readonly GetAllUserConversationsRequest request;
        private readonly IConversationsService conversationsService;
        private const byte CONVERSATIONS_LIMIT = 100;

        public GetAllUserConversationsNodeRequestHandler(Request request, ClientConnection clientConnection, IConversationsService conversationsService)
        {
            this.clientConnection = clientConnection;
            this.request = (GetAllUserConversationsRequest)request;
            this.conversationsService = conversationsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            var conversations = await conversationsService.GetUsersConversationsAsync(
                clientConnection.UserId.Value, request.NavigationConversationId, request.NavigationConversationType, request.RequestId).ConfigureAwait(false);
            return new ConversationsResponse(request.RequestId, conversations.Take(CONVERSATIONS_LIMIT));
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
