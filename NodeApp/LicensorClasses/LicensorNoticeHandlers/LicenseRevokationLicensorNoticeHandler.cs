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
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using ObjectsLibrary.LicensorNoticeClasses;

namespace NodeApp
{
    public class LicenseRevokationLicensorNoticeHandler : ICommunicationHandler
    {
        private readonly LicenseRevokationLicensorNotice licensorNotice;
        private readonly IConnectionsService connectionsService;

        public LicenseRevokationLicensorNoticeHandler(LicensorNotice licensorNotice, IConnectionsService connectionsService)
        {
            this.licensorNotice = (LicenseRevokationLicensorNotice) licensorNotice;
            this.connectionsService = connectionsService;
        }

        public async Task HandleAsync()
        {
            var nodeConnection = connectionsService.GetNodeConnection(licensorNotice.NodeId);
            if(nodeConnection != null && nodeConnection.NodeWebSocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                await nodeConnection.NodeWebSocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, null, CancellationToken.None).ConfigureAwait(false);
            }
        }

        public bool IsObjectValid()
        {
            return true;
        }
    }
}