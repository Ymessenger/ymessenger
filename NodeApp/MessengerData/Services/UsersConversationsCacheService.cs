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
using Microsoft.EntityFrameworkCore;
using NodeApp.CacheStorageClasses;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class UsersConversationsCacheService
    {
        private const string DIALOG_KEY_FORMAT = "USER_DIALOG:{0}";
        private const string CHAT_KEY_FORMAT = "USER_CHAT:{0}";
        private const string CHANNEL_KEY_FORMAT = "USER_CHANNEL:{0}";
        private readonly ICacheRepository<ConversationPreviewVm> repository;
        private readonly ILoadDialogsService loadDialogsService;
        private readonly ILoadUsersService loadUsersService;
        private readonly ILoadChatsService loadChatsService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly IConversationsService conversationsService;

        private static readonly Lazy<UsersConversationsCacheService> lazy =
            new Lazy<UsersConversationsCacheService>(() => new UsersConversationsCacheService());
        private UsersConversationsCacheService()
        {
            loadDialogsService = AppServiceProvider.Instance.LoadDialogsService;
            loadUsersService = AppServiceProvider.Instance.LoadUsersService;
            loadChatsService = AppServiceProvider.Instance.LoadChatsService;
            loadChannelsService = AppServiceProvider.Instance.LoadChannelsService;
            conversationsService = AppServiceProvider.Instance.ConversationsService;
            repository = new RedisUserConversationsRepository(NodeSettings.Configs.CacheServerConnection);
        }
        public static UsersConversationsCacheService Instance => lazy.Value;

        public async Task<List<ConversationPreviewVm>> GetUserConversationsAsync(long userId, ConversationType conversationType)
        {
            switch (conversationType)
            {
                case ConversationType.Dialog:
                    return await GetUserDialogsAsync(userId).ConfigureAwait(false);
                case ConversationType.Chat:
                    return await GetUserChatsAsync(userId).ConfigureAwait(false);
                case ConversationType.Channel:
                    return await GetUserChannelsAsync(userId).ConfigureAwait(false);
                default:
                    throw new WrongArgumentException($"Unknown conversation type value {conversationType}.");
            }
        }

        public void UpdateUserConversations(long userId, IEnumerable<ConversationPreviewVm> conversations)
        {
            var groupingConversations = conversations.GroupBy(opt => opt.ConversationType);
            foreach (var group in groupingConversations)
            {
                switch (group.Key)
                {
                    case ConversationType.Dialog:
                        UpdateUserDialogs(userId, group);
                        break;
                    case ConversationType.Chat:
                        UpdateUserChats(userId, group);
                        break;
                    case ConversationType.Channel:
                        UpdateUserChannels(userId, group);
                        break;
                }
            }
        }

        private void UpdateUserChats(long userId, IEnumerable<ConversationPreviewVm> chats)
        {
            repository.UpdateObjects(string.Format(CHAT_KEY_FORMAT, userId), chats);
        }
        private void UpdateUserDialogs(long userId, IEnumerable<ConversationPreviewVm> dialogs)
        {
            repository.UpdateObjects(string.Format(DIALOG_KEY_FORMAT, userId), dialogs);
        }
        private void UpdateUserChannels(long userId, IEnumerable<ConversationPreviewVm> conversations)
        {
            repository.UpdateObjects(string.Format(CHANNEL_KEY_FORMAT, userId), conversations);
        }
        private async Task<List<ConversationPreviewVm>> GetUserDialogsAsync(long userId, long? limit = null)
        {
            var dialogs = await repository.GetObjects(string.Format(DIALOG_KEY_FORMAT, userId), limit.GetValueOrDefault(-1)).ConfigureAwait(false);
            if (dialogs == null)
            {
                return null;
            }

            return new List<ConversationPreviewVm>(dialogs);
        }
        private async Task<List<ConversationPreviewVm>> GetUserChatsAsync(long userId, long? limit = null)
        {
            var chats = await repository.GetObjects(string.Format(CHAT_KEY_FORMAT, userId), limit.GetValueOrDefault(-1)).ConfigureAwait(false);
            if (chats == null)
            {
                return null;
            }

            return new List<ConversationPreviewVm>(chats);
        }
        private async Task<List<ConversationPreviewVm>> GetUserChannelsAsync(long userId, long? limit = null)
        {
            var channels = await repository.GetObjects(string.Format(CHANNEL_KEY_FORMAT, userId), limit.GetValueOrDefault(-1)).ConfigureAwait(false);
            if (channels == null)
            {
                return null;
            }

            return new List<ConversationPreviewVm>(channels);
        }
        public async void NewMessageUpdateUserDialogsAsync(MessageVm message, long receiverDialogId)
        {
            try
            {
                List<ConversationPreviewVm> senderDialogs = (await GetUserDialogsAsync(message.SenderId.Value).ConfigureAwait(false))?.ToList();
                IEnumerable<ConversationPreviewVm> receiverDialogs = await GetUserDialogsAsync(message.ReceiverId.Value).ConfigureAwait(false);
                if (senderDialogs != null)
                {
                    ConversationPreviewVm changedSenderDialog = senderDialogs
                        .FirstOrDefault(opt => opt.ConversationId == message.ConversationId);
                    if (changedSenderDialog != null)
                    {
                        changedSenderDialog.LastMessageSenderId = message.SenderId;
                        changedSenderDialog.LastMessageTime = message.SendingTime;
                        changedSenderDialog.PreviewText = message.Text;
                        changedSenderDialog.AttachmentType = message.Attachments?.FirstOrDefault()?.Type;
                        changedSenderDialog.Read = false;
                        changedSenderDialog.LastMessageId = message.GlobalId;
                    }
                    else
                    {
                        senderDialogs = (await loadDialogsService.GetUserDialogsPreviewAsync(message.SenderId.Value).ConfigureAwait(false)).ToList();
                    }
                }
                else
                {
                    senderDialogs = (await loadDialogsService.GetUserDialogsPreviewAsync(message.SenderId.Value).ConfigureAwait(false)).ToList();
                }
                if (receiverDialogs != null)
                {
                    ConversationPreviewVm changedReceiverDialog = receiverDialogs
                        .FirstOrDefault(opt => opt.ConversationId == receiverDialogId);
                    if (changedReceiverDialog != null)
                    {
                        changedReceiverDialog.LastMessageSenderId = message.SenderId;
                        changedReceiverDialog.LastMessageTime = message.SendingTime;
                        changedReceiverDialog.PreviewText = message.Text;
                        changedReceiverDialog.AttachmentType = message.Attachments?.FirstOrDefault()?.Type;
                        changedReceiverDialog.Read = false;
                        changedReceiverDialog.LastMessageId = message.GlobalId;
                        receiverDialogs = receiverDialogs.Where(opt => opt.ConversationId != changedReceiverDialog.ConversationId);
                        receiverDialogs = receiverDialogs.Append(changedReceiverDialog);
                    }
                    else
                    {
                        receiverDialogs = await loadDialogsService.GetUserDialogsPreviewAsync(message.ReceiverId.Value).ConfigureAwait(false);
                    }
                }
                else
                {
                    receiverDialogs = await loadDialogsService.GetUserDialogsPreviewAsync(message.ReceiverId.Value).ConfigureAwait(false);
                }
                UpdateUserDialogs(message.SenderId.Value, senderDialogs.OrderByDescending(opt => opt.LastMessageTime));
                UpdateUserDialogs(message.ReceiverId.Value, receiverDialogs.OrderByDescending(opt => opt.LastMessageTime));
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void MessagesRemovedUpdateConversationsAsync(List<Message> removedMessages)
        {
            try
            {
                var chatsIds = removedMessages.Where(opt => opt.ChatId != null)?.Select(opt => opt.ChatId).Distinct();
                var channelsIds = removedMessages.Where(opt => opt.ChannelId != null)?.Select(opt => opt.ChannelId).Distinct();
                var dialogsIds = removedMessages.Where(opt => opt.DialogId != null)?.Select(opt => opt.DialogId).Distinct();
                using (MessengerDbContext context = new MessengerDbContext())
                {
                    if (chatsIds != null)
                    {
                        var chatsUsersIds = await context.ChatUsers
                            .AsNoTracking()
                            .Where(opt => chatsIds.Contains(opt.ChatId) && !opt.Banned && !opt.Deleted)
                            .Select(opt => opt.UserId)
                            .Distinct()
                            .ToListAsync()
                            .ConfigureAwait(false);
                        UpdateUsersChatsAsync(chatsUsersIds);
                    }
                    if (channelsIds != null)
                    {
                        var channelsUsersIds = await context.ChannelUsers
                            .AsNoTracking()
                            .Where(opt => channelsIds.Contains(opt.ChannelId) && !opt.Banned && !opt.Deleted)
                            .Select(opt => opt.UserId)
                            .Distinct()
                            .ToListAsync()
                            .ConfigureAwait(false);
                        UpdateUsersChannelsAsync(channelsUsersIds);
                    }
                    if (dialogsIds != null)
                    {
                        var dialogsUsersIds = await context.Dialogs
                            .AsNoTracking()
                            .Where(opt => dialogsIds.Contains(opt.Id))
                            .Select(opt => opt.FirstUID)
                            .Distinct()
                            .ToListAsync()
                            .ConfigureAwait(false);
                        UpdateUsersDialogsAsync(dialogsUsersIds);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async Task NewMessageUpdateUsersConversationsAsync(MessageVm message)
        {
            try
            {
                switch (message.ConversationType)
                {
                    case ConversationType.Dialog:
                        var dialogs = await loadDialogsService.GetUsersDialogsAsync(message.SenderId.Value, message.ReceiverId.Value).ConfigureAwait(false);
                        NewMessageUpdateUserDialogsAsync(message, dialogs.FirstOrDefault(opt => opt.FirstUserId == message.ReceiverId).Id);
                        break;
                    case ConversationType.Chat:
                        NewMessageUpdateUserChatsAsync(message);
                        break;
                    case ConversationType.Channel:
                        NewMessageUpdateUsersChannelsAsync(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void UpdateUsersDialogsAsync(IEnumerable<long> usersId)
        {
            try
            {
                foreach (long userId in usersId)
                {
                    var userDialogs = await loadDialogsService.GetUserDialogsPreviewAsync(userId).ConfigureAwait(false);
                    UpdateUserDialogs(userId, userDialogs);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async void UserEditedUpdateUserDialogsAsync(UserVm user)
        {
            try
            {
                var userDialogs = await loadDialogsService.GetUserDialogsPreviewAsync(user.Id.GetValueOrDefault()).ConfigureAwait(false);
                foreach (var dialog in userDialogs)
                {
                    IEnumerable<ConversationPreviewVm> secondUserDialogs = await GetUserDialogsAsync(dialog.SecondUid.GetValueOrDefault()).ConfigureAwait(false);
                    ConversationPreviewVm updatedDialog = secondUserDialogs?.FirstOrDefault(opt => opt.SecondUid == user.Id.GetValueOrDefault());
                    if (updatedDialog != null)
                    {
                        updatedDialog.Photo = user.Photo;
                        updatedDialog.Title = $"{user.NameFirst} {user.NameSecond}";
                        secondUserDialogs = secondUserDialogs.Where(opt => opt.ConversationId != updatedDialog.ConversationId).Append(updatedDialog);
                        UpdateUserDialogs(dialog.SecondUid.GetValueOrDefault(), secondUserDialogs);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async void NewMessageUpdateUserChatsAsync(MessageVm message)
        {
            try
            {
                UserVm senderInfo = await loadUsersService.GetUserAsync(message.SenderId.GetValueOrDefault()).ConfigureAwait(false);
                IEnumerable<long> usersId = await loadChatsService.GetChatUsersIdAsync(message.ConversationId.GetValueOrDefault()).ConfigureAwait(false);
                foreach (long userId in usersId)
                {
                    List<ConversationPreviewVm> cachedChats = (await GetUserChatsAsync(userId).ConfigureAwait(false))?.ToList();
                    if (cachedChats == null || cachedChats.All(opt => opt.ConversationId != message.ConversationId))
                    {
                        cachedChats = (await loadChatsService.GetUserChatPreviewAsync(userId, DateTime.UtcNow.ToUnixTime()).ConfigureAwait(false))?.ToList();
                        UpdateUserChats(userId, cachedChats);
                        continue;
                    }
                    ConversationPreviewVm updatedChat = cachedChats.FirstOrDefault(opt => opt.ConversationId == message.ConversationId);
                    if (updatedChat != null)
                    {
                        updatedChat.LastMessageSenderId = message.SenderId;
                        updatedChat.LastMessageTime = message.SendingTime;
                        updatedChat.LastMessageSenderName = senderInfo.NameFirst;
                        updatedChat.PreviewText = message.Text;
                        updatedChat.LastMessageId = message.GlobalId;
                        updatedChat.Read = false;
                        updatedChat.AttachmentType = (AttachmentType?)message.Attachments?.FirstOrDefault()?.Type ?? null;
                        UpdateUserChats(userId, cachedChats);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async void UpdateUsersChannelsAsync(IEnumerable<long> usersId)
        {
            try
            {
                foreach (long userId in usersId)
                {
                    var userChannels = await loadChannelsService.GetUserChannelsPreviewAsync(userId).ConfigureAwait(false);
                    UpdateUserChannels(userId, userChannels);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void UpdateUsersChatsAsync(IEnumerable<long> usersId)
        {
            try
            {
                foreach (long userId in usersId)
                {
                    var cachedChats = await loadChatsService.GetUserChatPreviewAsync(userId, DateTime.UtcNow.ToUnixTime()).ConfigureAwait(false);
                    UpdateUserChats(userId, cachedChats);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async void UpdateUsersDialogsAsync(long firstUser, long secondUser)
        {
            try
            {
                var firstUserDialogs = await loadDialogsService.GetUserDialogsPreviewAsync(firstUser).ConfigureAwait(false);
                var secondUserDialogs = await loadDialogsService.GetUserDialogsPreviewAsync(secondUser).ConfigureAwait(false);
                UpdateUserDialogs(firstUser, firstUserDialogs);
                UpdateUserDialogs(secondUser, secondUserDialogs);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async void NewMessageUpdateUsersChannelsAsync(MessageVm message)
        {
            try
            {
                var usersId = await loadChannelsService.GetChannelUsersIdAsync(message.ConversationId.GetValueOrDefault()).ConfigureAwait(false);
                foreach (long userId in usersId)
                {
                    List<ConversationPreviewVm> channels = (await GetUserChannelsAsync(userId).ConfigureAwait(false))?.ToList();
                    if (channels == null || channels.All(opt => opt.ConversationId != message.ConversationId))
                    {
                        channels = (await loadChannelsService.GetUserChannelsPreviewAsync(userId).ConfigureAwait(false))?.ToList();
                        UpdateUserChannels(userId, channels);
                        continue;
                    }
                    ConversationPreviewVm updatedChannel = channels.FirstOrDefault(opt => opt.ConversationId == message.ConversationId);
                    if (updatedChannel != null)
                    {
                        updatedChannel.LastMessageTime = message.SendingTime;
                        updatedChannel.PreviewText = message.Text;
                        updatedChannel.LastMessageId = message.GlobalId;
                        updatedChannel.Read = false;
                        updatedChannel.AttachmentType = message.Attachments?.FirstOrDefault()?.Type;
                        UpdateUserChannels(userId, channels);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void MessagesReadedUpdateConversations(IEnumerable<MessageVm> messages, long conversationId, ConversationType conversationType)
        {
            try
            {
                List<long> usersId = new List<long>();
                switch (conversationType)
                {
                    case ConversationType.Dialog:
                        usersId = (await loadDialogsService.GetDialogUsersAsync(conversationId).ConfigureAwait(false)).Select(opt => opt.Id.Value).ToList();
                        break;
                    case ConversationType.Chat:
                        usersId = await loadChatsService.GetChatUsersIdAsync(conversationId).ConfigureAwait(false);
                        break;
                    case ConversationType.Channel:
                        usersId = (await loadChannelsService.GetChannelUsersIdAsync(conversationId).ConfigureAwait(false)).ToList();
                        break;
                }
                var lastMessage = messages.OrderByDescending(opt => opt.SendingTime).FirstOrDefault();
                foreach (var userId in usersId)
                {
                    var userConversations = await GetUserConversationsAsync(userId, conversationType).ConfigureAwait(false);
                    if (userConversations != null)
                    {
                        var conversation = userConversations.FirstOrDefault(opt =>
                               opt.LastMessageId == lastMessage.GlobalId
                            && opt.LastMessageTime == lastMessage.SendingTime
                            && opt.LastMessageSenderId == lastMessage.SenderId);
                        if (conversation != null && lastMessage != null && conversation.LastMessageId == lastMessage.GlobalId)
                        {
                            conversation.Read = true;
                            UpdateUserConversations(userId, userConversations);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void MessageEditedUpdateConversations(MessageDto message)
        {
            try
            {
                List<long> usersId = new List<long>();
                switch (message.ConversationType)
                {
                    case ConversationType.Dialog:
                        usersId = (await loadDialogsService.GetDialogUsersAsync(message.ConversationId).ConfigureAwait(false)).Select(opt => opt.Id.Value).ToList();
                        break;
                    case ConversationType.Chat:
                        usersId = await loadChatsService.GetChatUsersIdAsync(message.ConversationId).ConfigureAwait(false);
                        break;
                    case ConversationType.Channel:
                        usersId = (await loadChannelsService.GetChannelUsersIdAsync(message.ConversationId).ConfigureAwait(false)).ToList();
                        break;
                }
                foreach (var userId in usersId)
                {
                    var userConversations = await GetUserConversationsAsync(userId, message.ConversationType).ConfigureAwait(false);
                    if (userConversations != null)
                    {
                        var conversation = userConversations.FirstOrDefault(opt => opt.ConversationId == message.ConversationId && opt.LastMessageId == message.GlobalId);
                        if (conversation != null)
                        {
                            conversation.PreviewText = message.Text;
                            conversation.AttachmentType = message.Attachments?.FirstOrDefault()?.Type;
                            UpdateUserConversations(userId, userConversations);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async void ConversationMutedUpdateConversations(long conversationId, ConversationType conversationType, long userId)
        {
            try
            {
                var userConversations = await GetUserConversationsAsync(userId, conversationType).ConfigureAwait(false);
                if (userConversations != null)
                {
                    var conversation = userConversations.FirstOrDefault(conv => conv.ConversationId == conversationId);
                    if (conversation == null)
                    {
                        userConversations = await conversationsService.GetUsersConversationsAsync(userId, conversationId, conversationType, 0).ConfigureAwait(false);
                        conversation = userConversations.FirstOrDefault(conv => conv.ConversationId == conversationId);
                    }
                    if (conversation != null)
                    {
                        conversation.IsMuted = !conversation.IsMuted;
                        UpdateUserConversations(userId, userConversations);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
    }
}