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
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class EditChatsNoticeHandler : ICommunicationHandler
    {
        private readonly CreateOrEditChatsNodeNotice notice;
        private readonly NodeConnection nodeConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly ILoadChatsService loadChatsService;
        private readonly ICrossNodeService crossNodeService;
        private readonly ISystemMessagesService systemMessagesService;
        public EditChatsNoticeHandler(
            CommunicationObject @object,
            NodeConnection nodeConnection,
            INodeNoticeService nodeNoticeService,
            IConversationsNoticeService conversationsNoticeService,
            ILoadChatsService loadChatsService,
            ICrossNodeService crossNodeService,
            ISystemMessagesService systemMessagesService)
        {
            notice = (CreateOrEditChatsNodeNotice)@object;
            this.nodeConnection = nodeConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.loadChatsService = loadChatsService;
            this.crossNodeService = crossNodeService;
            this.systemMessagesService = systemMessagesService;
        }
        public async Task HandleAsync()
        {
            foreach (var chat in notice.Chats)
            {
                var editableChat = await loadChatsService.GetChatByIdAsync(chat.Id.Value).ConfigureAwait(false);
                var editedChat = await crossNodeService.NewOrEditChatAsync(chat).ConfigureAwait(false);
                if (editableChat.Name != editedChat.Name)
                {
                    var systemMessageInfo = SystemMessageInfoFactory.CreateNameChangedMessageInfo(editableChat.Name, editedChat.Name);
                    var message = await systemMessagesService.CreateMessageAsync(ConversationType.Chat, editedChat.Id.Value, systemMessageInfo);
                    conversationsNoticeService.SendSystemMessageNoticeAsync(message);
                }
                List<BlockSegmentVm> segments =
                    await BlockSegmentsService.Instance.CreateEditPrivateChatSegmentsAsync(
                        editedChat,
                        nodeConnection.Node.Id,
                        NodeData.Instance.NodeKeys.SignPrivateKey,
                        NodeData.Instance.NodeKeys.SymmetricKey,
                        NodeData.Instance.NodeKeys.Password,
                        NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
                if (segments.Any())
                {
                    nodeNoticeService.SendBlockSegmentsNodeNoticeAsync(segments.ToList());
                    BlockGenerationHelper.Instance.AddSegments(segments);
                }
                editedChat.Users = null;
                conversationsNoticeService.SendEditChatNoticeAsync(editedChat, null);
                UsersConversationsCacheService.Instance.UpdateUsersChatsAsync(await loadChatsService.GetChatUsersIdAsync(editedChat.Id.GetValueOrDefault()).ConfigureAwait(false));
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
                if (chat.Id == 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}