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
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Objects;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.Requests
{
    public class GetConversationsUsersNodeRequestHandler : ICommunicationHandler
    {
        private readonly ILoadChannelsService loadChannelsService;
        private readonly ILoadChatsService loadChatsService;
        private readonly GetConversationsUsersNodeRequest nodeRequest;
        private readonly NodeConnection nodeConnection;
        public GetConversationsUsersNodeRequestHandler(NodeRequest nodeRequest, NodeConnection nodeConnection, ILoadChannelsService loadChannelsService, ILoadChatsService loadChatsService)
        {
            this.nodeConnection = nodeConnection;
            this.nodeRequest = (GetConversationsUsersNodeRequest)nodeRequest;
            this.loadChatsService = loadChatsService;
            this.loadChannelsService = loadChannelsService;
        }

        public async Task HandleAsync()
        {            
            ConversationsUsersNodeResponse response;
            List<ChatUserVm> chatsUsers = new List<ChatUserVm>();
            List<ChannelUserVm> channelsUsers = new List<ChannelUserVm>();
            foreach(long id in nodeRequest.ConversationsIds) 
            {
                switch (nodeRequest.ConversationType)
                {
                    case ObjectsLibrary.Enums.ConversationType.Chat:
                        {
                            chatsUsers.AddRange(await loadChatsService.GetChatUsersAsync(id, null, int.MaxValue).ConfigureAwait(false));
                        }
                        break;
                    case ObjectsLibrary.Enums.ConversationType.Channel:
                        {
                            channelsUsers.AddRange(await loadChannelsService.GetChannelUsersAsync(id, null, null).ConfigureAwait(false));
                        }
                        break;
                }
            }
            response = new ConversationsUsersNodeResponse(nodeRequest.RequestId, chatsUsers, channelsUsers);
            await NodeWebSocketCommunicationManager.SendResponseAsync(response, nodeConnection).ConfigureAwait(false);
        }

        public bool IsObjectValid()
        {
            return nodeConnection.Node != null && !nodeRequest.ConversationsIds.IsNullOrEmpty();
        }
    }
}
