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
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class AddUsersToChannelsRequestHandler : IRequestHandler
    {
        private readonly AddUsersToChannelsRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IUpdateChannelsService updateChannelsService;
        private readonly ILoadChannelsService loadChannelsService;        
        private readonly IConnectionsService connectionsService;
        private readonly INodeRequestSender nodeRequestSender;

        public AddUsersToChannelsRequestHandler(
            Request request, 
            ClientConnection clientConnection, 
            INodeNoticeService nodeNoticeService, 
            IConversationsNoticeService conversationsNoticeService,
            IUpdateChannelsService updateChannelsService,
            ILoadChannelsService loadChannelsService,            
            IConnectionsService connectionsService,
            INodeRequestSender nodeRequestSender)
        {
            this.request = (AddUsersToChannelsRequest) request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.updateChannelsService = updateChannelsService;
            this.loadChannelsService = loadChannelsService;
            this.connectionsService = connectionsService;
            this.nodeRequestSender = nodeRequestSender;           
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<ChannelUserVm> resultChannelsUsers = new List<ChannelUserVm>();
            List<BlockSegmentVm> segments = new List<BlockSegmentVm>();
            foreach (long channelId in request.ChannelsId)
            {
                try
                {
                    ChannelVm channel = await loadChannelsService.GetChannelByIdAsync(channelId).ConfigureAwait(false);
                    var existingUsers = await loadChannelsService.GetChannelUsersAsync(channelId, null, null).ConfigureAwait(false);
                    if(!existingUsers.Any(opt => opt.ChannelUserRole == ChannelUserRole.Creator))
                    {
                        var nodeConnection = connectionsService.GetNodeConnection(channel.NodesId.FirstOrDefault(id => id != NodeSettings.Configs.Node.Id));
                        if (nodeConnection != null) 
                        {
                            await nodeRequestSender.GetChannelInformationAsync(channelId, nodeConnection).ConfigureAwait(false);                           
                        }
                    }
                    List<ChannelUserVm> channelUsers =
                        await updateChannelsService.AddUsersToChannelAsync(request.UsersId.ToList(), channelId, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                    resultChannelsUsers.AddRange(channelUsers);
                    BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateChannelUsersSegmentAsync(
                        channelUsers,
                        NodeSettings.Configs.Node.Id, 
                        channelId,
                        NodeData.Instance.NodeKeys.SignPrivateKey,
                        NodeData.Instance.NodeKeys.SymmetricKey,
                        NodeData.Instance.NodeKeys.Password,
                        NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
                    segments.Add(segment);
                    conversationsNoticeService.SendNewChannelNoticesAsync(channelUsers, channelId, clientConnection);
                    nodeNoticeService.SendChannelUsersNodeNoticeAsync(channelUsers, clientConnection.UserId.Value, channel);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex, request);
                }
            }
            BlockGenerationHelper.Instance.AddSegments(segments);
            return new ChannelUsersResponse(request.RequestId, null, resultChannelsUsers, null);
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
                throw new UnauthorizedUserException();
            if (!clientConnection.Confirmed)
                throw new PermissionDeniedException("User is not confirmed.");
            return request.ChannelsId != null
                   && request.ChannelsId.Any();
        }
    }
}