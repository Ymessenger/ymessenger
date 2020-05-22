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
using NodeApp.Interfaces;
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
    public class DeleteMessagesService : IDeleteMessagesService
    {
        private readonly ILoadMessagesService _loadMessagesService;        
        private readonly ILoadDialogsService _loadDialogsService;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public DeleteMessagesService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            _loadMessagesService = appServiceProvider.LoadMessagesService;            
            _loadDialogsService = appServiceProvider.LoadDialogsService;
            this.contextFactory = contextFactory;
        }
        public async Task<List<MessageDto>> DeleteMessagesInfoAsync(long conversationId, ConversationType conversationType, IEnumerable<Guid> messagesIds, long userId)
        {
            switch (conversationType)
            {
                case ConversationType.Dialog:
                    {
                        return await DeleteDialogMessagesInfoAsync(conversationId, messagesIds, userId).ConfigureAwait(false);
                    }
                case ConversationType.Chat:
                    {
                        return await DeleteChatMessagesInfoAsync(conversationId, messagesIds, userId).ConfigureAwait(false);
                    }
                case ConversationType.Channel:
                    {
                        return await DeleteChannelMessagesInfoAsync(conversationId, messagesIds, userId).ConfigureAwait(false);
                    }
                default:
                    throw new WrongArgumentException("Unknown conversation type.");
            }
        }
        private async Task<List<MessageDto>> DeleteChatMessagesInfoAsync(long chatId, IEnumerable<Guid> messagesIds, long userId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    var messagesCondition = PredicateBuilder.New<Message>();
                    messagesCondition = messagesIds.Aggregate(messagesCondition,
                        (current, value) => current.Or(opt => opt.GlobalId == value && opt.ChatId == chatId && opt.Deleted == false));
                    var query = from conversation in context.Chats
                                join chatUser in context.ChatUsers on conversation.Id equals chatUser.ChatId
                                where chatUser.Banned == false
                                   && chatUser.Deleted == false
                                   && conversation.Deleted == false
                                   && chatUser.UserId == userId
                                   && conversation.Id == chatId
                                select new
                                {
                                    Chat = conversation,
                                    User = chatUser
                                };
                    var result = await query.FirstOrDefaultAsync().ConfigureAwait(false);
                    if (result == null || result.User == null)
                    {
                        return new List<MessageDto>();
                    }

                    List<Message> messages = await context.Messages
                        .Include(opt => opt.Attachments)
                        .Where(messagesCondition)
                        .ToListAsync()
                        .ConfigureAwait(false);
                    if (!messages.Any())
                    {
                        return new List<MessageDto>();
                    }
                    var deletedMessagesIds = messages.Select(message => message.GlobalId).ToList();
                    var usersCondition = PredicateBuilder.New<ChatUser>();
                    usersCondition = deletedMessagesIds.Aggregate(usersCondition,
                        (current, value) => current.Or(opt => opt.LastReadedGlobalMessageId == value).Expand());
                    var chatUsers = await context.ChatUsers.Where(usersCondition).ToListAsync().ConfigureAwait(false);
                    var groupedChatUsers = chatUsers.GroupBy(opt => opt.LastReadedGlobalMessageId);
                    foreach (var group in groupedChatUsers)
                    {
                        var message = await context.Messages
                            .OrderByDescending(opt => opt.SendingTime)
                            .ThenBy(opt => opt.GlobalId)
                            .Where(opt => !deletedMessagesIds.Contains(opt.GlobalId) && opt.ChatId == chatId && !opt.Deleted)
                            .FirstOrDefaultAsync()
                            .ConfigureAwait(false);                       
                        foreach (var chatUser in group)
                        {
                            chatUser.LastReadedGlobalMessageId = message?.GlobalId;
                        }
                        context.ChatUsers.UpdateRange(group);
                    }
                    List<MessageDto> deletedMessages = MessageConverter.GetMessagesDto(messages);
                    foreach (var message in messages)
                    {
                        if (message.SenderId != result.User.UserId && result.User.UserRole == UserRole.User)
                        {
                            continue;
                        }

                        message.Deleted = true;
                        message.UpdatedAt = DateTime.UtcNow.ToUnixTime();
                        if (NodeSettings.Configs.Node.PermanentlyDeleting)
                        {
                            message.Attachments = null;
                            message.Replyto = null;
                            message.SendingTime = 0;
                            message.Text = null;
                            message.SenderId = null;
                        }
                    }
                    context.UpdateRange(messages);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    Message lastMessage = await _loadMessagesService.GetLastValidChatMessageAsync(chatId).ConfigureAwait(false);
                    result.Chat.LastMessageId = lastMessage?.Id ?? null;
                    result.Chat.LastMessageGlobalId = lastMessage?.GlobalId;
                    context.Update(result.Chat);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return deletedMessages;
                }
            }
            catch (Exception ex)
            {
                throw new DeleteMessagesException("An error occurred while deleting messages.", ex);
            }
        }
        private async Task<List<MessageDto>> DeleteDialogMessagesInfoAsync(long dialogId, IEnumerable<Guid> messagesIds, long userId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    long mirrorDialogId = await _loadDialogsService.GetMirrorDialogIdAsync(dialogId).ConfigureAwait(false);                   
                    var messagesCondition = PredicateBuilder.New<Message>();
                    messagesCondition = messagesIds.Aggregate(messagesCondition,
                        (current, value) => current.Or(opt => opt.GlobalId == value
                        && (opt.DialogId == dialogId || opt.DialogId == mirrorDialogId)
                        && (opt.SenderId == userId || opt.ReceiverId == userId)).Expand());
                    List<MessageDto> deletedMessages;
                    List<Message> messages = await context.Messages
                        .Include(opt => opt.Attachments)
                        .Where(messagesCondition)
                        .ToListAsync()
                        .ConfigureAwait(false);                   
                    if (!messages.Any())
                    {
                        return new List<MessageDto>();
                    }
                    deletedMessages = MessageConverter.GetMessagesDto(messages);
                    Dialog firstDialog = await context.Dialogs.FirstOrDefaultAsync(opt => opt.Id == dialogId).ConfigureAwait(false);
                    Dialog mirrorDialog = await context.Dialogs.FirstOrDefaultAsync(opt => opt.Id == mirrorDialogId).ConfigureAwait(false);
                    foreach (var message in messages)
                    {
                        message.Deleted = true;
                        message.UpdatedAt = DateTime.UtcNow.ToUnixTime();
                        if (NodeSettings.Configs.Node.PermanentlyDeleting)
                        {
                            message.Attachments = null;
                            message.Replyto = null;
                            message.SendingTime = 0;
                            message.Text = null;
                            message.SenderId = null;
                            message.ReceiverId = null;
                        }
                    }
                    context.UpdateRange(messages);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    Message lastMessageFirstDialog = await _loadMessagesService.GetLastValidDialogMessageAsync(dialogId).ConfigureAwait(false);
                    Message lastMessageSecondDialog = await _loadMessagesService.GetLastValidDialogMessageAsync(mirrorDialogId).ConfigureAwait(false);
                    firstDialog.LastMessageId = lastMessageFirstDialog?.Id ?? null;
                    mirrorDialog.LastMessageId = lastMessageSecondDialog?.Id ?? null;
                    firstDialog.LastMessageGlobalId = lastMessageFirstDialog?.GlobalId;
                    mirrorDialog.LastMessageGlobalId = lastMessageSecondDialog?.GlobalId;
                    context.Dialogs.UpdateRange(firstDialog, mirrorDialog);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return deletedMessages;
                }
            }
            catch (Exception ex)
            {
                throw new DeleteMessagesException("An error occurred while deleting messages.", ex);
            }
        }
        private async Task<List<MessageDto>> DeleteChannelMessagesInfoAsync(long channelId, IEnumerable<Guid> messagesIds, long userId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    var messagesCondition = PredicateBuilder.New<Message>();
                    messagesCondition = messagesIds.Aggregate(messagesCondition,
                        (current, value) => current.Or(opt => opt.GlobalId == value).Expand());
                    var channelUser = await context.ChannelUsers
                        .Include(opt => opt.Channel)
                        .FirstOrDefaultAsync(opt =>
                            opt.ChannelId == channelId
                         && opt.UserId == userId
                         && opt.ChannelUserRole >= ChannelUserRole.Administrator)
                        .ConfigureAwait(false);
                    if (channelUser == null)
                    {
                        throw new PermissionDeniedException();
                    }

                    var messages = await context.Messages
                        .Include(opt => opt.Attachments)
                        .Where(messagesCondition)
                        .Where(opt => opt.ChannelId == channelId)
                        .ToListAsync()
                        .ConfigureAwait(false);
                    if (!messages.Any())
                    {
                        return new List<MessageDto>();
                    }
                    var deletedMessagesIds = messages.Select(message => message.GlobalId).ToList();
                    var usersCondition = PredicateBuilder.New<ChannelUser>();
                    usersCondition = deletedMessagesIds.Aggregate(usersCondition,
                        (current, value) => current.Or(opt => opt.LastReadedGlobalMessageId == value).Expand());
                    var channelUsers = await context.ChannelUsers.Where(usersCondition).ToListAsync().ConfigureAwait(false);
                    var groupedChannelUsers = channelUsers.GroupBy(opt => opt.LastReadedGlobalMessageId);
                    foreach (var group in groupedChannelUsers)
                    {
                        var message = await context.Messages
                            .OrderByDescending(opt => opt.SendingTime)
                            .ThenBy(opt => opt.GlobalId)
                            .Where(opt => !deletedMessagesIds.Contains(opt.GlobalId) && opt.ChannelId == channelId && !opt.Deleted)
                            .FirstOrDefaultAsync()
                            .ConfigureAwait(false);                        
                        foreach (var user in group)
                        {
                            user.LastReadedGlobalMessageId = message?.GlobalId;
                        }
                        context.ChannelUsers.UpdateRange(group);
                    }
                    var deletedMessages = MessageConverter.GetMessagesDto(messages);
                    messages.ForEach(message =>
                    {
                        message.Deleted = true;
                        message.UpdatedAt = DateTime.UtcNow.ToUnixTime();
                        if (NodeSettings.Configs.Node.PermanentlyDeleting)
                        {
                            message.Attachments = null;
                            message.Replyto = null;
                            message.SendingTime = 0;
                            message.Text = null;
                            message.SenderId = null;
                        }
                    });
                    context.UpdateRange(messages);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    Message lastValidMessage = await _loadMessagesService.GetLastValidChannelMessageAsync(channelId).ConfigureAwait(false);
                    channelUser.Channel.LastMessageId = lastValidMessage?.Id ?? null;
                    channelUser.Channel.LastMessageGlobalId = lastValidMessage?.GlobalId;
                    context.Update(channelUser);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return deletedMessages;
                }
            }
            catch (Exception ex)
            {
                throw new DeleteMessagesException("An error ocurred while deleting the messages.", ex);
            }
        }
        public async Task<List<MessageDto>> DeleteForwardedDialogMessagesAsync(List<MessageVm> messages)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messagesCondition = PredicateBuilder.New<Message>();
                messagesCondition = messages.Aggregate(messagesCondition,
                    (current, value) => current.Or(opt =>
                    opt.GlobalId == value.GlobalId
                    && opt.SenderId == value.SenderId
                    && opt.ReceiverId == value.ReceiverId).Expand());
                var deletedMessages = await context.Messages.Where(messagesCondition).ToListAsync().ConfigureAwait(false);
                context.Messages.RemoveRange(deletedMessages);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return MessageConverter.GetMessagesDto(deletedMessages);
            }
        }
        public async Task DeleteMessagesAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                long messageId = 0;
                List<Message> messages = await context.Messages
                    .AsNoTracking()
                    .Include(opt => opt.Attachments)
                    .OrderBy(opt => opt.Id)
                    .Where(opt => opt.SenderId == userId && opt.Id > messageId && !opt.Deleted)
                    .Take(100)
                    .ToListAsync()
                    .ConfigureAwait(false);
                while (messages.Any())
                {
                    var channelMessages = messages.Where(message => message.ChannelId != null);
                    var dialogMessages = messages.Where(message => message.DialogId != null);
                    var chatMessages = messages.Where(message => message.ChatId != null);
                    if (!channelMessages.IsNullOrEmpty())
                    {
                        var groups = channelMessages.GroupBy(opt => opt.ChannelId);
                        foreach (var group in groups)
                        {
                            await DeleteChannelMessagesInfoAsync(group.Key.Value, group.Select(message => message.GlobalId), userId).ConfigureAwait(false);
                        }
                    }
                    if (!dialogMessages.IsNullOrEmpty())
                    {
                        var groups = dialogMessages.GroupBy(opt => opt.DialogId);
                        foreach (var group in groups)
                        {
                            await DeleteDialogMessagesInfoAsync(group.Key.Value, group.Select(message => message.GlobalId), userId).ConfigureAwait(false);
                        }
                    }
                    if (!chatMessages.IsNullOrEmpty())
                    {
                        var groups = chatMessages.GroupBy(opt => opt.ChatId);
                        foreach (var group in groups)
                        {
                            await DeleteChatMessagesInfoAsync(group.Key.Value, group.Select(message => message.GlobalId), userId).ConfigureAwait(false);
                        }
                    }
                    messageId = messages.Max(opt => opt.Id);
                    messages = await context.Messages
                        .AsNoTracking()
                        .Include(opt => opt.Attachments)
                        .OrderBy(opt => opt.Id)
                        .Where(opt => opt.SenderId == userId && opt.Id > messageId && !opt.Deleted)
                        .Take(100)
                        .ToListAsync()
                        .ConfigureAwait(false);
                }
            }
        }
        public async Task DeleteMessagesAsync(long conversationId, ConversationType conversationType, long userId)
        {
            var messageCondition = PredicateBuilder.New<Message>();
            switch (conversationType)
            {
                case ConversationType.Dialog:
                    messageCondition = messageCondition.And(message => message.DialogId == conversationId);
                    break;
                case ConversationType.Chat:
                    messageCondition = messageCondition.And(message => message.ChatId == conversationId);
                    break;
                case ConversationType.Channel:
                    messageCondition = messageCondition.And(message => message.ChannelId == conversationId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(conversationType));
            }
            using (MessengerDbContext context = contextFactory.Create())
            {

                long messageId = 0;
                var messages = await context.Messages
                    .AsNoTracking()
                    .OrderBy(message => message.Id)
                    .Where(messageCondition)
                    .Where(message => message.SenderId == userId && !message.Deleted && message.Id > messageId)
                    .Take(100)
                    .ToListAsync()
                    .ConfigureAwait(false);
                while (messages.Any())
                {
                    await DeleteMessagesInfoAsync(conversationId, conversationType, messages.Select(message => message.GlobalId), userId).ConfigureAwait(false);
                    messageId = messages.Max(message => message.Id);
                    messages = await context.Messages
                        .AsNoTracking()
                        .OrderBy(message => message.Id)
                        .Where(messageCondition)
                        .Where(message => message.SenderId == userId && !message.Deleted && message.Id > messageId)
                        .Take(100)
                        .ToListAsync()
                        .ConfigureAwait(false);
                }
            }
        }
    }
}