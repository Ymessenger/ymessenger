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
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class ConversationActionNodeNoticeHandler : ICommunicationHandler
    {
        private readonly IConversationsService conversationsService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly ILoadDialogsService loadDialogsService;
        private readonly ISystemMessagesService systemMessagesService;
        private readonly ConversationActionNodeNotice notice;
        private readonly NodeConnection nodeConnection;
        public ConversationActionNodeNoticeHandler(
            NodeNotice notice,
            NodeConnection nodeConnection,
            IConversationsService conversationsService,
            IConversationsNoticeService conversationsNoticeService,
            ILoadDialogsService loadDialogsService,
            ISystemMessagesService systemMessagesService)
        {
            this.notice = (ConversationActionNodeNotice) notice;
            this.nodeConnection = nodeConnection;
            this.conversationsService = conversationsService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.loadDialogsService = loadDialogsService;
            this.systemMessagesService = systemMessagesService;
        }
        public async Task HandleAsync()
        {
            if (!await conversationsService.IsUserInConversationAsync(notice.ConversationType, notice.ConversationId, notice.UserId) && notice.DialogUserId == null)
            {
                return;
            }
            if (notice.ConversationType == ConversationType.Dialog)
            {
                var dialogs = await loadDialogsService.GetUsersDialogsAsync(notice.DialogUserId.Value, notice.UserId).ConfigureAwait(false);
                var senderDialog = dialogs.FirstOrDefault(opt => opt.FirstUserId == notice.UserId);
                if (notice.Action != ConversationAction.Screenshot)
                {
                    conversationsNoticeService.SendConversationActionNoticeAsync(notice.UserId, ConversationType.Dialog, senderDialog.Id, notice.Action);
                }
            }
            else
            {
                if (notice.Action != ConversationAction.Screenshot)
                {
                    conversationsNoticeService.SendConversationActionNoticeAsync(notice.UserId, notice.ConversationType, notice.ConversationId, notice.Action);
                }
            }
            if (notice.ConversationType != ConversationType.Channel && notice.Action == ConversationAction.Screenshot)
            {
                var systemMessageInfo = SystemMessageInfoFactory.CreateScreenshotMessageInfo(notice.UserId);
                var message = await systemMessagesService.CreateMessageAsync(notice.ConversationType, notice.ConversationId, systemMessageInfo).ConfigureAwait(false);
                conversationsNoticeService.SendSystemMessageNoticeAsync(message);
            }
        }

        public bool IsObjectValid()
        {
            return nodeConnection.Node != null;
        }
    }
}
