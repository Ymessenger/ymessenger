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
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class BlockUsersRequestHandler : IRequestHandler
    {
        private readonly BlockUsersRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IUpdateUsersService updateUsersService;
        public BlockUsersRequestHandler(Request request, ClientConnection clientConnection, INodeNoticeService nodeNoticeService, IUpdateUsersService updateUsersService)
        {
            this.request = (BlockUsersRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.updateUsersService = updateUsersService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                var blockedUsersId = await updateUsersService.AddUsersToBlackListAsync(request.UsersId,
                        clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                if (blockedUsersId.Any())
                {
                    nodeNoticeService.SendUsersAddedToUserBlacklistNodeNoticeAsync(
                        blockedUsersId.ToList(),
                        clientConnection.UserId.GetValueOrDefault());
                    BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateUsersAddedToUserBlacklistSegmentAsync(
                        blockedUsersId,
                        clientConnection.UserId.GetValueOrDefault(),
                        NodeSettings.Configs.Node.Id,
                        NodeData.Instance.NodeKeys.SignPrivateKey,
                        NodeData.Instance.NodeKeys.SymmetricKey,
                        NodeData.Instance.NodeKeys.Password,
                        NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
                    BlockGenerationHelper.Instance.AddSegment(segment);
                    nodeNoticeService.SendBlockSegmentsNodeNoticeAsync(new List<BlockSegmentVm> { segment });
                    UsersConversationsCacheService.Instance.UpdateUsersDialogsAsync(
                        blockedUsersId.Append(clientConnection.UserId.GetValueOrDefault()));
                }
                return new ResultResponse(request.RequestId);
            }
            catch (ObjectDoesNotExistsException ex)
            {
                Logger.WriteLog(ex);
                return new ResultResponse(request.RequestId, "Users not found.", ErrorCode.ObjectDoesNotExists);
            }
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

            if (request.UsersId == null || request.UsersId.Contains(clientConnection.UserId.GetValueOrDefault()) ||
                !request.UsersId.Any())
            {
                return false;
            }

            request.UsersId = request.UsersId.Distinct().ToList();
            if (request.UsersId.Count > 50)
            {
                return false;
            }

            return true;
        }
    }
}
