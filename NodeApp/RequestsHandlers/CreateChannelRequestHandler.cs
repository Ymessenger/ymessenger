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
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class CreateChannelRequestHandler : IRequestHandler
    {
        private readonly CreateChannelRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly ICreateChannelsService createChannelsService;

        public CreateChannelRequestHandler(
            Request request, 
            ClientConnection clientConnection, 
            INodeNoticeService nodeNoticeService, 
            IConversationsNoticeService conversationsNoticeService,
            ILoadChannelsService loadChannelsService,
            ICreateChannelsService createChannelsService)
        {
            this.request = (CreateChannelRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.loadChannelsService = loadChannelsService;
            this.createChannelsService = createChannelsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                request.Channel.ChannelId = null;
                request.Channel.Tag = null;
                ChannelVm channel = await createChannelsService.CreateChannelAsync(request.Channel, clientConnection.UserId.GetValueOrDefault(), request.Subscribers).ConfigureAwait(false);
                List<long> usersId = await loadChannelsService.GetChannelUsersIdAsync(channel.ChannelId.GetValueOrDefault()).ConfigureAwait(false);
                conversationsNoticeService.SendChannelNoticeAsync(channel, usersId, clientConnection);
                UsersConversationsCacheService.Instance.UpdateUsersChannelsAsync(usersId);
                nodeNoticeService.SendChannelNodeNoticeAsync(channel, clientConnection.UserId.Value, request.Subscribers);                
                BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateChannelSegmentAsync(channel, NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                BlockGenerationHelper.Instance.AddSegment(segment);
                return new ChannelsResponse(request.RequestId, channel);
            }
            catch(UserNotFoundException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Users not found.", ErrorCode.ObjectDoesNotExists);
            }
            catch(UserBlockedException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "The user is blacklisted by another user.", ErrorCode.UserBlocked);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
                throw new UnauthorizedUserException();
            if (!clientConnection.Confirmed)
                throw new PermissionDeniedException("User is not confirmed.");
            if (request.Channel.About != null)
            {
                return request.Channel.About.Length <= 1000;                
            }
            if(request.Channel.Photo != null)
            {                
               return request.Channel.Photo.Length <= 300;
            }
            return request.Channel != null
                && !string.IsNullOrWhiteSpace(request.Channel.ChannelName)
                && request.Channel.ChannelName.Length <= 50;                
        }
    }
}