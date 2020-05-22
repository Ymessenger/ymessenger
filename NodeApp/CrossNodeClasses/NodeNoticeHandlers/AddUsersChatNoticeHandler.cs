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
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class AddUsersChatNoticeHandler : ICommunicationHandler
    {
        private readonly AddUsersChatNodeNotice notice;
        private readonly NodeConnection node;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IUpdateChatsService updateChatsService;
        private readonly INodeRequestSender nodeRequestSender;
        private readonly ICrossNodeService crossNodeService;
        private readonly ISystemMessagesService systemMessagesService;

        public AddUsersChatNoticeHandler(NodeNotice notice,
                                         NodeConnection node,
                                         IConversationsNoticeService conversationsNoticeService,
                                         IUpdateChatsService updateChatsService,
                                         INodeRequestSender nodeRequestSender,
                                         ICrossNodeService crossNodeService,
                                         ISystemMessagesService systemMessagesService)
        {
            this.notice = (AddUsersChatNodeNotice)notice;
            this.node = node;
            this.conversationsNoticeService = conversationsNoticeService;
            this.updateChatsService = updateChatsService;
            this.nodeRequestSender = nodeRequestSender;
            this.crossNodeService = crossNodeService;
            this.systemMessagesService = systemMessagesService;
        }

        public async Task HandleAsync()
        {
            ChatVm chat;
            try
            {
                chat = await updateChatsService.AddUsersToChatAsync(notice.UsersId, notice.ChatId, notice.RequestorId).ConfigureAwait(false);
            }
            catch (AddUserChatException)
            {
                chat = await nodeRequestSender.GetFullChatInformationAsync(notice.ChatId, node).ConfigureAwait(false);
                await crossNodeService.NewOrEditChatAsync(chat).ConfigureAwait(false);
            }
            catch (ConversationNotFoundException ex)
            {
                chat = await nodeRequestSender.GetFullChatInformationAsync(ex.ConversationId, node).ConfigureAwait(false);
                await crossNodeService.NewOrEditChatAsync(chat).ConfigureAwait(false);
                UsersConversationsCacheService.Instance.UpdateUsersChatsAsync(notice.UsersId);
            }
            foreach (var userId in notice.UsersId)
            {
                var message = await systemMessagesService.CreateMessageAsync(ConversationType.Chat, chat.Id.Value, SystemMessageInfoFactory.CreateUserAddedMessageInfo(userId));
                conversationsNoticeService.SendSystemMessageNoticeAsync(message);
            }
            conversationsNoticeService.SendNewUsersAddedToChatNoticeAsync(chat, null);
            BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateAddUsersChatSegmentAsync(
                notice.UsersId,
                notice.ChatId,
                notice.RequestorId,
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
                && notice.UsersId.Distinct().Any()
                && notice.RequestorId != 0
                && node.Node != null;
        }
    }
}
