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
using NodeApp.CrossNodeClasses;
using NodeApp.CrossNodeClasses.Requests;
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp
{
    public class BatchPhonesSearchNodeRequestHandler : ICommunicationHandler
    {
        private readonly BatchPhonesSearchNodeRequest request;
        private readonly NodeConnection current;
        private readonly ILoadUsersService loadUsersService;
        private readonly IPrivacyService privacyService;

        public BatchPhonesSearchNodeRequestHandler(NodeRequest request, NodeConnection current, ILoadUsersService loadUsersService, IPrivacyService privacyService)
        {
            this.request = (BatchPhonesSearchNodeRequest) request;
            this.current = current;
            this.loadUsersService = loadUsersService;
            this.privacyService = privacyService;
        }

        public async Task HandleAsync()
        {
            var users = await loadUsersService.FindUsersByPhonesAsync(request.Phones).ConfigureAwait(false);
            NodeWebSocketCommunicationManager.SendResponse(
                new UsersNodeResponse(request.RequestId, privacyService.ApplyPrivacySettings(users, request.Phones, request.RequestorId)), 
                current);
        }

        public bool IsObjectValid()
        {
            return current.Node != null && request.Phones != null && request.Phones.Any();
        }
    }
}