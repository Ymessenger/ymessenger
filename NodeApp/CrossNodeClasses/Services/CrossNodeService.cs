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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.Services
{
    public class CrossNodeService : ICrossNodeService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public CrossNodeService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task NewOrEditUserAsync(ShortUser shortUser, long nodeId = 0)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                try
                {
                    var user = await context.Users.FindAsync(shortUser.UserId).ConfigureAwait(false);
                    if (user == null)
                    {
                        user = new User
                        {
                            Id = shortUser.UserId,
                            NodeId = nodeId,
                            Confirmed = true
                        };
                        await context.Users.AddAsync(user).ConfigureAwait(false);
                    }
                    else
                    {
                        user.NodeId = nodeId;
                        user.Confirmed = true;
                        context.Update(user);
                    }
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex);
                }
            }
        }

        public async Task CreateNewUsersAsync(List<UserVm> users)
        {
            try
            {
                var usersCondition = PredicateBuilder.New<User>();
                usersCondition = users.Aggregate(usersCondition,
                    (current, value) => current.Or(opt => opt.Id == value.Id).Expand());
                using (MessengerDbContext context = contextFactory.Create())
                {
                    List<User> existingUsers = await context.Users.Where(usersCondition).ToListAsync().ConfigureAwait(false);
                    List<UserVm> nonExistingUsers = users.Where(opt => !existingUsers.Any(p => p.Id == opt.Id)).ToList();
                    List<User> newUsers = nonExistingUsers.Select(user => new User
                    {
                        Id = user.Id.GetValueOrDefault(),
                        Tag = user.Tag,
                        NameFirst = user.NameFirst,
                        NodeId = user.NodeId.GetValueOrDefault(),
                        Confirmed = true

                    }).ToList();
                    await context.AddRangeAsync(newUsers).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async Task NewOrEditNodeAsync(NodeVm targetNode)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    var node = await context.Nodes.FindAsync(targetNode.Id).ConfigureAwait(false);
                    if (node == null)
                    {
                        node = NodeConverter.GetNode(targetNode);
                        await context.AddAsync(node).ConfigureAwait(false);
                    }
                    else
                    {
                        node = NodeConverter.GetNode(targetNode);
                        context.Update(node);
                    }
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async Task<ChatVm> NewOrEditChatAsync(ChatVm targetChat)
        {
            if (targetChat == null)
            {
                return null;
            }

            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    var query = from chat in context.Chats
                                where chat.Id == targetChat.Id
                                select chat;
                    Chat editableChat = await query.Include(opt => opt.ChatUsers).FirstOrDefaultAsync().ConfigureAwait(false);
                    if (editableChat == null)
                    {
                        editableChat = ChatConverter.GetChat(targetChat);
                        if (targetChat.Users != null)
                        {
                            editableChat.ChatUsers = ChatUserConverter.GetChatUsers(targetChat.Users).ToList();
                        }

                        await context.AddAsync(editableChat).ConfigureAwait(false);
                    }
                    else
                    {
                        editableChat = ChatConverter.GetChat(editableChat, new EditChatVm
                        {
                            About = targetChat.About,
                            Name = targetChat.Name,
                            Photo = targetChat.Photo,
                            Public = targetChat.Public,
                            Security = targetChat.Security,
                            Visible = targetChat.Visible
                        });
                        if (!targetChat.Users.IsNullOrEmpty())
                        {
                            editableChat.ChatUsers = ChatUserConverter.GetChatUsers(targetChat.Users);
                        }

                        context.Update(editableChat);
                    }
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return ChatConverter.GetChatVm(editableChat);
                }
            }
            catch (DbUpdateException ex)
            {
                Logger.WriteLog(ex);
                return targetChat;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return null;

            }
        }

        public async Task AddChatUsersAsync(IEnumerable<ChatUserVm> chatUsers)
        {
            try
            {
                var newChatUsers = ChatUserConverter.GetChatUsers(chatUsers);
                using (MessengerDbContext context = contextFactory.Create())
                {
                    await context.ChatUsers.AddRangeAsync(newChatUsers).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async Task AddUsersToChatAsync(long chatId, IEnumerable<long> usersId, long requestorId)
        {
            try
            {
                List<ChatUser> chatUsers = new List<ChatUser>();
                foreach (long userId in usersId)
                {
                    chatUsers.Add(ChatUserConverter.GetNewChatUser(chatId, userId, requestorId));
                }
                using (MessengerDbContext context = contextFactory.Create())
                {
                    await context.AddRangeAsync(chatUsers).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async Task ChangeChatUsersAsync(IEnumerable<ChatUserVm> changedChatUsers, long chatId, long requestedUserId)
        {
            await AppServiceProvider.Instance.UpdateChatsService.EditChatUsersAsync(changedChatUsers, chatId, requestedUserId).ConfigureAwait(false);
        }

        public async Task DeleteFileAsync(long fileId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                FileInfo fileInfo = await context.FilesInfo.FindAsync(fileId).ConfigureAwait(false);
                if (fileInfo != null)
                {
                    fileInfo.Deleted = true;
                    context.Update(fileInfo);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task DeleteChatAsync(long chatId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                Chat chat = await context.Chats.FindAsync(chatId).ConfigureAwait(false);
                if (chat != null)
                {
                    chat.Deleted = true;
                    context.Update(chat);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task DeleteUserAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                User user = await context.Users.FindAsync(userId).ConfigureAwait(false);
                if (user != null)
                {
                    user.Deleted = true;
                    context.Update(user);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }
    }
}