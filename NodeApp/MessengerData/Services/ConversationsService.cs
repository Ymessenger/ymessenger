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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.Entities;
using Npgsql;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class ConversationsService : IConversationsService
    {
        private ILoadChatsService LoadChatsService => appServiceProvider.LoadChatsService;
        private ILoadDialogsService LoadDialogsService => appServiceProvider.LoadDialogsService;
        private ILoadChannelsService LoadChannelsService => appServiceProvider.LoadChannelsService;
        private IConnectionsService ConnectionsService => appServiceProvider.ConnectionsService;
        private ILoadUsersService LoadUsersService => appServiceProvider.LoadUsersService;
        private IPrivacyService PrivacyService => appServiceProvider.PrivacyService;
        private INodeRequestSender NodeRequestSender => appServiceProvider.NodeRequestSender;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        private readonly IAppServiceProvider appServiceProvider;
        public ConversationsService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
            this.appServiceProvider = appServiceProvider;
        }
        public async Task<bool> IsUserInConversationAsync(ConversationType conversationType, long conversationId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                switch (conversationType)
                {
                    case ConversationType.Dialog:
                        {
                            return await context.Dialogs
                                .AnyAsync(opt => (opt.FirstUID == userId) && opt.Id == conversationId).ConfigureAwait(false);
                        }
                    case ConversationType.Chat:
                        {
                            return await context.ChatUsers
                                .AnyAsync(opt => opt.UserId == userId && opt.ChatId == conversationId && !opt.Deleted && !opt.Banned).ConfigureAwait(false);
                        }
                    case ConversationType.Channel:
                        {
                            return await context.ChannelUsers
                                .AnyAsync(opt => opt.UserId == userId && opt.ChannelId == conversationId && !opt.Deleted && !opt.Banned).ConfigureAwait(false);
                        }
                    default:
                        return false;
                }
            }
        }
        public async Task<List<long>> GetConversationNodesIdsAsync(ConversationType conversationType, long conversationId)
        {
            switch (conversationType)
            {
                case ConversationType.Dialog:
                    {
                        return await LoadDialogsService.GetDlalogNodesAsync(conversationId).ConfigureAwait(false);
                    }
                case ConversationType.Chat:
                    {
                        return await LoadChatsService.GetChatNodeListAsync(conversationId).ConfigureAwait(false);
                    }
                case ConversationType.Channel:
                    {
                        return await LoadChannelsService.GetChannelNodesIdAsync(conversationId).ConfigureAwait(false);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(conversationType));
            }
        }
        public async Task MuteConversationAsync(ConversationType conversationType, long conversationId, long? userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                if (userId != null && !await IsUserInConversationAsync(conversationType, conversationId, userId.Value).ConfigureAwait(false))
                {
                    throw new PermissionDeniedException();
                }

                switch (conversationType)
                {
                    case ConversationType.Dialog:
                        {
                            var dialog = await context.Dialogs.FirstOrDefaultAsync(option => option.Id == conversationId).ConfigureAwait(false);
                            dialog.IsMuted = !dialog.IsMuted;
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            var chatUser = await context.ChatUsers.FirstOrDefaultAsync(option => option.ChatId == conversationId).ConfigureAwait(false);
                            chatUser.IsMuted = !chatUser.IsMuted;
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            var channelUser = await context.ChannelUsers.FirstOrDefaultAsync(option => option.ChannelId == conversationId).ConfigureAwait(false);
                            channelUser.IsMuted = !channelUser.IsMuted;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(conversationType));
                }
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        public async Task<long> GetConversationNodeIdAsync(ConversationType conversationType, long conversationId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                switch (conversationType)
                {
                    case ConversationType.Chat:
                        {
                            var chat = await context.Chats
                                .Include(opt => opt.ChatUsers)
                                .ThenInclude(opt => opt.User)
                                .FirstOrDefaultAsync(opt => opt.Id == conversationId)
                                .ConfigureAwait(false);
                            var nodeId = chat.ChatUsers?.FirstOrDefault(opt => opt.UserRole == UserRole.Creator)?.User.NodeId;
                            if (nodeId != null)
                            {
                                return nodeId.Value;
                            }
                            if (!chat.NodesId.IsNullOrEmpty())
                            {
                                return chat.NodesId.FirstOrDefault(id => id != NodeSettings.Configs.Node.Id);
                            }
                            return NodeSettings.Configs.Node.Id;
                        }
                    case ConversationType.Channel:
                        {
                            var channel = await context.Channels
                                .Include(opt => opt.ChannelUsers)
                                .ThenInclude(opt => opt.User)
                                .FirstOrDefaultAsync(opt => opt.ChannelId == conversationId)
                                .ConfigureAwait(false);
                            var nodeId = channel.ChannelUsers?.FirstOrDefault(opt => opt.ChannelUserRole == ChannelUserRole.Creator)?.User.NodeId;
                            if (nodeId != null)
                            {
                                return nodeId.Value;
                            }
                            if (!channel.NodesId.IsNullOrEmpty())
                            {
                                return channel.NodesId.FirstOrDefault(id => id != NodeSettings.Configs.Node.Id);
                            }
                            return NodeSettings.Configs.Node.Id;
                        }
                    default: return NodeSettings.Configs.Node.Id;
                }
            }
        }
        public async void UpdateConversationLastMessageId(long conversationId, ConversationType conversationType, Guid messageId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    switch (conversationType)
                    {
                        case ConversationType.Dialog:
                            {
                                Dialog dialog = await context.Dialogs.FirstOrDefaultAsync(opt => opt.Id == conversationId).ConfigureAwait(false);
                                var oldLastMessage = await context.Messages.FirstOrDefaultAsync(opt => opt.DialogId == conversationId && opt.GlobalId == dialog.LastMessageGlobalId).ConfigureAwait(false);
                                var newLastMessage = await context.Messages.FirstOrDefaultAsync(opt => opt.DialogId == conversationId && opt.GlobalId == messageId).ConfigureAwait(false);
                                if (oldLastMessage == null || oldLastMessage.SendingTime < newLastMessage?.SendingTime)
                                {
                                    dialog.LastMessageId = newLastMessage.Id;
                                    dialog.LastMessageGlobalId = messageId;
                                    context.Update(dialog);
                                }
                            }
                            break;
                        case ConversationType.Chat:
                            {
                                Chat chat = await context.Chats.FirstOrDefaultAsync(opt => opt.Id == conversationId).ConfigureAwait(false);
                                var oldLastMessage = await context.Messages.FirstOrDefaultAsync(opt => opt.ChatId == conversationId && opt.GlobalId == chat.LastMessageGlobalId).ConfigureAwait(false);
                                var newLastMessage = await context.Messages.FirstOrDefaultAsync(opt => opt.ChatId == conversationId && opt.GlobalId == messageId).ConfigureAwait(false);
                                if (oldLastMessage == null || oldLastMessage.SendingTime < newLastMessage?.SendingTime)
                                {
                                    chat.LastMessageId = newLastMessage.Id;
                                    chat.LastMessageGlobalId = messageId;
                                    context.Update(chat);
                                }
                            }
                            break;
                        case ConversationType.Channel:
                            {
                                Channel channel = await context.Channels.FirstOrDefaultAsync(opt => opt.ChannelId == conversationId).ConfigureAwait(false);
                                var oldLastMessage = await context.Messages.FirstOrDefaultAsync(opt => opt.ChannelId == conversationId && opt.GlobalId == channel.LastMessageGlobalId).ConfigureAwait(false);
                                var newLastMessage = await context.Messages.FirstOrDefaultAsync(opt => opt.ChannelId == conversationId && opt.GlobalId == messageId).ConfigureAwait(false);
                                if (oldLastMessage == null || oldLastMessage.SendingTime < newLastMessage?.SendingTime)
                                {
                                    channel.LastMessageId = newLastMessage.Id;
                                    channel.LastMessageGlobalId = messageId;
                                    context.Update(channel);
                                }
                            }
                            break;
                    }
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async Task<List<ConversationPreviewVm>> GetUsersConversationsAsync(long userId, long? conversationId, ConversationType? conversationType, long requestId)
        {
            List<ConversationPreviewVm> conversations = new List<ConversationPreviewVm>();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(10000);
            conversations.AddRange(await GetUserConversationsAsync(userId).ConfigureAwait(false));
            List<long> dialogUsersId = conversations.Where(opt => opt.ConversationType == ConversationType.Dialog).Select(opt => opt.SecondUid.GetValueOrDefault()).ToList();
            List<long> chatMessagesSendersId = conversations.Where(opt => opt.ConversationType == ConversationType.Chat).Select(opt => opt.LastMessageSenderId.GetValueOrDefault()).ToList();
            List<UserVm> conversationsUsers = new List<UserVm>();
            conversationsUsers.AddRange(await LoadUsersService.GetUsersByIdAsync(chatMessagesSendersId.Concat(dialogUsersId)).ConfigureAwait(false));
            ConcurrentBag<UserVm> resultUsers = new ConcurrentBag<UserVm>();
            var groupsUsers = conversationsUsers.GroupBy(opt => opt.NodeId.GetValueOrDefault());
            List<Task> getUsersTasks = new List<Task>();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = new CancellationTokenSource(10000);
            foreach (var group in groupsUsers)
            {
                getUsersTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var nodeConnection = ConnectionsService.GetNodeConnection(group.Key);
                        if (nodeConnection != null)
                        {
                            await MetricsHelper.Instance.SetCrossNodeApiInvolvedAsync(requestId).ConfigureAwait(false);
                            List<UserVm> responseUsers = await NodeRequestSender.GetUsersInfoAsync(
                                group.Select(opt => opt.Id.GetValueOrDefault()).ToList(),
                                userId,
                                nodeConnection).ConfigureAwait(false);
                            resultUsers.AddRange(responseUsers);
                        }
                        else
                        {
                            resultUsers.AddRange(await PrivacyService.ApplyPrivacySettingsAsync(group, userId).ConfigureAwait(false));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex);
                    }
                }, cancellationTokenSource.Token));
            }
            await Task.WhenAll(getUsersTasks).ConfigureAwait(false);
            foreach (var conversation in conversations)
            {
                switch (conversation.ConversationType)
                {
                    case ConversationType.Dialog:
                        {
                            UserVm dialogUser = resultUsers.FirstOrDefault(opt => opt.Id == conversation.SecondUid);
                            conversation.Photo = dialogUser?.Photo;
                            conversation.Title = dialogUser?.GetUsername();
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            UserVm messageSender = resultUsers.FirstOrDefault(opt => opt.Id == conversation.LastMessageSenderId);
                            conversation.LastMessageSenderName = messageSender?.GetUsername();
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            conversation.LastMessageSenderId = null;
                            conversation.LastMessageSenderName = null;
                        }
                        break;
                }
            }
            var resultConversations = conversations.OrderByDescending(opt => opt.LastMessageTime).ToList();
            if (conversationId != null && conversationType != null)
            {
                resultConversations = resultConversations.SkipWhile(opt =>
                    !(opt.ConversationType == conversationType && opt.ConversationId == conversationId)).ToList();
                if (resultConversations.Any())
                {
                    resultConversations.RemoveAt(0);
                }
            }
            cancellationTokenSource.Dispose();
            return resultConversations.ToList();
        }
        private async Task<List<ConversationPreviewVm>> GetUserConversationsAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                NpgsqlParameter userIdParam = new NpgsqlParameter("userId", userId);
                var conversations = await context.ConversationPreview.FromSql("SELECT * FROM get_user_conversations(@userId)", userIdParam).ToListAsync().ConfigureAwait(false);
                return conversations.Select(opt => new ConversationPreviewVm
                {
                    Title = opt.Title,
                    AttachmentType = opt.AttachmentTypes.Select(type => (AttachmentType?)type).FirstOrDefault(),
                    ConversationId = opt.ConversationId,
                    ConversationType = opt.ConversationType,
                    IsMuted = opt.IsMuted,
                    LastMessageId = opt.LastMessageId,
                    LastMessageSenderId = opt.LastMessageSenderId,
                    LastMessageSenderName = opt.LastMessageSenderName,
                    LastMessageTime = opt.LastMessageTime,
                    Photo = opt.Photo,
                    PreviewText = opt.PreviewText,
                    Read = opt.Read,
                    SecondUid = opt.SecondUserId,
                    UnreadedCount = opt.UnreadedCount.GetValueOrDefault(),
                    AttachmentTypes = opt.AttachmentTypes?.Select(type => (AttachmentType)type).ToList()
                }).ToList();
            }
        }
    }
}