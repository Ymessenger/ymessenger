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
using NodeApp.Extensions;
using NodeApp.Helpers;
using NodeApp.HttpServer.Models.ViewModels;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Messages
{
    public class LoadMessagesService : ILoadMessagesService
    {
        private readonly ILoadChatsService loadChatsService;
        private readonly IConversationsService conversationsService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly ILoadDialogsService loadDialogsService;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public LoadMessagesService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.loadChatsService = appServiceProvider.LoadChatsService;
            this.conversationsService = appServiceProvider.ConversationsService;
            this.loadChannelsService = appServiceProvider.LoadChannelsService;
            this.loadDialogsService = appServiceProvider.LoadDialogsService;
            this.contextFactory = contextFactory;
        }
        public async Task<MessageDto> GetLastValidConversationMessage(ConversationType conversationType, long conversationId)
        {
            switch (conversationType)
            {
                case ConversationType.Dialog:
                    return MessageConverter.GetMessageDto(await GetLastValidDialogMessageAsync(conversationId).ConfigureAwait(false));
                case ConversationType.Chat:
                    return MessageConverter.GetMessageDto(await GetLastValidChatMessageAsync(conversationId).ConfigureAwait(false));
                case ConversationType.Channel:
                    return MessageConverter.GetMessageDto(await GetLastValidChannelMessageAsync(conversationId).ConfigureAwait(false));
                default:
                    throw new ArgumentOutOfRangeException(nameof(conversationType));
            }
        }
        public async Task<List<MessageDto>> GetMessagesByIdAsync(IEnumerable<Guid> messagesId, ConversationType conversationType, long conversationId, long? userId)
        {
            switch (conversationType)
            {
                case ConversationType.Dialog:
                    {
                        return await GetDialogMessagesByIdAsync(messagesId, userId, conversationId).ConfigureAwait(false);
                    }
                case ConversationType.Chat:
                    {
                        return await GetChatMessagesByIdAsync(messagesId, conversationId, userId).ConfigureAwait(false);
                    }
                case ConversationType.Channel:
                    {
                        return await GetChannelMessagesByIdAsync(messagesId, conversationId).ConfigureAwait(false);
                    }
                default:
                    throw new WrongArgumentException();
            }
        }
        private async Task<List<MessageDto>> GetChatMessagesByIdAsync(IEnumerable<Guid> messagesId, long chatId, long? userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messagesCondition = PredicateBuilder.New<Message>();
                messagesCondition = messagesId.Aggregate(messagesCondition,
                    (current, value) => current.Or(message => message.GlobalId == value).Expand());
                List<MessageDto> result = new List<MessageDto>();
                List<Message> messages = await context.Messages
                    .AsNoTracking()
                    .Where(messagesCondition)
                    .Where(message => message.ChatId == chatId)
                    .Where(message => message.ExpiredAt > DateTime.UtcNow.ToUnixTime() || message.ExpiredAt == null)
                    .Include(message => message.Attachments)
                    .ToListAsync()
                    .ConfigureAwait(false);
                if (!await CanUserGetMessageAsync(ConversationType.Chat, chatId, userId).ConfigureAwait(false))
                {
                    throw new GetMessagesException("Messages are not available for user.");
                }
                foreach (var message in messages)
                {
                    result.Add(MessageConverter.GetMessageDto(message));
                }
                return result;
            }
        }
        private async Task<List<MessageDto>> GetDialogMessagesByIdAsync(IEnumerable<Guid> messagesId, long? userId, long dialogId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messagesCondition = PredicateBuilder.New<Message>();
                messagesCondition = messagesId.Aggregate(messagesCondition,
                        (current, value) => current.Or(message => message.GlobalId == value).Expand());
                List<Message> targetMessages = await context.Messages
                    .AsNoTracking()
                    .Include(opt => opt.Attachments)
                    .Where(messagesCondition)
                    .Where(opt => opt.ExpiredAt > DateTime.UtcNow.ToUnixTime() || opt.ExpiredAt == null)
                    .Where(message => message.DialogId == dialogId)
                    .ToListAsync()
                    .ConfigureAwait(false);
                foreach (var message in targetMessages)
                {
                    if (!await CanUserGetMessageAsync(ConversationType.Dialog, message.DialogId.GetValueOrDefault(), userId).ConfigureAwait(false))
                    {
                        throw new GetMessagesException("Messages are not available for user.");
                    }
                }
                return MessageConverter.GetMessagesDto(targetMessages);
            }
        }
        private async Task<List<MessageDto>> GetChannelMessagesByIdAsync(IEnumerable<Guid> messagesId, long channelId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messagesCondition = PredicateBuilder.New<Message>();
                messagesCondition = messagesId.Aggregate(messagesCondition,
                    (current, value) => current.Or(opt => opt.GlobalId == value).Expand());
                var messages = await context.Messages
                    .Include(opt => opt.Attachments)
                    .Where(messagesCondition)
                    .Where(opt => opt.ChannelId == channelId)
                    .Where(opt => opt.ExpiredAt > DateTime.UtcNow.ToUnixTime() || opt.ExpiredAt == null)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return MessageConverter.GetMessagesDto(messages);
            }
        }
        public async Task<bool> CanUserGetMessageAsync(ConversationType conversationType, long? conversationId, long? userId)
        {
            if (userId == null)
            {
                return true;
            }

            using (MessengerDbContext context = contextFactory.Create())
            {
                switch (conversationType)
                {
                    case ConversationType.Chat:
                        {
                            return await context.ChatUsers.AnyAsync(chatUser =>
                                   chatUser.Banned == false
                                && chatUser.Deleted == false
                                && chatUser.ChatId == conversationId
                                && chatUser.UserId == userId)
                                .ConfigureAwait(false);
                        }
                    case ConversationType.Dialog:
                        {
                            if (conversationId == null)
                            {
                                return true;
                            }
                            else
                            {
                                return await context.Dialogs.AnyAsync(dialog =>
                                    (dialog.FirstUID == userId || dialog.SecondUID == userId)
                                    && dialog.Id == conversationId)
                                    .ConfigureAwait(false);
                            }
                        }
                    case ConversationType.Channel:
                        {
                            return await context.ChannelUsers.AnyAsync(channelUser =>
                                   channelUser.ChannelId == conversationId
                                && channelUser.UserId == userId
                                && channelUser.Banned == false
                                && channelUser.Deleted == false)
                                .ConfigureAwait(false);
                        }
                    default: return false;
                }
            }
        }
        public async Task<List<MessageDto>> GetMessageEditHistoryAsync(long conversationId, ConversationType conversationType, Guid messageId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                Message message;
                switch (conversationType)
                {
                    case ConversationType.Dialog:
                        message = await context.Messages.FirstOrDefaultAsync(opt => opt.GlobalId == messageId && opt.DialogId == conversationId).ConfigureAwait(false);
                        break;
                    case ConversationType.Chat:
                        message = await context.Messages.FirstOrDefaultAsync(opt => opt.GlobalId == messageId && opt.ChatId == conversationId).ConfigureAwait(false);
                        break;
                    case ConversationType.Channel:
                        message = await context.Messages.FirstOrDefaultAsync(opt => opt.GlobalId == messageId && opt.ChannelId == conversationId).ConfigureAwait(false);
                        break;
                    default:
                        throw new WrongArgumentException();
                }
                if (message == null)
                {
                    throw new ObjectDoesNotExistsException();
                }
                var messages = await context.EditedMessages
                    .Include(opt => opt.Attachments)
                    .Where(opt => opt.MessageId == message.Id)
                    .OrderByDescending(opt => opt.RecordId)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return MessageConverter.GetMessagesDto(messages, message);
            }
        }
        public async Task<List<MessageDto>> SearchMessagesAsync(
           string query,
           ConversationType? conversationType,
           long? conversationId,
           ConversationType? navConversationType,
           long? navConversationId,
           Guid? navMessageId,
           long? userId,
           int limit = 30)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messagesCondition = PredicateBuilder.New<Message>();
                var messagesQuery = context.Messages.AsNoTracking();
                ExpressionsHelper expressionsHelper = new ExpressionsHelper();
                var messageExpression = expressionsHelper.GetMessageExpression(query);
                if (conversationType != null && conversationId != null)
                {
                    if (userId != null && !await conversationsService.IsUserInConversationAsync(conversationType.Value, conversationId.Value, userId.Value).ConfigureAwait(false))
                    {
                        return new List<MessageDto>();
                    }

                    switch (conversationType.Value)
                    {
                        case ConversationType.Dialog:
                            messagesQuery = messagesQuery.Where(message => message.DialogId == conversationId);
                            break;
                        case ConversationType.Chat:
                            messagesQuery = messagesQuery.Where(message => message.ChatId == conversationId);
                            break;
                        case ConversationType.Channel:
                            messagesQuery = messagesQuery.Where(message => message.ChannelId == conversationId);
                            break;
                    }
                }
                else
                {
                    if (userId != null)
                    {
                        var dialogsIds = await loadDialogsService.GetUserDialogsIdAsync(userId.Value).ConfigureAwait(false);
                        var chatsIds = await loadChatsService.GetUserChatsIdAsync(userId.Value).ConfigureAwait(false);
                        var channelsIds = await loadChannelsService.GetUserChannelsIdAsync(userId.Value).ConfigureAwait(false);
                        messagesCondition = dialogsIds.Aggregate(messagesCondition,
                            (current, value) => current.Or(option => option.DialogId == value).Expand());
                        messagesCondition = chatsIds.Aggregate(messagesCondition,
                            (current, value) => current.Or(option => option.ChatId == value).Expand());
                        messagesCondition = channelsIds.Aggregate(messagesCondition,
                            (current, value) => current.Or(option => option.ChannelId == value).Expand());
                        messagesQuery = messagesQuery.Where(messagesCondition);
                    }
                }
                if (navConversationId != null && navConversationType != null && navMessageId != null)
                {
                    var navMessage = (await GetMessagesByIdAsync(
                        new List<Guid> { navMessageId.Value },
                        navConversationType.Value,
                        navConversationId.Value,
                        userId).ConfigureAwait(false)).FirstOrDefault();
                    if (navMessage != null)
                    {
                        messagesQuery = messagesQuery
                            .Where(message => message.SendingTime <= navMessage.SendingTime && message.GlobalId != navMessage.GlobalId);
                    }
                }
                var messages = await messagesQuery
                    .Where(messageExpression)
                    .Where(message => !message.Deleted && (message.ExpiredAt == null || message.ExpiredAt > DateTime.UtcNow.ToUnixTime()))
                    .OrderByDescending(message => message.SendingTime)
                    .Take(limit)
                    .Include(message => message.Attachments)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return MessageConverter.GetMessagesDto(messages);
            }
        }
        public async Task<List<MessageDto>> GetMessagesAsync(long conversationId, ConversationType conversationType, bool direction, Guid? messageId, List<AttachmentType> attachmentsTypes, int limit)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                IQueryable<Message> query = context.Messages
                    .Where(message => message.ExpiredAt > DateTime.UtcNow.ToUnixTime() || message.ExpiredAt == null)
                    .Where(message => !message.Deleted);
                Message navigationMessage = null;

                switch (conversationType)
                {
                    case ConversationType.Dialog:
                        {
                            navigationMessage = await context.Messages.FirstOrDefaultAsync(opt => opt.GlobalId == messageId && opt.DialogId == conversationId).ConfigureAwait(false);
                            query = query.Where(opt => opt.DialogId == conversationId);
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            navigationMessage = await context.Messages.FirstOrDefaultAsync(opt => opt.GlobalId == messageId && opt.ChatId == conversationId).ConfigureAwait(false);
                            query = query.Where(opt => opt.ChatId == conversationId);
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            navigationMessage = await context.Messages.FirstOrDefaultAsync(opt => opt.GlobalId == messageId && opt.ChannelId == conversationId).ConfigureAwait(false);
                            query = query.Where(opt => opt.ChannelId == conversationId);
                        }
                        break;
                }
                if (navigationMessage != null)
                {
                    query = direction
                         ? query.Where(opt => opt.Id < navigationMessage.Id)
                         : query.Where(opt => opt.Id > navigationMessage.Id);
                }
                if (!attachmentsTypes.IsNullOrEmpty())
                {
                    List<short> types = attachmentsTypes.Select(type => (short)type).ToList();
                    query = query.Where(opt =>
                        opt.Attachments.Any(attach => types.Contains(attach.Type)));
                }
                query = direction
                    ? query.OrderByDescending(message => message.SendingTime)
                                .ThenByDescending(message => message.Id)
                                .ThenByDescending(message => message.GlobalId)
                    : query.OrderBy(message => message.SendingTime)
                                .ThenBy(message => message.Id)
                                .ThenBy(message => message.GlobalId);
                var messages = await query
                    .AsNoTracking()
                    .Include(message => message.Attachments)
                    .Take(limit)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return MessageConverter.GetMessagesDto(messages);
            }
        }
        public async Task<List<MessageDto>> GetUserUpdatedMessagesAsync(long userId, long updatedTime, long? conversationId, ConversationType? conversationType, Guid? messageId, int limit = 1000)
        {
            IEnumerable<long> userDialogsIds = await loadDialogsService.GetUserDialogsIdAsync(userId).ConfigureAwait(false);
            IEnumerable<long> userChatsIds = await loadChatsService.GetUserChatsIdAsync(userId).ConfigureAwait(false);
            IEnumerable<long> userChannelsIds = await loadChannelsService.GetUserChannelsIdAsync(userId).ConfigureAwait(false);
            var messagesCondition = PredicateBuilder.New<Message>();
            messagesCondition = userDialogsIds.Aggregate(messagesCondition,
                (current, value) => current.Or(opt => opt.DialogId == value));
            messagesCondition = messagesCondition.Or(opt => userChatsIds.Contains(opt.ChatId.GetValueOrDefault()));
            messagesCondition = messagesCondition.Or(opt => userChannelsIds.Contains(opt.ChannelId.GetValueOrDefault())).Expand();
            List<Message> updatedMessages = new List<Message>();
            Message navigationMessage = null;
            using (MessengerDbContext context = contextFactory.Create())
            {
                switch (conversationType.GetValueOrDefault())
                {
                    case ConversationType.Dialog:
                        {
                            navigationMessage = await context.Messages
                                .FirstOrDefaultAsync(message => message.DialogId == conversationId && message.GlobalId == messageId)
                                .ConfigureAwait(false);
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            navigationMessage = await context.Messages
                                .FirstOrDefaultAsync(message => message.ChatId == conversationId && message.GlobalId == messageId)
                                .ConfigureAwait(false);
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            navigationMessage = await context.Messages
                                .FirstOrDefaultAsync(message => message.ChannelId == conversationId && message.GlobalId == messageId)
                                .ConfigureAwait(false);
                        }
                        break;
                }
                if (navigationMessage == null)
                {
                    navigationMessage = new Message { Id = 0 };
                }
                updatedMessages = await context.Messages
                    .Where(messagesCondition)
                    .Where(opt => opt.UpdatedAt >= updatedTime)
                    .Where(opt => opt.ExpiredAt > DateTime.UtcNow.ToUnixTime() || opt.ExpiredAt == null)
                    .Take(limit)
                    .ToListAsync()
                    .ConfigureAwait(false);
                updatedMessages = updatedMessages
                    .OrderBy(opt => opt.UpdatedAt)
                    .ThenBy(opt => opt.Id)
                    .ThenBy(opt => opt.GlobalId)
                    .Where(opt => opt.Id > navigationMessage.Id)
                    .ToList();
                return MessageConverter.GetMessagesDto(updatedMessages);
            }
        }
        public async Task<Message> GetLastValidDialogMessageAsync(long dialogId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var message = await context.Messages
                .OrderByDescending(opt => opt.SendingTime)
                .ThenByDescending(opt => opt.Id)
                .ThenByDescending(opt => opt.GlobalId)
                .Where(opt => opt.Deleted == false && opt.DialogId == dialogId)
                .Where(opt => opt.ExpiredAt > DateTime.UtcNow.ToUnixTime() || opt.ExpiredAt == null)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
                return message;
            }
        }
        public async Task<Message> GetLastValidChatMessageAsync(long chatId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var message = await context.Messages
                .OrderByDescending(opt => opt.SendingTime)
                .ThenByDescending(opt => opt.Id)
                .ThenByDescending(opt => opt.GlobalId)
                .Where(opt => opt.Deleted == false && opt.ChatId == chatId)
                .Where(opt => opt.ExpiredAt > DateTime.UtcNow.ToUnixTime() || opt.ExpiredAt == null)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
                return message;
            }
        }
        public async Task<Message> GetLastValidChannelMessageAsync(long channelId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var message = await context.Messages
                 .OrderByDescending(opt => opt.SendingTime)
                 .Where(opt => opt.ChannelId == channelId)
                 .FirstOrDefaultAsync()
                 .ConfigureAwait(false);
                return message;
            }
        }
        public async Task<bool> IsChannelMessageExistsAsync(Guid messageId, long conversationId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return await context.Messages
                .AnyAsync(opt => opt.GlobalId == messageId && opt.ChannelId == conversationId)
                .ConfigureAwait(false);
            }
        }
        public async Task<bool> IsChatMessageExistsAsync(Guid messageId, long chatId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return await context.Messages
                .Where(opt => opt.ExpiredAt > DateTime.UtcNow.ToUnixTime() || opt.ExpiredAt == null)
                .AnyAsync(message => message.ChatId == chatId
                && message.GlobalId == messageId)
                .ConfigureAwait(false);
            }
        }
        public async Task<bool> IsDialogMessageExistsAsync(Guid messageId, long senderId, long receiverId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                IEnumerable<long> dialogsId = await loadDialogsService.GetDialogsIdByUsersIdPairAsync(senderId, receiverId).ConfigureAwait(false);
                if (dialogsId == null)
                {
                    return false;
                }

                return await context.Messages
                    .Where(opt => opt.ExpiredAt > DateTime.UtcNow.ToUnixTime() || opt.ExpiredAt == null)
                    .AnyAsync(message => message.GlobalId == messageId
                    && message.DialogId == dialogsId.FirstOrDefault())
                    .ConfigureAwait(false);
            }
        }
        public async Task<bool> IsReplyMessageExistsAsync(MessageVm message)
        {
            switch (message.ConversationType)
            {
                case ConversationType.Dialog:
                    {
                        return await IsDialogMessageExistsAsync(message.ReplyTo.Value, message.SenderId.Value, message.ReceiverId.Value).ConfigureAwait(false);
                    }
                case ConversationType.Chat:
                    {
                        return await IsChatMessageExistsAsync(message.ReplyTo.Value, message.ConversationId.GetValueOrDefault()).ConfigureAwait(false);
                    }
                case ConversationType.Channel:
                    {
                        return await IsChannelMessageExistsAsync(message.ReplyTo.Value, message.ConversationId.GetValueOrDefault()).ConfigureAwait(false);
                    }
                default:
                    return false;
            }
        }
        public async Task<List<MessageDto>> GetChatMessagesAsync(long chatId, long userId, Guid? navMessageId, List<AttachmentType> attachmentsTypes = null, bool direction = true, byte messagesLimit = 30)
        {
            if (!await loadChatsService.IsUserJoinedToChatAsync(chatId, userId).ConfigureAwait(false))
            {
                throw new GetMessagesException("User does not have access to chat.");
            }
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messagesQuery = from message in context.Messages
                                    where message.ChatId == chatId && message.Deleted == false
                                    select message;
                if (!attachmentsTypes.IsNullOrEmpty())
                {
                    List<short> types = attachmentsTypes.Select(type => (short)type).ToList();
                    messagesQuery = messagesQuery.Where(opt =>
                        opt.Attachments.Any(attach => types.Contains(attach.Type)));
                }
                if (navMessageId != null)
                {
                    MessageDto navigationMessage = (await GetMessagesByIdAsync(
                        new List<Guid>
                        {
                            navMessageId.Value
                        },
                        ConversationType.Chat,
                        chatId,
                        userId).ConfigureAwait(false)).FirstOrDefault();
                    if (navigationMessage != null)
                    {
                        if (direction)
                        {
                            messagesQuery = messagesQuery.Where(opt => opt.SendingTime < navigationMessage.SendingTime);
                        }
                        else
                        {
                            messagesQuery = messagesQuery.Where(opt => opt.SendingTime > navigationMessage.SendingTime);
                        }
                    }
                }
                if (direction)
                {
                    messagesQuery = messagesQuery
                                .OrderByDescending(message => message.SendingTime)
                                .ThenByDescending(message => message.Id)
                                .ThenByDescending(message => message.GlobalId);
                }
                else
                {
                    messagesQuery = messagesQuery
                                .OrderBy(message => message.SendingTime)
                                .ThenBy(message => message.Id)
                                .ThenBy(message => message.GlobalId);
                }
                var messages = await messagesQuery
                    .AsNoTracking()
                    .Include(message => message.Attachments)
                    .Take(messagesLimit)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return MessageConverter.GetMessagesDto(messages);
            }
        }
        public async Task<List<KeyValuePair<long, int>>> GetChatUnreadedMessagesCountAsync(List<long> chatsId, long userId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    var query = from chat in context.Chats
                                where chatsId.Contains(chat.Id)
                                join chatUser in context.ChatUsers on chat.Id equals chatUser.ChatId
                                join lastMessage in context.Messages on
                                    new { MessageId = chatUser.LastReadedGlobalMessageId, ChatId = (long?)chatUser.ChatId }
                                equals
                                    new { MessageId = (Guid?)lastMessage.GlobalId, ChatId = lastMessage.ChatId }
                                join message in context.Messages on chat.Id equals message.ChatId
                                where chatUser.UserId == userId
                                    && message.SendingTime > lastMessage.SendingTime
                                    && message.SenderId != userId
                                    && message.Deleted == false
                                group message by message.ChatId into messagesGroup
                                select new
                                {
                                    ChatId = messagesGroup.Key,
                                    UnreadedCount = messagesGroup.Count()
                                };
                    var result = await query.ToListAsync().ConfigureAwait(false);
                    return result.Select(opt => new KeyValuePair<long, int>(opt.ChatId.GetValueOrDefault(), opt.UnreadedCount)).ToList();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<List<MessageDto>> GetChannelMessagesAsync(long channelId, long userId, Guid? navigationMessageId, List<AttachmentType> attachmentsTypes = null, bool direction = true, byte limit = 30)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var query = context.Messages.Where(opt => opt.ChannelId == channelId && opt.Deleted == false);
                if (navigationMessageId != null)
                {
                    var navigationMessage = await context.Messages.FirstOrDefaultAsync(opt => opt.GlobalId == navigationMessageId && opt.ChannelId == channelId).ConfigureAwait(false);
                    if (navigationMessage != null)
                    {
                        if (direction)
                        {
                            query = query.Where(opt => opt.Id < navigationMessage.Id);
                        }
                        else
                        {
                            query = query.Where(opt => opt.Id > navigationMessage.Id);
                        }
                    }
                }
                if (direction)
                {
                    query = query
                                .OrderByDescending(message => message.SendingTime)
                                .ThenByDescending(message => message.Id)
                                .ThenByDescending(message => message.GlobalId);
                }
                else
                {
                    query = query
                                .OrderBy(message => message.SendingTime)
                                .ThenBy(message => message.Id)
                                .ThenBy(message => message.GlobalId);
                }
                if (!attachmentsTypes.IsNullOrEmpty())
                {
                    List<short> types = attachmentsTypes.Select(type => (short)type).ToList();
                    query = query.Where(opt =>
                        opt.Attachments.Any(attach => types.Contains(attach.Type)));
                }
                var messages = await query
                    .AsNoTracking()
                    .Include(message => message.Attachments)
                    .Take(limit)
                    .ToListAsync()
                    .ConfigureAwait(false);
                var channelUser = await context.ChannelUsers.FirstOrDefaultAsync(opt => opt.ChannelId == channelId && opt.UserId == userId).ConfigureAwait(false);
                if (channelUser != null)
                {
                    var lastReadedMessage = await context.Messages
                        .FirstOrDefaultAsync(opt => opt.GlobalId == channelUser.LastReadedGlobalMessageId && opt.ChannelId == channelId).ConfigureAwait(false);
                    if (lastReadedMessage != null)
                    {
                        messages.ForEach(message =>
                        {
                            if (message.Id > lastReadedMessage.Id && message.SenderId != userId)
                            {
                                message.Read = false;
                            }
                            else
                            {
                                message.Read = true;
                            }
                        });
                    }
                }
                return MessageConverter.GetMessagesDto(messages);
            }
        }
        public async Task<List<KeyValuePair<long, int>>> GetChannelsUnreadedMessagesCountAsync(List<long> channelsId, long userId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    var query = from channel in context.Channels
                                where channelsId.Contains(channel.ChannelId)
                                join channelUser in context.ChannelUsers on channel.ChannelId equals channelUser.ChannelId
                                join message in context.Messages on channel.ChannelId equals message.ChannelId
                                join lastMessage in context.Messages on
                                        new { MessageId = channelUser.LastReadedGlobalMessageId, ChannelId = (long?)channelUser.ChannelId }
                                    equals
                                        new { MessageId = (Guid?)lastMessage.GlobalId, ChannelId = lastMessage.ChannelId }
                                where channelUser.UserId == userId
                                       && message.SendingTime > lastMessage.SendingTime
                                       && message.SenderId != userId
                                       && message.Deleted == false
                                group message by message.ChannelId into messagesGroup
                                select new
                                {
                                    ChannelId = messagesGroup.Key,
                                    UnreadedCount = messagesGroup.Count()
                                };
                    var results = await query.ToListAsync().ConfigureAwait(false);
                    return results.Select(opt => new KeyValuePair<long, int>(opt.ChannelId.Value, opt.UnreadedCount)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return null;
            }
        }
        public async Task<List<MessageDto>> GetDialogMessagesAsync(long dialogId, long userId, Guid? lastId = null, List<AttachmentType> attachmentsTypes = null, bool direction = true, short limit = 30)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    IQueryable<Message> queryPart = context.Messages
                        .Include(message => message.Dialog)
                        .Include(message => message.Attachments);
                    if (lastId != null)
                    {
                        MessageDto messageQueryResult = (await GetMessagesByIdAsync(
                            new List<Guid>
                            {
                                lastId.Value
                            },
                            ConversationType.Dialog,
                            dialogId,
                            userId).ConfigureAwait(false)).FirstOrDefault();
                        if (messageQueryResult != null)
                        {
                            if (direction)
                            {
                                queryPart = queryPart
                                    .Where(opt => opt.SendingTime < messageQueryResult.SendingTime);
                            }
                            else
                            {
                                queryPart = queryPart
                                    .Where(opt => opt.SendingTime > messageQueryResult.SendingTime);
                            }
                        }
                    }
                    if (direction)
                    {
                        queryPart = queryPart.OrderByDescending(message => message.SendingTime)
                                    .ThenByDescending(message => message.Id)
                                    .ThenByDescending(message => message.GlobalId);
                    }
                    else
                    {
                        queryPart = queryPart.OrderBy(message => message.SendingTime)
                                    .ThenBy(message => message.Id)
                                    .ThenBy(message => message.GlobalId);
                    }
                    if (!attachmentsTypes.IsNullOrEmpty())
                    {
                        List<short> types = attachmentsTypes.Select(type => (short)type).ToList();
                        queryPart = queryPart.Where(opt =>
                            opt.Attachments.Any(attach => types.Contains(attach.Type)));
                    }
                    queryPart = queryPart.Where(message => message.DialogId == dialogId
                                        && message.Dialog.FirstUID == userId
                                        && message.Deleted == false);
                    queryPart = queryPart.Take(limit);
                    var messages = await queryPart.ToListAsync().ConfigureAwait(false);
                    return MessageConverter.GetMessagesDto(messages);
                }
            }
            catch (Exception ex)
            {
                throw new GetMessagesException(null, ex);
            }
        }
        public async Task<List<KeyValuePair<long, int>>> GetDialogUnreadedMessagesCountAsync(List<long> dialogsId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var query = from dialog in context.Dialogs
                            where dialogsId.Contains(dialog.Id)
                            join message in context.Messages on dialog.Id equals message.DialogId
                            where message.SenderId != userId
                                && !message.Deleted
                                && !message.Read
                            group message by message.DialogId into messagesGroup
                            select new
                            {
                                DialogId = messagesGroup.Key,
                                UnreadedCount = messagesGroup.Count()
                            };
                var result = await query.ToListAsync().ConfigureAwait(false);
                return result.Select(opt => new KeyValuePair<long, int>(opt.DialogId.Value, opt.UnreadedCount)).ToList();
            }
        }
        public async Task<MessagesPageViewModel> GetMessagesPageAsync(long conversationId, ConversationType conversationType, int pageNumber, int limit)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var countQuery = context.Messages.AsNoTracking();
                var messagesQuery = context.Messages
                    .Include(message => message.Attachments)
                    .AsNoTracking();
                switch (conversationType)
                {                    
                    case ConversationType.Dialog:
                        {
                            countQuery = countQuery.Where(message => message.DialogId == conversationId);
                            messagesQuery = messagesQuery.Where(message => message.DialogId == conversationId);
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            countQuery = countQuery.Where(message => message.ChatId == conversationId);
                            messagesQuery = messagesQuery.Where(message => message.ChatId == conversationId);
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            countQuery = countQuery.Where(message => message.ChannelId == conversationId);
                            messagesQuery = messagesQuery.Where(message => message.ChannelId == conversationId);
                        }
                        break;
                }
                var messagesCount = await countQuery.Where(message => !message.Deleted).CountAsync().ConfigureAwait(false);
                var messages = await messagesQuery
                    .Where(message => !message.Deleted)
                    .OrderByDescending(message => message.SendingTime)
                    .Skip(pageNumber * limit)
                    .Take(limit)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return new MessagesPageViewModel
                {
                    CurrentPage = pageNumber,
                    Messages = MessageConverter.GetMessagesVm(MessageConverter.GetMessagesDto(messages), null),
                    PagesCount = messagesCount / limit + (messagesCount % limit > 0 ? 1 : 0)
                };                
            }
        }
    }
}