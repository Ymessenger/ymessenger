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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace NodeApp
{
    public class BatchPhonesSearchRequestHandler : IRequestHandler
    {
        private readonly BatchPhonesSearchRequest request;
        private readonly ClientConnection clientConnection;
        private readonly ILoadUsersService loadUsersService;
        private readonly IConnectionsService connectionsService;
        private readonly IPrivacyService privacyService;
        private readonly INodeRequestSender nodeRequestSender;

        public BatchPhonesSearchRequestHandler(Request request,
            ClientConnection clientConnection,
            ILoadUsersService loadUsersService,
            IConnectionsService connectionsService,
            IPrivacyService privacyService,
            INodeRequestSender nodeRequestSender)
        {
            this.request = (BatchPhonesSearchRequest) request;
            this.clientConnection = clientConnection;
            this.loadUsersService = loadUsersService;
            this.connectionsService = connectionsService;
            this.privacyService = privacyService;
            this.nodeRequestSender = nodeRequestSender;
        }

        public async Task<Response> CreateResponseAsync()
        {
            var users = new ConcurrentBag<UserVm>();
            var getUsersTasks = new List<Task>
            {
                Task.Run(async () =>
                {
                    users.AddRange(privacyService.ApplyPrivacySettings(await loadUsersService.FindUsersByPhonesAsync(request.Phones).ConfigureAwait(false), request.Phones, clientConnection.UserId));
                })
            };
            var nodeConnections = connectionsService.GetNodeConnections();
            foreach(var connection in nodeConnections)
            {
                getUsersTasks.Add(Task.Run(async () =>
                {
                    if (connection.IsEncryptedConnection && connection.NodeWebSocket.State == WebSocketState.Open)
                    {
                        users.AddRange(await nodeRequestSender.BatchPhonesSearchAsync(connection, request.Phones, clientConnection.UserId).ConfigureAwait(false));
                    }
                }));
            }
            await Task.WhenAll(getUsersTasks).ConfigureAwait(false);
            return new UsersResponse(request.RequestId, users);
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
                throw new UnauthorizedUserException();
            return request.Phones != null && request.Phones.Any();
        }
    }
}