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
using NodeApp.Interfaces.Services.Channels;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class EditChannelUsersRequestHandler : IRequestHandler
    {
        private readonly EditChannelUsersRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly IUpdateChannelsService updateChannelsService;

        public EditChannelUsersRequestHandler(
            Request request,
            ClientConnection clientConnection,
            INodeNoticeService nodeNoticeService,
            ILoadChannelsService loadChannelsService,
            IUpdateChannelsService updateChannelsService)
        {
            this.request = (EditChannelUsersRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.loadChannelsService = loadChannelsService;
            this.updateChannelsService = updateChannelsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                List<ChannelUserVm> editedChannelUsers = await updateChannelsService.EditChannelUsersAsync(
                    request.Users, clientConnection.UserId.GetValueOrDefault(), request.ChannelId).ConfigureAwait(false);
                ChannelVm channel = await loadChannelsService.GetChannelByIdAsync(request.ChannelId).ConfigureAwait(false);
                nodeNoticeService.SendChannelUsersNodeNoticeAsync(editedChannelUsers, clientConnection.UserId.GetValueOrDefault(), channel);
                UsersConversationsCacheService.Instance.UpdateUsersChannelsAsync(editedChannelUsers.Select(opt => opt.UserId));
                BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateChannelUsersSegmentAsync(
                    editedChannelUsers,
                    NodeSettings.Configs.Node.Id,
                    request.ChannelId,
                    NodeData.Instance.NodeKeys.SignPrivateKey,
                    NodeData.Instance.NodeKeys.SymmetricKey,
                    NodeData.Instance.NodeKeys.Password,
                    NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
                nodeNoticeService.SendBlockSegmentsNodeNoticeAsync(new List<BlockSegmentVm> { segment });
                BlockGenerationHelper.Instance.AddSegment(segment);
                return new ChannelUsersResponse(
                    request.RequestId,
                    editedChannelUsers.Where(opt => opt.ChannelUserRole.GetValueOrDefault() >= ChannelUserRole.Administrator && opt.Banned == false),
                    editedChannelUsers.Where(opt => opt.ChannelUserRole.GetValueOrDefault() == ChannelUserRole.Subscriber && opt.Banned == false),
                    editedChannelUsers.Where(opt => opt.Banned == true));
            }
            catch (PermissionDeniedException ex)
            {
                Logger.WriteLog(ex);
                return new ResultResponse(request.RequestId, "Channel not found or user does not have access to the channel.", ErrorCode.PermissionDenied);
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

            foreach (var channelUser in request.Users)
            {
                if (channelUser.ChannelUserRole == ChannelUserRole.Creator && channelUser.UserId != clientConnection.UserId)
                {
                    return false;
                }
            }

            return request.ChannelId != 0
                && request.Users != null && request.Users.Any();
        }
    }
}