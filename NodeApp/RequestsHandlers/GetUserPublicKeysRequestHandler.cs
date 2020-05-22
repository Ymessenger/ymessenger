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
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetUserPublicKeysRequestHandler : IRequestHandler
    {
        private readonly GetUserPublicKeysRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IKeysService keysService;

        public GetUserPublicKeysRequestHandler(Request request, ClientConnection clientConnection, IKeysService keysService)
        {
            this.request = (GetUserPublicKeysRequest)request;
            this.clientConnection = clientConnection;
            this.keysService = keysService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<KeyVm> userKeys;
            if (request.KeysId != null)
            {
                userKeys = await keysService.GetUserPublicKeysAsync(request.UserId, request.KeysId).ConfigureAwait(false);
            }
            else
            {
                userKeys = await keysService.GetUserPublicKeysAsync(
                    request.UserId,
                    request.NavigationTime.GetValueOrDefault(),
                    request.Direction.GetValueOrDefault(true)).ConfigureAwait(false);
            }
            return new KeysResponse(request.RequestId, userKeys);
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

            return request.UserId != 0;
        }
    }
}