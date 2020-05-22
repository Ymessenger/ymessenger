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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class ChangeUsersChatNoticeHandler : ICommunicationHandler
    {
        private readonly ChangeUsersChatNodeNotice notice;
        private readonly NodeConnection node;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IUpdateChatsService updateChatsService;
        private readonly ILoadChatsService loadChatsService;
        private readonly INodeRequestSender nodeRequestSender;
        private readonly ICrossNodeService crossNodeService;
        private readonly ISystemMessagesService systemMessagesService;
        public ChangeUsersChatNoticeHandler(
            NodeNotice notice,
            NodeConnection node,
            IConversationsNoticeService conversationsNoticeService,
            IUpdateChatsService updateChatsService,
            ILoadChatsService loadChatsService,
            INodeRequestSender nodeRequestSender,
            ICrossNodeService crossNodeService,
            ISystemMessagesService systemMessagesService)
        {
            this.notice = (ChangeUsersChatNodeNotice)notice;
            this.node = node;
            this.conversationsNoticeService = conversationsNoticeService;
            this.updateChatsService = updateChatsService;
            this.loadChatsService = loadChatsService;
            this.nodeRequestSender = nodeRequestSender;
            this.crossNodeService = crossNodeService;
            this.systemMessagesService = systemMessagesService;
        }

        public async Task HandleAsync()
        {
            try
            {
                var chatUsers = await updateChatsService.EditChatUsersAsync(notice.ChatUsers, notice.ChatId, notice.RequestedUserId).ConfigureAwait(false);
                var bannedUsers = chatUsers.Where(opt => opt.Banned == true);
                if (!bannedUsers.IsNullOrEmpty())
                {
                    foreach (var user in bannedUsers)
                    {
                        var systemMessageInfo = SystemMessageInfoFactory.CreateUserBannedMessageInfo(user.UserId);
                        var message = await systemMessagesService.CreateMessageAsync(ConversationType.Chat, user.ChatId.Value, systemMessageInfo);
                        conversationsNoticeService.SendSystemMessageNoticeAsync(message);
                    }
                }
                var deletedUsers = chatUsers.Where(opt => opt.Deleted == true);
                if (!deletedUsers.IsNullOrEmpty())
                {
                    foreach (var user in deletedUsers)
                    {
                        var systemMessageInfo = SystemMessageInfoFactory.CreateUserRemovedMessageInfo(user.UserId);
                        var message = await systemMessagesService.CreateMessageAsync(ConversationType.Chat, user.ChatId.Value, systemMessageInfo);
                        conversationsNoticeService.SendSystemMessageNoticeAsync(message);
                    }
                }
            }
            catch (UserIsNotInConversationException)
            {
                ChatVm newChat = await nodeRequestSender.GetFullChatInformationAsync(notice.ChatId, node).ConfigureAwait(false);
                await crossNodeService.NewOrEditChatAsync(newChat).ConfigureAwait(false);
            }
            catch (ConversationNotFoundException ex)
            {
                ChatVm newChat = await nodeRequestSender.GetFullChatInformationAsync(ex.ConversationId, node).ConfigureAwait(false);
                await crossNodeService.NewOrEditChatAsync(newChat).ConfigureAwait(false);
            }
            conversationsNoticeService.SendChangeChatUsersNoticeAsync(notice.ChatUsers, notice.ChatId);
            ChatVm chat = await loadChatsService.GetChatByIdAsync(notice.ChatId).ConfigureAwait(false);
            BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateChangeUsersChatSegmetAsync(
                notice.ChatUsers,
                notice.ChatId,
                notice.RequestedUserId,
                node.Node.Id,
                chat.Type,
                NodeData.Instance.NodeKeys.SignPrivateKey,
                NodeData.Instance.NodeKeys.SymmetricKey,
                NodeData.Instance.NodeKeys.Password,
                NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
            BlockGenerationHelper.Instance.AddSegment(segment);
        }

        public bool IsObjectValid()
        {
            return notice.ChatId != 0
                && notice.RequestedUserId != 0
                && notice.ChatUsers.Any()
                && node.Node != null;
        }
    }
}
