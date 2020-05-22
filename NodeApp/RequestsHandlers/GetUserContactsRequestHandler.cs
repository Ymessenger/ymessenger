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
    public class GetUserContactsRequestHandler : IRequestHandler
    {
        private readonly GetUserContactsRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IConnectionsService connectionsService;
        private readonly IContactsService contactsService;
        private readonly INodeRequestSender nodeRequestSender;

        public GetUserContactsRequestHandler(Request request, ClientConnection clientConnection, IConnectionsService connectionsService, IContactsService contactsService, INodeRequestSender nodeRequestSender)
        {
            this.request = (GetUserContactsRequest)request;
            this.clientConnection = clientConnection;
            this.connectionsService = connectionsService;
            this.contactsService = contactsService;
            this.nodeRequestSender = nodeRequestSender;
        }

        public async Task<Response> CreateResponseAsync()
        {
            ConcurrentBag<UserVm> contactsUsers = new ConcurrentBag<UserVm>();
            List<Task> getUsersTasks = new List<Task>();
            List<ContactDto> contactsDto = await contactsService.GetUserContactsAsync(clientConnection.UserId.GetValueOrDefault(), request.NavigationUserId).ConfigureAwait(false);
            var contacts = ContactConverter.GetContactsVm(contactsDto);
            var contactsGroupedUsers = contactsDto.Select(opt => opt.ContactUser).GroupBy(opt => opt.NodeId);
            foreach (var group in contactsGroupedUsers)
            {
                var nodeConnection = connectionsService.GetNodeConnection(group.Key.GetValueOrDefault());
                if (group.Key != NodeSettings.Configs.Node.Id && nodeConnection != null)
                {
                    getUsersTasks.Add(Task.Run(async () =>
                    {
                        await MetricsHelper.Instance.SetCrossNodeApiInvolvedAsync(request.RequestId).ConfigureAwait(false);
                        contactsUsers.AddRange(await nodeRequestSender.GetUsersInfoAsync(
                            group.Select(opt => opt.Id).ToList(),
                            clientConnection.UserId,
                            nodeConnection).ConfigureAwait(false));
                    }));
                }
                else if (group.Key == NodeSettings.Configs.Node.Id)
                {
                    contactsUsers.AddRange(UserConverter.GetUsersVm(group.ToList(), clientConnection.UserId));
                }
            }
            await Task.WhenAll(getUsersTasks).ConfigureAwait(false);
            foreach (var contact in contacts)
            {
                contact.ContactUser = contactsUsers.FirstOrDefault(opt => opt.Id == contact.ContactUserId);
            }
            return new ContactsResponse(request.RequestId, contacts);
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