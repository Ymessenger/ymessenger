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
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    class ProxyUsersNotificationsNodeNoticeHandler : ICommunicationHandler
    {
        private readonly ProxyUsersNotificationsNodeNotice notice;
        private readonly NodeConnection nodeConnection;
        private readonly IConnectionsService connectionsService;

        public ProxyUsersNotificationsNodeNoticeHandler(NodeNotice notice, NodeConnection nodeConnection, IConnectionsService connectionsService)
        {
            this.notice = (ProxyUsersNotificationsNodeNotice)notice;
            this.nodeConnection = nodeConnection;
            this.connectionsService = connectionsService;
        }

        public async Task HandleAsync()
        {
            var clientConnections = connectionsService.GetUserClientConnections(notice.UserId);
            if (clientConnections != null)
            {
                ClientConnection clientConnection = clientConnections.FirstOrDefault(opt => opt.IsProxiedClientConnection && opt.ClientSocket != null);
                if (clientConnection != null)
                {
                    await clientConnection.ClientSocket.SendAsync(
                        notice.CommunicationData,
                        System.Net.WebSockets.WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        public bool IsObjectValid()
        {
            return nodeConnection.Node != null && notice.CommunicationData != null && notice.CommunicationData.Any();
        }
    }
}