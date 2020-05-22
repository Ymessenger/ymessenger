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
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeResponseHandlers
{
    public class ProxyUsersCommunicationsNodeResponseHandler : ICommunicationHandler
    {
        private readonly ProxyUsersCommunicationsNodeResponse response;
        private readonly IConnectionsService connectionsService;

        public ProxyUsersCommunicationsNodeResponseHandler(NodeResponse response, IConnectionsService connectionsService)
        {
            this.response = (ProxyUsersCommunicationsNodeResponse)response;
            this.connectionsService = connectionsService;
        }

        public async Task HandleAsync()
        {
            var clientConnections = connectionsService.GetUserClientConnections(response.UserId);
            if (clientConnections != null)
            {
                ClientConnection clientConnection = clientConnections.FirstOrDefault(opt => opt.IsProxiedClientConnection
                    && opt.ClientSocket != null
                    && opt.ClientSocket.State == System.Net.WebSockets.WebSocketState.Open);
                if (clientConnection != null)
                {
                    await clientConnection.ClientSocket.SendAsync(
                        response.CommunicationData,
                        System.Net.WebSockets.WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
        public bool IsObjectValid()
        {
            return !response.CommunicationData.IsNullOrEmpty();
        }
    }
}
