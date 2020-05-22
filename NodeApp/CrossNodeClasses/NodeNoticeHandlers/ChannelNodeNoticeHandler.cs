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
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Objects;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class ChannelNodeNoticeHandler : ICommunicationHandler
    {
        private readonly ChannelNodeNotice notice;
        private readonly NodeConnection current;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly ICreateChannelsService createChannelsService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly ISystemMessagesService systemMessagesService;

        public ChannelNodeNoticeHandler(
            NodeNotice notice,
            NodeConnection current,
            IConversationsNoticeService conversationsNoticeService,
            ICreateChannelsService createChannelsService,
            ILoadChannelsService loadChannelsService,
            ISystemMessagesService systemMessagesService)
        {
            this.notice = (ChannelNodeNotice)notice;
            this.current = current;
            this.conversationsNoticeService = conversationsNoticeService;
            this.createChannelsService = createChannelsService;
            this.loadChannelsService = loadChannelsService;
            this.systemMessagesService = systemMessagesService;
        }

        public async Task HandleAsync()
        {
            var editableChannel = await loadChannelsService.GetChannelByIdAsync(notice.Channel.ChannelId.Value).ConfigureAwait(false);            
            ChannelVm channel = await createChannelsService.CreateOrEditChannelAsync(notice.Channel, notice.RequestorId, notice.Subscribers).ConfigureAwait(false);
            if (editableChannel?.ChannelName == channel.ChannelName)
            {
                var systemMessageInfo = SystemMessageInfoFactory.CreateNameChangedMessageInfo(editableChannel.ChannelName, channel.ChannelName);
                var message = await systemMessagesService.CreateMessageAsync(ObjectsLibrary.Enums.ConversationType.Channel, channel.ChannelId.Value, systemMessageInfo);
                conversationsNoticeService.SendSystemMessageNoticeAsync(message);
            }
            IEnumerable<long> usersId = await loadChannelsService.GetChannelUsersIdAsync(channel.ChannelId.GetValueOrDefault()).ConfigureAwait(false);
            conversationsNoticeService.SendChannelNoticeAsync(channel, usersId.ToList());
        }

        public bool IsObjectValid()
        {
            if (current.Node == null)
            {
                return false;
            }

            if (notice.Channel != null)
            {
                if (notice.Channel.About != null)
                {
                    return notice.Channel.About.Length <= 1000;
                }
                if (notice.Channel.Photo != null)
                {
                    return notice.Channel.Photo.Length <= 300;
                }
                if (notice.Channel.ChannelName != null)
                {
                    return notice.Channel.ChannelName.Length > 0 && notice.Channel.ChannelName.Length < 50;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}