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
using LinqKit;
using Microsoft.EntityFrameworkCore;
using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Chats
{
    public class UpdateChatsService : IUpdateChatsService
    {
        private readonly ICreateMessagesService createMessagesService;
        private readonly ILoadUsersService loadUsersService;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public UpdateChatsService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            createMessagesService = appServiceProvider.CreateMessagesService;
            loadUsersService = appServiceProvider.LoadUsersService;
            this.contextFactory = contextFactory;
        }
        public async Task<ChatVm> AddUsersToChatAsync(IEnumerable<long> usersId, long chatId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                using (var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    Chat chat = await context.Chats.FirstOrDefaultAsync(opt => opt.Id == chatId).ConfigureAwait(false);
                    if (chat == null)
                    {
                        throw new ConversationNotFoundException(chatId);
                    }
                    User requestingUser = await context.Users.FindAsync(userId).ConfigureAwait(false);
                    if (requestingUser == null || requestingUser.Deleted)
                    {
                        throw new AddUserChatException();
                    }
                    ChatUser chatUser = await context.ChatUsers
                           .FirstOrDefaultAsync(opt => opt.ChatId == chatId && opt.UserId == userId).ConfigureAwait(false);
                    List<ChatUserVm> addedUsers = new List<ChatUserVm>();
                    if (chat.Deleted)
                    {
                        throw new ConversationIsNotValidException();
                    }
                    if (chatUser != null && chatUser.Banned)
                    {
                        throw new ChatUserBlockedException();
                    }
                    if (usersId.Count() == 1 && usersId.FirstOrDefault() == userId)
                    {
                        if (chat.Type == (int)ChatType.Private)
                        {
                            throw new AddUserChatException();
                        }
                        if (chatUser == null)
                        {
                            chatUser = ChatUserConverter.GetNewChatUser(chatId, userId, null);
                            await context.AddAsync(chatUser).ConfigureAwait(false);
                        }
                        else if (chatUser.Deleted)
                        {
                            chatUser.Deleted = false;
                            chatUser.User = requestingUser;
                            context.Update(chatUser);
                        }
                        if (!chat.NodesId.Contains(NodeSettings.Configs.Node.Id))
                        {
                            createMessagesService.DownloadMessageHistoryAsync(chat.NodesId.FirstOrDefault(), chat.Id, ConversationType.Chat, null, false);
                        }
                        chat.NodesId = chat.NodesId.Append(requestingUser.NodeId.Value).Distinct().ToArray();
                        addedUsers.Add(ChatUserConverter.GetChatUserVm(chatUser));
                    }
                    else
                    {
                        if ((chatUser?.Deleted).GetValueOrDefault(true))
                        {
                            throw new AddUserChatException();
                        }

                        if (await loadUsersService.IsUserBlacklisted(userId, usersId).ConfigureAwait(false))
                        {
                            throw new UserBlockedException();
                        }

                        ExpressionStarter<User> usersCondition = PredicateBuilder.New<User>();
                        usersCondition = usersId.Aggregate(usersCondition,
                            (current, value) => current.Or(opt => opt.Id == value).Expand());
                        List<User> existingUsers = await context.Users
                            .AsNoTracking()
                            .Where(usersCondition)
                            .ToListAsync()
                            .ConfigureAwait(false);
                        ExpressionStarter<ChatUser> chatUsersCondition = PredicateBuilder.New<ChatUser>();
                        chatUsersCondition = existingUsers.Select(opt => opt.Id).Aggregate(chatUsersCondition,
                            (current, value) => current.Or(opt => opt.UserId == value && opt.ChatId == chatId).Expand());
                        List<ChatUser> validChatUsers = await context.ChatUsers
                            .Where(chatUsersCondition)
                            .Include(opt => opt.User)
                            .ToListAsync()
                            .ConfigureAwait(false);
                        foreach (ChatUser user in validChatUsers)
                        {
                            if (!user.Banned && user.Deleted)
                            {
                                user.Deleted = false;
                                addedUsers.Add(ChatUserConverter.GetChatUserVm(user));
                            }
                        }
                        context.UpdateRange(validChatUsers);
                        List<long> newChatUsersId = existingUsers.Select(opt => opt.Id).Except(validChatUsers.Select(opt => opt.UserId)).ToList();
                        List<ChatUser> newChatUsers = newChatUsersId.Select(id => ChatUserConverter.GetNewChatUser(chatId, id, userId)).ToList();
                        chat.NodesId = chat.NodesId?.Concat(existingUsers.Select(opt => opt.NodeId.GetValueOrDefault())).Distinct().ToArray()
                            ?? existingUsers.Select(opt => opt.NodeId.GetValueOrDefault()).Distinct().ToArray();
                        await context.ChatUsers
                            .AddRangeAsync(newChatUsers)
                            .ConfigureAwait(false);
                        /* foreach (ChatUser user in newChatUsers)
                         {
                             user.User = existingUsers.FirstOrDefault(opt => opt.Id == user.UserId);
                         }*/
                        addedUsers.AddRange(ChatUserConverter.GetChatUsersVm(newChatUsers));
                    }
                    context.Update(chat);
                    transaction.Commit();
                    await context.SaveChangesAsync()
                        .ConfigureAwait(false);
                    ChatVm resultChat = ChatConverter.GetChatVm(chat);
                    resultChat.Users = addedUsers;
                    return resultChat;
                }
            }
        }
        public async Task<ChatVm> EditChatAsync(EditChatVm editChat, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                IQueryable<Chat> query = from chat in context.Chats
                                         join chatUser in context.ChatUsers on chat.Id equals chatUser.ChatId
                                         join chatUsers in context.ChatUsers on chat.Id equals chatUsers.ChatId
                                         where chat.Id == editChat.Id
                                             && chatUser.UserId == userId
                                             && chatUser.UserRole >= UserRole.Admin
                                             && chat.Deleted == false
                                             && chatUser.Banned == false
                                             && chatUser.Deleted == false
                                             && chatUsers.Banned == false
                                             && chatUsers.Deleted == false
                                         select chat;
                Chat targetChat = await query
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
                if (targetChat != null)
                {
                    targetChat = ChatConverter.GetChat(targetChat, editChat);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return ChatConverter.GetChatVm(targetChat);
                }
                throw new PermissionDeniedException();
            }
        }
        public async Task UpdateLastReadedMessageIdAsync(Guid messageId, long userId, long chatId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    var targetChatUser = await context.ChatUsers
                    .Where(chatUser => chatUser.ChatId == chatId && chatUser.UserId == userId)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
                    var updatingMessage = await context.Messages.FirstOrDefaultAsync(opt => opt.ChatId == chatId && opt.GlobalId == messageId).ConfigureAwait(false);
                    var oldMessage = await context.Messages.FirstOrDefaultAsync(opt => opt.ChatId == chatId && opt.GlobalId == targetChatUser.LastReadedGlobalMessageId).ConfigureAwait(false);
                    if (oldMessage != null && oldMessage.SendingTime > updatingMessage.SendingTime)
                    {
                        return;
                    }

                    targetChatUser.LastReadedGlobalMessageId = messageId;
                    context.Update(targetChatUser);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async Task<List<ChatUserVm>> EditChatUsersAsync(IEnumerable<ChatUserVm> users, long chatId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                ExpressionStarter<ChatUser> usersCondition = PredicateBuilder.New<ChatUser>();
                usersCondition = users.Select(chatUser => chatUser.UserId).Aggregate(usersCondition,
                    (current, value) => current.Or(chatUser => chatUser.UserId == value).Expand());
                Chat currentChat = await context.Chats.FindAsync(chatId).ConfigureAwait(false);
                if (currentChat == null || currentChat.Deleted)
                {
                    throw new ConversationNotFoundException();
                }
                List<ChatUser> chatUsers = await context.ChatUsers
                    .Where(usersCondition)
                    .Where(chatUser => chatUser.ChatId == chatId)
                    .ToListAsync()
                    .ConfigureAwait(false);
                List<ChatUserVm> editedChatUsers = new List<ChatUserVm>();
                ChatUser requestUser = await context.ChatUsers
                    .FirstOrDefaultAsync(opt => opt.UserId == userId && opt.ChatId == chatId).ConfigureAwait(false);
                if (requestUser != null && requestUser.Banned)
                {
                    throw new ChatUserBlockedException();
                }
                if (requestUser == null || !chatUsers.Any())
                {
                    throw new UserIsNotInConversationException();
                }
                foreach (ChatUser user in chatUsers)
                {
                    ChatUserVm changedChatUser = users.FirstOrDefault(chatUser => user.UserId == chatUser.UserId);
                    if (changedChatUser != null)
                    {
                        if (requestUser.UserRole > changedChatUser.UserRole
                            && requestUser.UserRole > user.UserRole
                            && changedChatUser.UserRole < UserRole.Creator)
                        {
                            user.UserRole = changedChatUser.UserRole ?? user.UserRole;
                            user.Deleted = changedChatUser.Deleted ?? user.Deleted;
                            user.Banned = changedChatUser.Banned ?? user.Banned;
                            editedChatUsers.Add(ChatUserConverter.GetChatUserVm(user));
                        }
                        else if (requestUser.UserId == changedChatUser.UserId)
                        {
                            user.Deleted = changedChatUser.Deleted ?? user.Deleted;
                            editedChatUsers.Add(ChatUserConverter.GetChatUserVm(user));
                        }
                        else
                        {
                            throw new EditConversationUsersException();
                        }
                    }
                }
                if (users.Any(opt => opt.Deleted.GetValueOrDefault() || opt.Banned.GetValueOrDefault()))
                {
                    var query = from chat in context.Chats
                                join chatUser in context.ChatUsers on chat.Id equals chatUser.ChatId
                                join user in context.Users on chatUser.UserId equals user.Id
                                where chat.Id == chatId && !chatUser.Banned && !chatUser.Deleted
                                select user.NodeId;
                    var chatNodesId = (await query.ToListAsync().ConfigureAwait(false)).Distinct();
                    currentChat.NodesId = chatNodesId.Select(id => id.GetValueOrDefault()).ToArray();
                }
                await context.SaveChangesAsync().ConfigureAwait(false);
                return editedChatUsers;
            }
        }
    }
}
