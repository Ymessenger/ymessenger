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
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetUsersInfoNodeRequestHandler : IRequestHandler
    {
        private readonly GetUsersInformationNodeRequest request;
        private readonly ILoadUsersService loadUsersService;
        private readonly IPrivacyService privacyService;
        public GetUsersInfoNodeRequestHandler(Request request, ILoadUsersService loadUsersService, IPrivacyService privacyService)
        {
            this.request = (GetUsersInformationNodeRequest)request;
            this.loadUsersService = loadUsersService;
            this.privacyService = privacyService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<UserVm> users = await loadUsersService.GetUsersAsync(request.User, 100, request.NavigationUserId.GetValueOrDefault()).ConfigureAwait(false);
            users = await privacyService.ApplyPrivacySettingsAsync(users).ConfigureAwait(false);
            return new FoundUsersResponse(request.RequestId, request.User, users);
        }

        public bool IsRequestValid()
        {
            return true;
        }
    }
}
