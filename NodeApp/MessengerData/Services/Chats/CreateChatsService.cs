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

namespace NodeApp.MessengerData.Services.Chats
{
    public class CreateChatsService : ICreateChatsService
    {
        private readonly IPoolsService poolsService;
        private readonly ILoadUsersService loadUsersService;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public CreateChatsService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.poolsService = appServiceProvider.PoolsService;
            this.loadUsersService = appServiceProvider.LoadUsersService;
            this.contextFactory = contextFactory;
        }
        public async Task<ChatVm> CreateChatAsync(ChatVm chatVm, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                List<ChatUser> chatUsers = new List<ChatUser>();
                using (var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    if (chatVm.Users != null && await loadUsersService.IsUserBlacklisted(userId, chatVm.Users.Select(opt => opt.UserId)).ConfigureAwait(false))
                    {
                        throw new UserBlockedException();
                    }
                    Chat newChat = ChatConverter.GetChat(chatVm);
                    newChat.Tag = RandomExtensions.NextString(10, "QWERTYUIOPASDFGHJKLZXCVBNM1234567890");
                    newChat.Id = await poolsService.GetChatIdAsync().ConfigureAwait(false);
                    if (chatVm.Users != null)
                    {
                        chatUsers.AddRange(chatVm.Users.Where(opt => opt.UserId != userId).Select(chatUser => new ChatUser
                        {
                            Banned = chatUser.Banned.GetValueOrDefault(false),
                            Deleted = chatUser.Deleted.GetValueOrDefault(false),
                            Joined = DateTime.UtcNow.ToUnixTime(),
                            UserId = chatUser.UserId,
                            UserRole = chatUser.UserRole.GetValueOrDefault(UserRole.User),
                            InviterId = userId
                        }));
                    }
                    if (!chatUsers.Any(opt => opt.UserId == userId))
                    {
                        chatUsers.Add(new ChatUser
                        {
                            Banned = false,
                            Deleted = false,
                            Joined = DateTime.UtcNow.ToUnixTime(),
                            UserId = userId,
                            UserRole = UserRole.Creator
                        });
                    }
                    newChat.ChatUsers = chatUsers;
                    IEnumerable<UserVm> users = await loadUsersService.GetUsersByIdAsync(chatUsers.Select(chatUser => chatUser.UserId)).ConfigureAwait(false);
                    IEnumerable<long> nodesId = users.Select(user => user.NodeId ?? 0).Distinct();
                    newChat.NodesId = nodesId.ToArray();
                    await context.Chats.AddAsync(newChat).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    transaction.Commit();
                    newChat.ChatUsers = chatUsers;
                    return ChatConverter.GetChatVm(newChat);
                }
            }
        }
        public async Task<List<ChatDto>> CreateOrUpdateUserChatsAsync(List<ChatDto> userChats)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var chatsCondition = PredicateBuilder.New<Chat>();
                chatsCondition = userChats.Aggregate(chatsCondition,
                    (current, value) => current.Or(opt => opt.Id == value.Id).Expand());
                List<ChatDto> resultChats = new List<ChatDto>();
                List<Chat> existingChats = await context.Chats
                    .Where(chatsCondition)
                    .ToListAsync()
                    .ConfigureAwait(false);
                if (existingChats.Any())
                {
                    for (int i = 0; i < existingChats.Count; i++)
                    {
                        var editedChat = userChats.FirstOrDefault(opt => opt.Id == existingChats[i].Id);
                        editedChat.ChatUsers = null;
                        existingChats[i] = ChatConverter.GetChat(existingChats[i], editedChat);
                    }
                    context.Chats.UpdateRange(existingChats);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    resultChats.AddRange(ChatConverter.GetChatsDto(existingChats));
                }
                List<ChatDto> nonExistingChats = userChats.Where(chat => !existingChats.Any(opt => opt.Id == chat.Id))?.ToList();
                if (nonExistingChats != null && nonExistingChats.Any())
                {
                    List<Chat> newChats = nonExistingChats.Select(chat => ChatConverter.GetChat(null, chat))?.ToList();
                    await context.Chats.AddRangeAsync(newChats).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    resultChats.AddRange(ChatConverter.GetChatsDto(newChats));
                }
                return resultChats;
            }
        }
    }
}
