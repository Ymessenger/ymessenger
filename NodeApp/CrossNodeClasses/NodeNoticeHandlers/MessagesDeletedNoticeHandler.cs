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
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Services;
using ObjectsLibrary.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class MessagesDeletedNoticeHandler : ICommunicationHandler
    {
        private readonly MessagesDeletedNodeNotice notice;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IDeleteMessagesService deleteMessagesService;
        private readonly ILoadChatsService loadChatsService;
        private readonly ILoadDialogsService loadDialogsService;
        private readonly ILoadChannelsService loadChannelsService;

        public MessagesDeletedNoticeHandler(
            NodeNotice notice,
            IConversationsNoticeService conversationsNoticeService,
            IDeleteMessagesService deleteMessagesService,
            ILoadChatsService loadChatsService,
            ILoadDialogsService loadDialogsService,
            ILoadChannelsService loadChannelsService)
        {
            this.notice = (MessagesDeletedNodeNotice)notice;
            this.conversationsNoticeService = conversationsNoticeService;
            this.deleteMessagesService = deleteMessagesService;
            this.loadChatsService = loadChatsService;
            this.loadDialogsService = loadDialogsService;
            this.loadChannelsService = loadChannelsService;
        }

        public async Task HandleAsync()
        {
            switch (notice.ConversationType)
            {
                case ConversationType.Dialog:
                    {
                        var dialogs = await loadDialogsService.GetUsersDialogsAsync(
                            notice.Messages.FirstOrDefault().SenderId.GetValueOrDefault(),
                            notice.Messages.FirstOrDefault().ReceiverId.GetValueOrDefault()).ConfigureAwait(false);
                        if (!dialogs.Any())
                        {
                            await deleteMessagesService.DeleteForwardedDialogMessagesAsync(notice.Messages).ConfigureAwait(false);
                            return;
                        }
                        List<MessageDto> messages = await deleteMessagesService.DeleteMessagesInfoAsync(
                            dialogs.FirstOrDefault(dialog => dialog.FirstUserId == notice.RequestorId).Id,
                            ConversationType.Dialog,
                            notice.Messages.Select(opt => opt.GlobalId.GetValueOrDefault()),
                            notice.RequestorId).ConfigureAwait(false);                        
                        conversationsNoticeService.SendMessagesUpdatedNoticeAsync(
                            dialogs.FirstOrDefault(dialog => dialog.FirstUserId == notice.RequestorId).Id,
                            ConversationType.Dialog,
                            messages,
                            notice.RequestorId,
                            true,
                            null);
                        UsersConversationsCacheService.Instance.UpdateUsersDialogsAsync(
                            notice.Messages.FirstOrDefault().SenderId.GetValueOrDefault(),
                            notice.Messages.FirstOrDefault().ReceiverId.GetValueOrDefault());
                    }
                    break;
                case ConversationType.Chat:
                    {
                        List<MessageDto> messages = await deleteMessagesService.DeleteMessagesInfoAsync(
                            notice.ConversationId.GetValueOrDefault(),
                            ConversationType.Chat,
                            notice.Messages.Select(opt => opt.GlobalId.GetValueOrDefault()),
                            notice.RequestorId).ConfigureAwait(false);
                        conversationsNoticeService.SendMessagesUpdatedNoticeAsync(
                            notice.ConversationId.GetValueOrDefault(),
                            ConversationType.Chat,
                            messages,
                            notice.RequestorId,
                            true,
                            null);
                        UsersConversationsCacheService.Instance.UpdateUsersChatsAsync(
                            await loadChatsService.GetChatUsersIdAsync(notice.ConversationId.GetValueOrDefault()).ConfigureAwait(false));
                    }
                    break;
                case ConversationType.Channel:
                    {
                        List<MessageDto> messages = await deleteMessagesService.DeleteMessagesInfoAsync(
                            notice.ConversationId.GetValueOrDefault(),
                            ConversationType.Channel,
                            notice.Messages.Select(opt => opt.GlobalId.GetValueOrDefault()),
                            notice.RequestorId).ConfigureAwait(false);
                        conversationsNoticeService.SendMessagesUpdatedNoticeAsync(
                            notice.ConversationId.GetValueOrDefault(),
                            ConversationType.Channel,
                            messages,
                            notice.RequestorId,
                            true,
                            null);
                        UsersConversationsCacheService.Instance.UpdateUsersChannelsAsync(
                            await loadChannelsService.GetChannelUsersIdAsync(notice.ConversationId.GetValueOrDefault()).ConfigureAwait(false));
                    }
                    break;
            }
        }

        public bool IsObjectValid()
        {
            if (notice.ConversationType == ConversationType.Chat && notice.ConversationId == null)
            {
                return false;
            }
            if (notice.Messages == null || !notice.Messages.Any())
            {
                return false;
            }
            return true;
        }
    }
}