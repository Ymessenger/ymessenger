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
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetSelfRequestHandler : IRequestHandler
    {
        private readonly Request request;
        private readonly ClientConnection clientConnection;
        private readonly ILoadUsersService loadUsersService;
        public GetSelfRequestHandler(Request request, ClientConnection clientConnection, ILoadUsersService loadUsersService)
        {
            this.request = request;
            this.clientConnection = clientConnection;
            this.loadUsersService = loadUsersService;
        }
        public async Task<Response> CreateResponseAsync()
        {
            var user = await loadUsersService.GetUserInformationAsync(clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            return new UserResponse(request.RequestId, user);
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
            {
                throw new UnauthorizedUserException();
            }

            return true;
        }
    }
}
