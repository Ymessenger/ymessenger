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
using NodeApp.Helpers;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Chats
{
    public class LoadChatsService : ILoadChatsService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public LoadChatsService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task<List<ChatVm>> FindChatsByStringQueryAsync(string searchQuery, long? navigationId = 0, bool? direction = true, long? userId = null)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                ExpressionsHelper helper = new ExpressionsHelper();
                var searchExpression = helper.GetChatExpression(searchQuery);
                IQueryable<Chat> query = context.Chats
                    .Where(opt => !opt.Deleted)
                    .AsNoTracking();
                if (userId == null)
                {
                    query = query.Where(opt => (opt.Type == (short)ChatType.Public));
                }
                else
                {
                    query = query.Where(opt => opt.ChatUsers
                        .Any(chatUser => chatUser.UserId == userId && !chatUser.Banned && !chatUser.Deleted) || (opt.Type == (short)ChatType.Public));
                }

                query = query.Where(searchExpression);
                if (direction.GetValueOrDefault() == true)
                {
                    query = query.OrderBy(opt => opt.Id)
                        .Where(opt => opt.Id > navigationId.GetValueOrDefault());
                }
                else
                {
                    query = query.OrderByDescending(opt => opt.Id)
                        .Where(opt => opt.Id < navigationId.GetValueOrDefault());
                }
                List<Chat> chats = await query.ToListAsync().ConfigureAwait(false);
                return ChatConverter.GetChatsVm(chats);
            }
        }
        public async Task<List<ChatDto>> GetUserChatsAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var query = from chat in context.Chats
                            join chatUser in context.ChatUsers on chat.Id equals chatUser.ChatId
                            where chatUser.UserId == userId
                                && !chatUser.Deleted
                                && !chatUser.Banned
                                && !chat.Deleted
                            select chat;
                var chats = await query.AsNoTracking()
                    .Include(opt => opt.ChatUsers)
                    .Include(opt => opt.Messages)
                        .ThenInclude(opt => opt.Attachments)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return ChatConverter.GetChatsDto(chats);
            }
        }
        public async Task<List<long>> GetUserChatsIdAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return await context.ChatUsers
                .Where(chatUser => !chatUser.Banned && !chatUser.Deleted && chatUser.UserId == userId)
                .Select(chatUser => chatUser.ChatId)
                .ToListAsync()
                .ConfigureAwait(false);
            }
        }
        public async Task<List<ChatVm>> FindChatsAsync(SearchChatVm template, int limit = 100, long navigationChatId = 0, long? nodeId = null)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                IQueryable<Chat> query;
                if (template != null)
                {
                    ExpressionsHelper expressionsHelper = new ExpressionsHelper();
                    query = context.Chats
                        .AsNoTracking()
                        .OrderBy(chat => chat.Id)
                        .Where(expressionsHelper.GetChatExpression(template))
                        .Take(limit);
                }
                else
                {
                    query = context.Chats
                        .AsNoTracking()
                        .OrderBy(chat => chat.Id)
                        .Take(limit);
                }
                if (nodeId != null)
                {
                    query = query.Where(chat => chat.NodesId.Contains(nodeId.Value));
                }
                List<Chat> result = await query
                    .Where(chat => chat.Deleted == false && chat.Id > navigationChatId && (ChatType)chat.Type != ChatType.Private)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return ChatConverter.GetChatsVm(result);
            }
        }
        public async Task<ChatVm> GetChatByIdAsync(long chatId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return ChatConverter.GetChatVm(await context.Chats
                .AsNoTracking()
                .Include(chat => chat.ChatUsers)
                .FirstOrDefaultAsync(chat => chat.Id == chatId)
                .ConfigureAwait(false));
            }
        }
        public async Task<List<ChatVm>> GetChatsByIdAsync(IEnumerable<long> chatsId, long? userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var chatsCondition = PredicateBuilder.New<Chat>();
                chatsCondition = chatsId.Aggregate(chatsCondition,
                    (current, value) => current.Or(opt => opt.Id == value && !opt.Deleted).Expand());
                var query = context.Chats
                    .Where(chatsCondition);
                if (userId != null)
                {
                    query = query.Where(opt => opt.ChatUsers.Any(user => (user.UserId == userId && !user.Banned && !user.Deleted) || opt.Type == (short)ChatType.Public));
                }
                else
                {
                    query = query.Where(opt => opt.Type == (short)ChatType.Public);
                }
                var chats = await query.ToListAsync().ConfigureAwait(false);
                return ChatConverter.GetChatsVm(chats);
            }
        }
        public async Task<List<ConversationPreviewVm>> GetUserChatPreviewAsync(long userId, long? navigationTime)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var query = from chat in context.Chats
                            join chatUser in context.ChatUsers on chat.Id equals chatUser.ChatId
                            join lastReadedMessage in context.Messages
                            on
                                new { MessageId = chatUser.LastReadedGlobalMessageId.Value, chatUser.ChatId }
                            equals
                                new { MessageId = lastReadedMessage.GlobalId, ChatId = lastReadedMessage.ChatId.Value }
                            into lastReadedMessageTable
                            from lastReadedMessage in lastReadedMessageTable.DefaultIfEmpty()
                            join message in context.Messages
                            on
                                new { MessageId = chat.LastMessageGlobalId.Value, ChatId = chat.Id }
                            equals
                                new { MessageId = message.GlobalId, ChatId = message.ChatId.Value }
                            into messageTable
                            from message in messageTable.DefaultIfEmpty()
                            join attachment in context.Attachments on message.Id equals attachment.MessageId into attachTable
                            from attachment in attachTable.DefaultIfEmpty()
                            join user in context.Users on message.SenderId equals user.Id into usersTable
                            from user in usersTable.DefaultIfEmpty()
                            where chatUser.UserId == userId
                               && chatUser.Deleted == false
                               && chatUser.Banned == false
                               && chat.Deleted == false
                               && message.Deleted == false
                            select new ConversationPreviewVm
                            {
                                ConversationId = chat.Id,
                                Title = chat.Name,
                                ConversationType = ConversationType.Chat,
                                LastMessageTime = message.SendingTime,
                                Photo = chat.Photo,
                                LastMessageSenderId = message.SenderId,
                                LastMessageSenderName = $"{user.NameFirst} {user.NameSecond}",
                                PreviewText = message.Text,
                                Read = lastReadedMessage.Id > message.Id ? true : false,
                                AttachmentType = (AttachmentType)attachment.Type,
                                LastMessageId = message.GlobalId,
                                IsMuted = chatUser.IsMuted
                            };
                return await query.ToListAsync().ConfigureAwait(false);
            }
        }
        public async Task<List<ChatVm>> GetChatsNodeAsync(long userId = 0, byte limit = 100, long navigationId = 0)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    List<Chat> chats;
                    if (userId == 0)
                    {
                        chats = await context.Chats
                            .AsNoTracking()
                            .OrderBy(chat => chat.Id)
                            .Where(chat => chat.Id > navigationId && ((ChatType)chat.Type) != ChatType.Private)
                            .Take(limit)
                            .ToListAsync()
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        var query = from chat in context.Chats
                                    join chatUser in context.ChatUsers on chat.Id equals chatUser.ChatId
                                    where chatUser.UserId == userId && chatUser.Deleted == false
                                    select chat;
                        chats = await query
                            .AsNoTracking()
                            .OrderBy(chat => chat.Id)
                            .Where(chat => chat.Id > navigationId)
                            .Take(limit)
                            .ToListAsync()
                            .ConfigureAwait(false);
                    }
                    return ChatConverter.GetChatsVm(chats);
                }
            }
            catch (Exception ex)
            {
                throw new GetConversationsException("Error when retrieving chat list.", ex);
            }
        }
        public async Task<List<long>> GetChatNodeListAsync(long chatId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return (await context.Chats
                .Where(chat => chat.Id == chatId)
                .Select(chat => chat.NodesId)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false))?.ToList();
            }
        }
        public async Task<List<long>> GetChatUsersIdAsync(long chatId, bool banned = false, bool deleted = false)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return await context.ChatUsers
                .Where(chatUser => chatUser.ChatId == chatId && chatUser.Banned == banned && chatUser.Deleted == deleted)
                .Select(chatUser => chatUser.UserId)
                .ToListAsync()
                .ConfigureAwait(false);
            }
        }
        public async Task<List<ChatUserVm>> GetChatUsersAsync(long chatId, long? userId, int limit = 100, long navUserId = 0)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                if (userId != null && !await IsUserJoinedToChatAsync(chatId, userId.Value).ConfigureAwait(false))
                {
                    throw new GetUsersException();
                }
                var query = context.ChatUsers
                        .AsNoTracking()
                        .Where(chatUser => chatUser.ChatId == chatId && chatUser.Deleted == false && chatUser.Chat.Deleted == false)
                        .OrderByDescending(chatUser => chatUser.UserRole)
                        .ThenBy(chatUser => chatUser.UserId)
                        .Where(chatUser => chatUser.UserId > navUserId)
                        .Take(limit);
                return ChatUserConverter.GetChatUsersVm(await query.ToListAsync().ConfigureAwait(false));
            }
        }
        public async Task<List<ChatUserDto>> GetChatUsersAsync(IEnumerable<long> usersId, long chatId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var chatUsersCondition = PredicateBuilder.New<ChatUser>();
                chatUsersCondition = usersId.Aggregate(chatUsersCondition,
                    (current, value) =>
                        current.Or(chatUser => chatUser.UserId == value).Expand());
                var query = context.ChatUsers
                    .AsNoTracking()
                    .OrderByDescending(chatUser => chatUser.UserRole)
                    .ThenBy(chatUser => chatUser.UserId)
                    .Where(chatUsersCondition)
                    .Where(chatUser => chatUser.Deleted == false && chatUser.Banned == false && chatUser.ChatId == chatId);
                return ChatUserConverter.GetChatUsersDto(await query.ToListAsync().ConfigureAwait(false));
            }
        }
        public async Task<bool> IsUserJoinedToChatAsync(long chatId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return await context.ChatUsers
                 .Where(chatUser => chatUser.Deleted == false
                 && chatUser.UserId == userId
                 && chatUser.ChatId == chatId
                 && chatUser.Banned == false
                 && chatUser.Deleted == false)
                 .AnyAsync()
                 .ConfigureAwait(false);
            }
        }
    }
}