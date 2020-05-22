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
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class NewChatsNoticeHandler : ICommunicationHandler
    {
        private readonly CreateOrEditChatsNodeNotice notice;
        private readonly NodeConnection nodeConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly ICrossNodeService crossNodeService;
        public NewChatsNoticeHandler(
            CommunicationObject @object,
            NodeConnection nodeConnection,
            INodeNoticeService nodeNoticeService,
            IConversationsNoticeService conversationsNoticeService,
            ICrossNodeService crossNodeService)
        {
            notice = (CreateOrEditChatsNodeNotice)@object;
            this.nodeConnection = nodeConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.crossNodeService = crossNodeService;
        }
        public async Task HandleAsync()
        {
            foreach (var chat in notice.Chats)
            {
                await crossNodeService.NewOrEditChatAsync(chat).ConfigureAwait(false);
                conversationsNoticeService.SendNewChatNoticeAsync(chat, null);
                UsersConversationsCacheService.Instance.UpdateUsersChatsAsync(chat.Users.Select(opt => opt.UserId));
                List<BlockSegmentVm> segments =
                        await BlockSegmentsService.Instance.CreateNewPrivateChatSegmentsAsync(
                            chat,
                            NodeSettings.Configs.Node.Id,
                            NodeData.Instance.NodeKeys.SignPrivateKey,
                            NodeData.Instance.NodeKeys.SymmetricKey,
                            NodeData.Instance.NodeKeys.Password,
                            NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
                if (segments.Any())
                {
                    nodeNoticeService.SendBlockSegmentsNodeNoticeAsync(segments.ToList());
                    foreach (var segment in segments)
                    {
                        BlockGenerationHelper.Instance.AddSegment(segment);
                    }
                }
            }
        }

        public bool IsObjectValid()
        {
            if (notice.Chats == null || !notice.Chats.Any() || nodeConnection.Node == null)
            {
                return false;
            }

            foreach (var chat in notice.Chats)
            {
                if (chat.Id == 0 || string.IsNullOrWhiteSpace(chat.Name))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
