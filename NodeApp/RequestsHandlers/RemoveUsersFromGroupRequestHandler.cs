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
using NodeApp.Interfaces;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class RemoveUsersFromGroupRequestHandler : IRequestHandler
    {
        private readonly RemoveUsersFromGroupRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IGroupsService groupsService;

        public RemoveUsersFromGroupRequestHandler(Request request, ClientConnection clientConnection, IGroupsService groupsService)
        {
            this.request = (RemoveUsersFromGroupRequest)request;
            this.clientConnection = clientConnection;
            this.groupsService = groupsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                await groupsService.RemoveUsersFromGroupsAsync(request.UsersId, request.GroupId, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                return new ResultResponse(request.RequestId);
            }
            catch (ObjectDoesNotExistsException ex)
            {
                Logger.WriteLog(ex);
                return new ResultResponse(request.RequestId, "Users are not in a group.", ErrorCode.DeleteUserError);
            }
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

            return request.UsersId != null && request.UsersId.Any();
        }
    }
}