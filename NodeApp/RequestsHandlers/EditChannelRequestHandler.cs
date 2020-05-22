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
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class EditChannelRequestHandler : IRequestHandler
    {
        private readonly EditChannelRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly IUpdateChannelsService updateChannelsService;
        private readonly ISystemMessagesService systemMessagesService;

        public EditChannelRequestHandler(
            Request request, 
            ClientConnection clientConnection, 
            INodeNoticeService nodeNoticeService, 
            IConversationsNoticeService conversationsNoticeService,
            ILoadChannelsService loadChannelsService,
            IUpdateChannelsService updateChannelsService,
            ISystemMessagesService systemMessagesService)
        {
            this.request = (EditChannelRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.loadChannelsService = loadChannelsService;
            this.updateChannelsService = updateChannelsService;
            this.systemMessagesService = systemMessagesService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                ChannelVm editableChannel = await loadChannelsService.GetChannelByIdAsync(request.Channel.ChannelId.Value);
                ChannelVm channel = await updateChannelsService.EditChannelAsync(request.Channel, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                if(editableChannel.ChannelName != channel.ChannelName)
                {
                    var systemMessageInfo = SystemMessageInfoFactory.CreateNameChangedMessageInfo(editableChannel.ChannelName, channel.ChannelName);
                    var message = await systemMessagesService.CreateMessageAsync(ObjectsLibrary.Enums.ConversationType.Channel, channel.ChannelId.Value, systemMessageInfo);
                    conversationsNoticeService.SendSystemMessageNoticeAsync(message);
                }
                IEnumerable<long> usersId = await loadChannelsService.GetChannelUsersIdAsync(channel.ChannelId.GetValueOrDefault()).ConfigureAwait(false);
                conversationsNoticeService.SendChannelNoticeAsync(channel, usersId.ToList(), clientConnection);
                UsersConversationsCacheService.Instance.UpdateUsersChannelsAsync(usersId.ToList());
                nodeNoticeService.SendChannelNodeNoticeAsync(channel, clientConnection.UserId.GetValueOrDefault(), null);                
                BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateChannelSegmentAsync(channel, NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                BlockGenerationHelper.Instance.AddSegment(segment);
                return new ChannelsResponse(request.RequestId, channel);
            }
            catch(PermissionDeniedException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Channel not found or user does not have access to the channel.", ObjectsLibrary.Enums.ErrorCode.PermissionDenied);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
                throw new UnauthorizedUserException();
            if (!clientConnection.Confirmed)
                throw new PermissionDeniedException("User is not confirmed.");
            if (request.Channel != null)
            {
                if (request.Channel.About != null)                
                    return request.Channel.About.Length <= 1000;
                
                if (request.Channel.Photo != null)                
                    return request.Channel.Photo.Length <= 300;
                
                if(request.Channel.ChannelName != null)                
                    return request.Channel.ChannelName.Length > 0 && request.Channel.ChannelName.Length < 50;
                
                return true;
            }
            else
                return false;            
        }
    }
}