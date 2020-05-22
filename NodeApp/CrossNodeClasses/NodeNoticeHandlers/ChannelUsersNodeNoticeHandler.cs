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
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class ChannelUsersNodeNoticeHandler : ICommunicationHandler
    {
        private readonly ChannelUsersNodeNotice notice;
        private readonly NodeConnection current;
        private readonly ICreateChannelsService createChannelsService;
        private readonly INodeRequestSender nodeRequestSender;
        private readonly ICrossNodeService crossNodeService;
        public ChannelUsersNodeNoticeHandler(NodeNotice notice, NodeConnection current, ICreateChannelsService createChannelsService, INodeRequestSender nodeRequestSender, ICrossNodeService crossNodeService)
        {
            this.notice = (ChannelUsersNodeNotice)notice;
            this.current = current;
            this.createChannelsService = createChannelsService;
            this.nodeRequestSender = nodeRequestSender;
            this.crossNodeService = crossNodeService;
        }

        public async Task HandleAsync()
        {
            bool hasException = true;
            List<ChannelUserVm> channelUsers = null;
            while (hasException)
            {
                try
                {
                    channelUsers = await createChannelsService.CreateOrEditChannelUsersAsync(notice.ChannelUsers, notice.RequestorId).ConfigureAwait(false);
                    hasException = false;
                }
                catch (UserNotFoundException)
                {
                    List<long> usersId = notice.ChannelUsers.Select(opt => opt.UserId).Append(notice.RequestorId).ToList();
                    List<UserVm> users = await nodeRequestSender.GetUsersInfoAsync(usersId, null, current).ConfigureAwait(false);
                    await crossNodeService.CreateNewUsersAsync(users).ConfigureAwait(false);
                    hasException = true;
                }
                catch (ConversationNotFoundException)
                {
                    ChannelDto channel = await nodeRequestSender.GetChannelInformationAsync(notice.ChannelId, current).ConfigureAwait(false);
                    await createChannelsService.CreateOrUpdateUserChannelsAsync(new List<ChannelDto> { channel }).ConfigureAwait(false);
                    hasException = true;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex);
                    hasException = false;
                }
            }
            BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateChannelUsersSegmentAsync(
                channelUsers,
                current.Node.Id,
                notice.ChannelId,
                NodeData.Instance.NodeKeys.SignPrivateKey,
                NodeData.Instance.NodeKeys.SymmetricKey,
                NodeData.Instance.NodeKeys.Password,
                NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
            BlockGenerationHelper.Instance.AddSegment(segment);
        }

        public bool IsObjectValid()
        {
            return notice.ChannelUsers != null && notice.ChannelUsers.Any();
        }
    }
}