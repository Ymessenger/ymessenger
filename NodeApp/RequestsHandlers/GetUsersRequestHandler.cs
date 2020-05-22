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
using NodeApp.CacheStorageClasses;
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetUsersRequestHandler : IRequestHandler
    {
        private readonly GetUsersRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IConnectionsService connectionsService;
        private readonly ILoadUsersService loadUsersService;
        private readonly IPrivacyService privacyService;
        private readonly INodeRequestSender nodeRequestSender;
        public GetUsersRequestHandler(
            Request request, ClientConnection clientConnection, IConnectionsService connectionsService,
            ILoadUsersService loadUsersService, IPrivacyService privacyService, INodeRequestSender nodeRequestSender)
        {
            this.request = (GetUsersRequest)request;
            this.clientConnection = clientConnection;
            this.connectionsService = connectionsService;
            this.loadUsersService = loadUsersService;
            this.privacyService = privacyService;
            this.nodeRequestSender = nodeRequestSender;
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<UserVm> users = await loadUsersService.GetUsersByIdAsync(request.UsersId, request.IncludeContact ? clientConnection.UserId : null).ConfigureAwait(false);
            List<UserVm> resultUsers = new List<UserVm>();
            var groups = users.GroupBy(opt => opt.NodeId.GetValueOrDefault());
            foreach (var group in groups)
            {
                if (group.Key == NodeSettings.Configs.Node.Id)
                {
                    resultUsers.AddRange(await privacyService.ApplyPrivacySettingsAsync(group, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false));
                }
                else
                {
                    var nodeConnection = connectionsService.GetNodeConnection(group.Key);
                    if (nodeConnection != null)
                    {
                        await MetricsHelper.Instance.SetCrossNodeApiInvolvedAsync(request.RequestId).ConfigureAwait(false);
                        IEnumerable<UserVm> responseUsers = await nodeRequestSender.GetUsersInfoAsync(
                            group.Select(opt => opt.Id.GetValueOrDefault()).ToList(),
                            clientConnection.UserId,
                            nodeConnection).ConfigureAwait(false);
                        if (!responseUsers.IsNullOrEmpty())
                        {
                            resultUsers.AddRange(responseUsers);
                        }
                    }
                }
            }
            var otherNodesUsers = resultUsers.Where(opt => opt.NodeId != NodeSettings.Configs.Node.Id);
            if (otherNodesUsers != null)
            {
                foreach (var user in otherNodesUsers)
                {
                    var localUser = users.FirstOrDefault(opt => opt.Id == user.Id);
                    user.Contact = localUser?.Contact;
                    user.Groups = localUser?.Groups;
                }
            }
            return new UsersResponse(request.RequestId, resultUsers);
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

            return request.UsersId != null && request.UsersId.Any() && request.UsersId.Count() < 100;
        }
    }
}