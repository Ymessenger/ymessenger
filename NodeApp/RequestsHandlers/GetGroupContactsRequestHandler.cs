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
using NodeApp.Converters;
using NodeApp.CacheStorageClasses;
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetGroupContactsRequestHandler : IRequestHandler
    {
        private readonly GetGroupContactsRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IConnectionsService connectionsService;
        private readonly IGroupsService groupsService;
        private readonly INodeRequestSender nodeRequestSender;

        public GetGroupContactsRequestHandler(Request request, ClientConnection clientConnection, IConnectionsService connectionsService, IGroupsService groupsService, INodeRequestSender nodeRequestSender)
        {
            this.request = (GetGroupContactsRequest)request;
            this.clientConnection = clientConnection;
            this.connectionsService = connectionsService;
            this.groupsService = groupsService;
            this.nodeRequestSender = nodeRequestSender;
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<ContactDto> contactsDto = await groupsService.GetGroupContactsAsync(
                request.GroupId, clientConnection.UserId.GetValueOrDefault(), request.NavigationUserId.GetValueOrDefault()).ConfigureAwait(false);
            List<ContactVm> contactsVm = ContactConverter.GetContactsVm(contactsDto);
            IEnumerable<IGrouping<long, UserDto>> groupedUsers = contactsDto.Select(opt => opt.ContactUser).GroupBy(opt => opt.NodeId.GetValueOrDefault());
            ConcurrentBag<UserVm> resultUsers = new ConcurrentBag<UserVm>();
            List<Task> getUsersTasks = new List<Task>();
            foreach (var group in groupedUsers)
            {
                getUsersTasks.Add(Task.Run(async () =>
                {
                    var nodeConnection = connectionsService.GetNodeConnection(group.Key);
                    if (group.Key != NodeSettings.Configs.Node.Id && nodeConnection != null)
                    {
                        await MetricsHelper.Instance.SetCrossNodeApiInvolvedAsync(request.RequestId).ConfigureAwait(false);
                        var users = await nodeRequestSender.GetUsersInfoAsync(
                            group.Select(opt => opt.Id).ToList(),
                            clientConnection.UserId,
                            nodeConnection).ConfigureAwait(false);
                        resultUsers.AddRange(users);
                        resultUsers.AddRange(users);
                    }
                    else
                    {
                        resultUsers.AddRange(UserConverter.GetUsersVm(group.ToList(), clientConnection.UserId));
                    }
                }));
            }
            await Task.WhenAll(getUsersTasks).ConfigureAwait(false);
            foreach (var contact in contactsVm)
            {
                contact.ContactUser = resultUsers.FirstOrDefault(opt => opt.Id == contact.ContactUserId);
            }
            return new ContactsResponse(request.RequestId, contactsVm);
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

            return true;
        }
    }
}