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
using NodeApp.Converters;
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class MessagesReadNoticeHandler : ICommunicationHandler
    {
        private readonly MessagesReadNodeNotice notice;
        private readonly NodeConnection current;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IUpdateMessagesService updateMessagesService;
        private readonly IUpdateChatsService updateChatsService;
        private readonly ILoadDialogsService loadDialogsService;
        private readonly IUpdateChannelsService updateChannelsService;

        public MessagesReadNoticeHandler(NodeNotice notice,
                                         NodeConnection current,
                                         IConversationsNoticeService conversationsNoticeService,
                                         IUpdateMessagesService updateMessagesService,
                                         IUpdateChatsService updateChatsService,
                                         ILoadDialogsService loadDialogsService,
                                         IUpdateChannelsService updateChannelsService)
        {
            this.notice = (MessagesReadNodeNotice)notice;
            this.current = current;
            this.conversationsNoticeService = conversationsNoticeService;
            this.updateMessagesService = updateMessagesService;
            this.updateChatsService = updateChatsService;
            this.loadDialogsService = loadDialogsService;
            this.updateChannelsService = updateChannelsService;
        }

        public async Task HandleAsync()
        {
            switch (notice.ConversationType)
            {
                case ConversationType.Dialog:
                    {
                        await DialogMessageReadAsync().ConfigureAwait(false);
                    }
                    break;
                case ConversationType.Chat:
                    {
                        await ChatMessageReadAsync().ConfigureAwait(false);
                    }
                    break;
                case ConversationType.Channel:
                    {
                        await ChannelMessageReadAsync().ConfigureAwait(false);
                    }
                    break;
            }
        }
        public bool IsObjectValid()
        {
            return notice.MessagesId != null
                && notice.MessagesId.Any()
                && notice.ReaderUserId != 0
                && notice.ConversationOrUserId != 0
                && current.Node != null;
        }
        private async Task ChatMessageReadAsync()
        {
            List<MessageDto> readedMessages = await updateMessagesService.SetMessagesReadAsync(
                    notice.MessagesId,
                    notice.ConversationOrUserId,
                    ConversationType.Chat,
                    notice.ReaderUserId).ConfigureAwait(false);
            var lastMessage = readedMessages.OrderByDescending(opt => opt.SendingTime)
                       .Where(opt => opt.SenderId != notice.ReaderUserId)
                       .FirstOrDefault();
            conversationsNoticeService.SendMessagesReadedNoticeAsync(readedMessages, notice.ConversationOrUserId, ConversationType.Chat, notice.ReaderUserId);
            if (lastMessage != null)
            {
                await updateChatsService.UpdateLastReadedMessageIdAsync(lastMessage.GlobalId, notice.ReaderUserId, notice.ConversationOrUserId).ConfigureAwait(false);
            }
        }

        private async Task DialogMessageReadAsync()
        {
            List<MessageDto> readedMessages = await updateMessagesService.SetDialogMessagesReadByUsersIdAsync(
                    notice.MessagesId,
                    notice.ReaderUserId,
                    notice.ConversationOrUserId).ConfigureAwait(false);
            var receiverDialogs = await loadDialogsService.GetUsersDialogsAsync(notice.ReaderUserId, notice.ConversationOrUserId).ConfigureAwait(false);
            var currentDialog = receiverDialogs.FirstOrDefault(opt => opt.SecondUserId == notice.ReaderUserId);
            if (currentDialog != null)
            {
                conversationsNoticeService.SendMessagesReadedNoticeAsync(readedMessages, currentDialog.Id, ConversationType.Dialog, notice.ConversationOrUserId);
                UsersConversationsCacheService.Instance.MessagesReadedUpdateConversations(
                   MessageConverter.GetMessagesVm(readedMessages, null),
                    currentDialog.Id,
                    ConversationType.Dialog);
            }
        }

        private async Task ChannelMessageReadAsync()
        {
            List<MessageDto> readedMessages = await updateMessagesService.SetMessagesReadAsync(
                notice.MessagesId, notice.ConversationOrUserId, ConversationType.Channel, notice.ReaderUserId).ConfigureAwait(false);
            conversationsNoticeService.SendMessagesReadedNoticeAsync(
                readedMessages, notice.ConversationOrUserId, ConversationType.Channel, notice.ReaderUserId);
            var lastMessage = readedMessages.OrderByDescending(opt => opt.SendingTime).FirstOrDefault();
            if (lastMessage != null)
            {
                await updateChannelsService.UpdateChannelLastReadedMessageAsync(lastMessage, notice.ReaderUserId).ConfigureAwait(false);
            }

            UsersConversationsCacheService.Instance.MessagesReadedUpdateConversations(
                MessageConverter.GetMessagesVm(readedMessages, null),
                notice.ConversationOrUserId,
                ConversationType.Channel);
        }
    }
}