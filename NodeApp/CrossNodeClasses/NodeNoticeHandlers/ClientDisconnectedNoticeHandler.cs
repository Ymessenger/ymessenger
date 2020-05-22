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
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class ClientDisconnectedNoticeHandler : ICommunicationHandler
    {
        private readonly ClientDisconnectedNodeNotice notice;
        private readonly IConnectionsService connectionsService;

        public ClientDisconnectedNoticeHandler(NodeNotice notice, IConnectionsService connectionsService)
        {
            this.notice = (ClientDisconnectedNodeNotice)notice;
            this.connectionsService = connectionsService;
        }
        public Task HandleAsync()
        {
            return Task.Run(() =>
            {
                var clientConnections = connectionsService.GetUserClientConnections(notice.UserId);
                if (clientConnections != null)
                {
                    var disconnected = clientConnections.FirstOrDefault(opt => opt.ClientSocket == null);
                    clientConnections.Remove(disconnected);
                }
            });
        }

        public bool IsObjectValid()
        {
            return notice.UserId != 0;
        }
    }
}
