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
using NodeApp.Blockchain;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.PublicDataEntities;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class EditUserRequestHandler : IRequestHandler
    {
        public EditUserRequestHandler(Request request, ClientConnection clientConnection, INodeNoticeService nodeNoticeService, IUpdateUsersService updateUsersService)
        {
            this.request = (EditUserRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.updateUsersService = updateUsersService;
        }

        private readonly EditUserRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IUpdateUsersService updateUsersService;
        public async Task<Response> CreateResponseAsync()
        {
            UserVm editableUser = await updateUsersService.EditUserAsync(request.User, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateEditUserSegmentAsync(
                editableUser,
                NodeSettings.Configs.Node.Id,
                NodeData.Instance.NodeKeys.SignPrivateKey,
                NodeData.Instance.NodeKeys.SymmetricKey,
                NodeData.Instance.NodeKeys.Password,
                NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
            BlockGenerationHelper.Instance.AddSegment(segment);
            EditUserBlockData editBlockData = (EditUserBlockData)segment.PublicData;
            ShortUser shortUser = new ShortUser
            {
                PrivateData = segment.PrivateData,
                UserId = editBlockData.UserId
            };
            nodeNoticeService.SendEditUsersNodeNoticeAsync(shortUser, segment);
            UsersConversationsCacheService.Instance.UserEditedUpdateUserDialogsAsync(editableUser);
            return new UserResponse(request.RequestId, editableUser);
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId.GetValueOrDefault() == 0)
            {
                throw new UnauthorizedUserException();
            }

            if (!clientConnection.Confirmed)
            {
                throw new PermissionDeniedException("User is not confirmed.");
            }

            if (request.User == null)
            {
                return false;
            }

            if (request.User.NameFirst != null && string.IsNullOrWhiteSpace(request.User.NameFirst))
            {
                return false;
            }

            return true;
        }
    }
}
