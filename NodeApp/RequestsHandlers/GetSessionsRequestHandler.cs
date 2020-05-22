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
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp
{
    public class GetSessionsRequestHandler : IRequestHandler
    {
        private readonly Request request;
        private readonly ClientConnection clientConnection;
        private readonly ILoadUsersService loadUsersService;

        public GetSessionsRequestHandler(Request request, ClientConnection clientConnection, ILoadUsersService loadUsersService)
        {
            this.request = request;
            this.clientConnection = clientConnection;
            this.loadUsersService = loadUsersService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<SessionVm> sessions = await loadUsersService.GetUserSessionsAsync(clientConnection.UserId.Value).ConfigureAwait(false);
            sessions.ForEach(session => session.IsCurrent = clientConnection.CurrentToken.Id == session.TokenId);
            return new SessionsResponse(request.RequestId, sessions);
        }

        public bool IsRequestValid()
        {
            return clientConnection.UserId != null;
        }
    }
}