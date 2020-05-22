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
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp
{
    public class GetDevicesPrivateKeysRequestHandler : IRequestHandler
    {
        private readonly GetDevicesPrivateKeysRequest request;
        private readonly ClientConnection clientConnection;
        private readonly ClientRequestService clientRequestService;
        private readonly IConnectionsService connectionsService;

        public GetDevicesPrivateKeysRequestHandler(Request request, ClientConnection clientConnection, ClientRequestService clientRequestService, IConnectionsService connectionsService)
        {
            this.request = (GetDevicesPrivateKeysRequest)request;
            this.clientConnection = clientConnection;
            this.clientRequestService = clientRequestService;
            this.connectionsService = connectionsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            var clientConnections = connectionsService.GetUserClientConnections(clientConnection.UserId.Value);
            if (clientConnections != null)
            {
                var connections = clientConnections.Where(connection => connection != clientConnection)?.ToList();
                if (connections != null && connections.Any())
                {
                    clientRequestService.ExchangeEncryptedPrivateKeysAsync(request.PublicKey ?? clientConnection.PublicKey, connections, clientConnection);
                    return new ResultResponse(request.RequestId);
                }
            }
            return new ResultResponse(request.RequestId, "Another connections was not found.", ErrorCode.ObjectDoesNotExists);
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

            return (request.PublicKey != null && request.PublicKey.Any())
                || (clientConnection.PublicKey != null && clientConnection.PublicKey.Any());
        }
    }
}