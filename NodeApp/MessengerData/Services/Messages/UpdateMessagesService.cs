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
    public class UpdateMessagesService : IUpdateMessagesService
    {
        private readonly ILoadDialogsService loadDialogsService;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        private readonly IUpdateChatsService updateChatsService;
        private readonly IUpdateChannelsService updateChannelsService;
        public UpdateMessagesService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.loadDialogsService = appServiceProvider.LoadDialogsService;
            this.updateChatsService = appServiceProvider.UpdateChatsService;
            this.updateChannelsService = appServiceProvider.UpdateChannelsService;
            this.contextFactory = contextFactory;

        }
        public async Task<MessageDto> EditMessageAsync(MessageDto message, long editorId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messageQuery = context.Messages
                .Include(opt => opt.Attachments)
                .Where(opt =>
                    !opt.Deleted
                    && opt.SenderId == message.SenderId
                    && opt.ReceiverId == message.ReceiverId
                    && opt.GlobalId == message.GlobalId);
                switch (message.ConversationType)
                {
                    case ConversationType.Dialog:
                        {
                            messageQuery = messageQuery.Where(opt => opt.SenderId == editorId);
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            messageQuery = messageQuery.Where(opt => opt.ChatId == message.ConversationId && opt.SenderId == editorId);
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            messageQuery = messageQuery.Where(opt => opt.ChannelId == message.ConversationId)
                                .Where(opt => opt.Channel.ChannelUsers
                                        .Any(channelUser => channelUser.UserId == editorId && channelUser.ChannelUserRole > ChannelUserRole.Subscriber));
                        }
                        break;
                }
                var editableMessages = await messageQuery.ToListAsync().ConfigureAwait(false);
                if (!editableMessages.Any())
                {
                    throw new ObjectDoesNotExistsException();
                }

                var editedMessages = new List<EditedMessage>();
                editedMessages = editableMessages.Select(opt => new EditedMessage(opt, editorId)).ToList();
                long updatedTime = DateTime.UtcNow.ToUnixTime();
                foreach (var edited in editableMessages)
                {
                    edited.Text = message.Text;
                    edited.UpdatedAt = updatedTime;
                    if (message.Attachments != null)
                    {
                        edited.Attachments = AttachmentConverter.GetAttachments(message.Attachments, edited.Id);
                    }
                }
                context.UpdateRange(editableMessages);
                await context.AddRangeAsync(editedMessages).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return MessageConverter.GetMessageDto(editableMessages.FirstOrDefault());
            }
        }
        public async Task<List<MessageDto>> SetMessagesReadAsync(IEnumerable<Guid> messagesId, long conversationId, ConversationType conversationType, long readerId)
        {
            switch (conversationType)
            {
                case ConversationType.Dialog:
                    {
                        return await SetDialogMessagesReadAsync(messagesId, readerId, conversationId).ConfigureAwait(false);
                    }
                case ConversationType.Chat:
                    {
                        return await SetChatMessagesReadAsync(messagesId, conversationId, readerId).ConfigureAwait(false);
                    }
                case ConversationType.Channel:
                    {
                        return await SetChannelMessagesReadAsync(messagesId, conversationId, readerId).ConfigureAwait(false);
                    }
                default:
                    throw new WrongArgumentException("Unknown conversation type");
            }
        }
        public async Task<List<MessageDto>> SetDialogMessagesReadByUsersIdAsync(IEnumerable<Guid> messagesId, long firstUserId, long secondUserId)
        {
            var dialogs = await loadDialogsService.GetUsersDialogsAsync(firstUserId, secondUserId).ConfigureAwait(false);
            if (dialogs != null && dialogs.Any())
            {
                List<MessageDto> readedMessages = new List<MessageDto>();
                readedMessages.AddRange(await SetDialogMessagesReadAsync(messagesId, firstUserId, dialogs.FirstOrDefault(opt => opt.FirstUserId == firstUserId).Id).ConfigureAwait(false));
                return readedMessages.GroupBy(message => message.GlobalId).FirstOrDefault()?.ToList();
            }
            throw new GetOrCreateDialogsException("Unable to get or create dialogs ID.");
        }
        public async Task<List<MessageDto>> SetDialogMessagesReadAsync(IEnumerable<Guid> messagesId, long userId, long dialogId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    var messagesCondition = PredicateBuilder.New<Message>();
                    messagesCondition = messagesId.Aggregate(messagesCondition,
                       (current, value) => current.Or(message => message.GlobalId == value).Expand());
                    IQueryable<long> dialogQuery = from dialog in context.Dialogs
                                                   join dialog2 in context.Dialogs on dialog.FirstUID equals dialog2.SecondUID
                                                   where (dialog.SecondUID == dialog2.FirstUID && dialog.Id == dialogId && dialog.FirstUID == userId)
                                                   select dialog2.Id;
                    long secondDialogId = await dialogQuery.FirstOrDefaultAsync().ConfigureAwait(false);
                    List<Message> messages = await context.Messages
                        .Where(message => message.DialogId == dialogId || message.DialogId == secondDialogId)
                        .Where(messagesCondition)
                        .ToListAsync()
                        .ConfigureAwait(false);
                    messages.ForEach(message => message.Read = true);
                    context.UpdateRange(messages);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return MessageConverter.GetMessagesDto(messages);
                }
            }
            catch (Exception ex)
            {
                throw new ReadMessageException("Unable to mark a message as read", ex);
            }
        }
        private async Task<List<MessageDto>> SetChannelMessagesReadAsync(IEnumerable<Guid> messagesId, long channelId, long readerId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messagesCondition = PredicateBuilder.New<Message>();
                messagesCondition = messagesId.Aggregate(messagesCondition,
                    (current, value) => current.Or(message => message.GlobalId == value).Expand());
                var readerUser = await context.ChannelUsers.FirstOrDefaultAsync(opt => opt.ChannelId == channelId && opt.UserId == readerId && !opt.Banned && !opt.Deleted).ConfigureAwait(false);
                if (readerUser == null)
                {
                    throw new ReadMessageException();
                }

                var lastReadedMessage = await context.Messages
                   .FirstOrDefaultAsync(opt => opt.GlobalId == readerUser.LastReadedGlobalMessageId && opt.ChannelId == channelId).ConfigureAwait(false);
                List<Message> targetMessages = await context.Messages
                    .Where(messagesCondition)
                    .Include(message => message.Attachments)
                    .ToListAsync()
                    .ConfigureAwait(false);
                if (targetMessages.Any())
                {
                    var lastMessage = targetMessages.OrderByDescending(opt => opt.SendingTime).FirstOrDefault();
                    if (lastMessage != null && (lastReadedMessage == null || lastMessage.SendingTime > lastReadedMessage.SendingTime))
                    {
                        await updateChannelsService.UpdateChannelLastReadedMessageAsync(
                            MessageConverter.GetMessageDto(lastMessage),
                            readerId).ConfigureAwait(false);
                    }

                    targetMessages.ForEach(message =>
                    {
                        message.Read = true;
                    });
                    context.Messages.UpdateRange(targetMessages);
                    await context.SaveChangesAsync().ConfigureAwait(false);

                    return MessageConverter.GetMessagesDto(targetMessages);

                }
                else
                {
                    throw new ReadMessageException();
                }
            }
        }
        private async Task<List<MessageDto>> SetChatMessagesReadAsync(IEnumerable<Guid> messagesId, long chatId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messagesCondition = PredicateBuilder.New<Message>();
                messagesCondition = messagesId.Aggregate(messagesCondition,
                        (current, value) => current.Or(message => message.GlobalId == value).Expand());
                var readerUser = await context.ChatUsers
                    .FirstOrDefaultAsync(opt => !opt.Banned && !opt.Deleted && opt.ChatId == chatId && opt.UserId == userId).ConfigureAwait(false);
                if (readerUser == null)
                {
                    throw new ReadMessageException();
                }

                var lastReadedMessage = await context.Messages
                    .FirstOrDefaultAsync(opt => opt.GlobalId == readerUser.LastReadedGlobalMessageId && opt.ChatId == chatId).ConfigureAwait(false);
                List<Message> targetMessages = await context.Messages
                    .Where(messagesCondition)
                    .Include(message => message.Attachments)
                    .ToListAsync()
                    .ConfigureAwait(false);
                if (targetMessages != null)
                {
                    var lastMessage = targetMessages.OrderByDescending(opt => opt.SendingTime).FirstOrDefault();
                    if (lastMessage != null && (lastReadedMessage == null || lastMessage.SendingTime > lastReadedMessage.SendingTime))
                    {
                        await updateChatsService.UpdateLastReadedMessageIdAsync(
                            lastMessage.GlobalId,
                            userId,
                            chatId).ConfigureAwait(false);
                    }

                    targetMessages.ForEach(message =>
                    {
                        message.Read = true;
                    });
                    context.Messages.UpdateRange(targetMessages);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return MessageConverter.GetMessagesDto(targetMessages);
                }
                else
                {
                    throw new ReadMessageException();
                }
            }
        }
        public async Task UpdateMessagesNodesIdsAsync(List<MessageVm> messages, IEnumerable<long> nodesIds)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                if (nodesIds == null)
                {
                    return;
                }

                var groups = messages.GroupBy(message => message.ConversationType);
                foreach (var group in groups)
                {
                    var messagesCondition = PredicateBuilder.New<Message>();
                    switch (group.Key)
                    {
                        case ConversationType.Dialog:
                            messagesCondition = messages.Aggregate(messagesCondition,
                                (current, value) => current.Or(opt => opt.GlobalId == value.GlobalId && opt.DialogId == value.ConversationId).Expand());
                            break;
                        case ConversationType.Chat:
                            messagesCondition = messages.Aggregate(messagesCondition,
                                (current, value) => current.Or(opt => opt.GlobalId == value.GlobalId && opt.ChatId == value.ConversationId).Expand());
                            break;
                        case ConversationType.Channel:
                            messagesCondition = messages.Aggregate(messagesCondition,
                                (current, value) => current.Or(opt => opt.GlobalId == value.GlobalId && opt.ChannelId == value.ConversationId).Expand());
                            break;
                    }
                    var updatingMessages = await context.Messages
                        .Where(messagesCondition)
                        .ToListAsync()
                        .ConfigureAwait(false);
                    updatingMessages.ForEach(message => message.NodesIds = message.NodesIds.Concat(nodesIds).Distinct().ToArray());
                    context.Messages.UpdateRange(updatingMessages);
                }
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        public async Task<List<MessageDto>> DialogMessagesReadAsync(IEnumerable<Guid> messagesId, long dialogId, long userId)
        {
            var messagesCondition = PredicateBuilder.New<Message>();
            messagesCondition = messagesId.Aggregate(messagesCondition,
                (current, value) => current.Or(message => message.GlobalId == value).Expand());
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messages = await context.Messages
                .Where(messagesCondition)
                .Where(message => (message.Dialog.Id == dialogId) && !message.Read)
                .ToListAsync()
                .ConfigureAwait(false);
                if (!messages.Any())
                {
                    throw new WrongArgumentException();
                }
                var earlierMessage = messages.OrderBy(message => message.SendingTime).FirstOrDefault();
                if (earlierMessage != null)
                {
                    var unreadedMessages = await context.Messages
                        .Where(message => message.SendingTime < earlierMessage.SendingTime && !message.Read && message.DialogId == dialogId)
                        .ToListAsync()
                        .ConfigureAwait(false);
                    List<Message> earlierUnreadedMessages = new List<Message>();
                    foreach (var readedMessage in unreadedMessages)
                    {
                        if (readedMessage.SenderId != userId)
                        {
                            readedMessage.Read = true;
                            var sameMessage = await context.Messages
                                .FirstOrDefaultAsync(message => message.Id == readedMessage.SameMessageId)
                                .ConfigureAwait(false);
                            sameMessage.Read = true;
                            earlierUnreadedMessages.Add(sameMessage);                            
                        }
                    }                   
                }
                List<Message> sameMessages = new List<Message>();
                foreach (var readedMessage in messages)
                {
                    if (readedMessage.SenderId != userId)
                    {
                        readedMessage.Read = true;
                        var sameMessage = await context.Messages
                            .FirstOrDefaultAsync(message => message.Id == readedMessage.SameMessageId).ConfigureAwait(false);
                        if (sameMessage == null)
                        {
                            sameMessages.Add(readedMessage);
                        }
                        else
                        {
                            sameMessage.Read = true;
                            sameMessages.Add(sameMessage);
                        }
                    }
                }
                await context.SaveChangesAsync().ConfigureAwait(false);
                return MessageConverter.GetMessagesDto(sameMessages);
            }
        }
    }
}