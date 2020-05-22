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
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using Npgsql;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Messages
{
    public class CreateMessagesService : ICreateMessagesService
    {
        private IConversationsService ConversationsService => appServiceProvider.ConversationsService;
        private ILoadChannelsService LoadChannelsService => appServiceProvider.LoadChannelsService;
        private IAttachmentsService AttachmentsService => appServiceProvider.AttachmentsService;
        private ILoadUsersService LoadUsersService => appServiceProvider.LoadUsersService;
        private INodeRequestSender NodeRequestSender => appServiceProvider.NodeRequestSender;
        private readonly IAppServiceProvider appServiceProvider;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public CreateMessagesService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.appServiceProvider = appServiceProvider;
            this.contextFactory = contextFactory;
        }
        public async Task<List<MessageDto>> SaveMessagesAsync(IEnumerable<MessageDto> messages, long userId)
        {
            var groupedMessages = messages.GroupBy(opt => opt.ConversationType);
            List<MessageDto> resultMessages = new List<MessageDto>();
            foreach (var group in groupedMessages)
            {
                switch (group.Key)
                {
                    case ConversationType.Dialog:
                        {
                            resultMessages.AddRange(await SaveDialogMessagesAsync(group.ToList(), userId).ConfigureAwait(false));
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            resultMessages.AddRange(await SaveChatMessagesAsync(group).ConfigureAwait(false));
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            resultMessages.AddRange(await SaveChannelMessagesAsync(group).ConfigureAwait(false));
                        }
                        break;
                }
            }
            return resultMessages;
        }
        private async Task<List<MessageDto>> SaveChannelMessagesAsync(IEnumerable<MessageDto> messages)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messagesCondition = PredicateBuilder.New<Message>();
                messagesCondition = messages.Aggregate(messagesCondition,
                    (current, value) => current.Or(opt => opt.ChannelId == value.ConversationId && opt.GlobalId == value.GlobalId).Expand());
                List<Message> existingMessages = await context.Messages.Where(messagesCondition).ToListAsync().ConfigureAwait(false);
                IEnumerable<MessageDto> nonExistingMessages = messages.Where(opt => !existingMessages.Any(p => p.GlobalId == opt.GlobalId && p.ChannelId == opt.ConversationId));
                var newMessages = MessageConverter.GetMessages(nonExistingMessages);
                await context.AddRangeAsync(newMessages.OrderBy(opt => opt.SendingTime)).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                var groupedMessages = newMessages.GroupBy(opt => opt.ChannelId.GetValueOrDefault());
                foreach (var group in groupedMessages)
                {
                    var lastMessage = group.OrderByDescending(opt => opt.SendingTime).FirstOrDefault();
                    if (lastMessage != null)
                    {
                        ConversationsService.UpdateConversationLastMessageId(group.Key, ConversationType.Channel, lastMessage.GlobalId);
                    }
                }
                return messages.ToList();
            }
        }
        public async Task SaveForwardedMessagesAsync(List<MessageDto> messages)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {               
                var messagesConfition = PredicateBuilder.New<Message>();
                messagesConfition = messages.Aggregate(messagesConfition,
                    (current, value) => current.Or(opt => opt.GlobalId == value.GlobalId).Expand());
                var convGroups = messages.GroupBy(message => message.ConversationType);
                List<Message> existingMessages = new List<Message>();
                foreach (var group in convGroups)
                {
                    var groupCondition = PredicateBuilder.New<Message>();
                    switch (group.Key)
                    {
                        case ConversationType.Dialog:
                            {
                                groupCondition = group.Aggregate(groupCondition,
                                    (current, value) => current.Or(opt => opt.ReceiverId == value.ReceiverId && opt.SenderId == value.SenderId).Expand());
                            }
                            break;
                        case ConversationType.Chat:
                            {
                                groupCondition = group.Aggregate(groupCondition,
                                    (current, value) => current.Or(opt => opt.ChatId == value.ConversationId).Expand());
                            }
                            break;
                        case ConversationType.Channel:
                            {
                                groupCondition = group.Aggregate(groupCondition,
                                    (current, value) => current.Or(opt => opt.ChannelId == value.ConversationId).Expand());
                            }
                            break;
                    }
                    existingMessages.AddRange(
                        await context.Messages
                           .AsNoTracking()
                           .Where(messagesConfition)
                           .Where(groupCondition)
                           .ToListAsync()
                           .ConfigureAwait(false));
                }
                if (existingMessages.Count < messages.Count)
                {
                    messages = messages.Where(message => !existingMessages.Any(opt => opt.GlobalId == message.GlobalId))?.ToList();
                    if (messages != null)
                    {
                        var dialogMessages = messages.Where(opt => opt.ConversationType == ConversationType.Dialog);
                        if (dialogMessages != null)
                        {
                            var groupDialogMessages = dialogMessages.GroupBy(opt => new { opt.SenderId, opt.ReceiverId });
                            foreach (var group in groupDialogMessages)
                            {
                                var dialogs = await context.Dialogs
                                    .Where(dialog => (dialog.FirstUID == group.Key.SenderId && dialog.SecondUID == group.Key.ReceiverId)
                                    || (dialog.FirstUID == group.Key.ReceiverId && dialog.SecondUID == group.Key.SenderId))
                                    .ToListAsync()
                                    .ConfigureAwait(false);
                                if (!dialogs.Any())
                                {
                                    dialogs = new List<Dialog>
                                    {
                                        new Dialog
                                        {
                                            FirstUID = group.Key.SenderId.Value,
                                            SecondUID = group.Key.ReceiverId.Value
                                        }
                                    };
                                    if (group.Key.SenderId != group.Key.ReceiverId)
                                    {
                                        dialogs.Add(
                                        new Dialog
                                        {
                                            FirstUID = group.Key.ReceiverId.Value,
                                            SecondUID = group.Key.SenderId.Value
                                        });
                                    }

                                    await context.Dialogs.AddRangeAsync(dialogs).ConfigureAwait(false);
                                }
                                foreach (var item in group)
                                {
                                    item.ConversationType = ConversationType.Dialog;
                                    item.ConversationId = dialogs.FirstOrDefault().Id;
                                }
                            }
                        }
                        var newMessages = MessageConverter.GetMessages(messages);
                        await context.Messages.AddRangeAsync(newMessages).ConfigureAwait(false);
                        await context.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
            }
        }
        private async Task<List<MessageDto>> SaveDialogMessagesAsync(List<MessageDto> messages, long messagesOwnerId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        List<Dialog> userDialogs = await context.Dialogs
                                .Where(opt => opt.FirstUID == messagesOwnerId)
                                .ToListAsync()
                                .ConfigureAwait(false);
                        List<MessageDto> newDialogsMessages = new List<MessageDto>();
                        List<MessageDto> existingDialogMessages = new List<MessageDto>();
                        List<long> nonExistingUsersId = new List<long>();
                        List<Dialog> newDialogs = new List<Dialog>();
                        List<Message> newMessages = new List<Message>();
                        foreach (var message in messages)
                        {
                            if (!message.Deleted)
                            {
                                Dialog dialog = userDialogs.FirstOrDefault(opt =>
                                    (opt.FirstUID == message.SenderId && opt.SecondUID == message.ReceiverId)
                                || (opt.SecondUID == message.SenderId && opt.FirstUID == message.ReceiverId));
                                if (dialog == null)
                                {
                                    newDialogsMessages.Add(message);
                                    if (message.SenderId == messagesOwnerId && !nonExistingUsersId.Contains(message.ReceiverId.GetValueOrDefault()))
                                    {
                                        nonExistingUsersId.Add(message.ReceiverId.GetValueOrDefault());
                                    }
                                    else if (message.ReceiverId == messagesOwnerId && !nonExistingUsersId.Contains(message.SenderId.GetValueOrDefault()))
                                    {
                                        nonExistingUsersId.Add(message.SenderId.GetValueOrDefault());
                                    }
                                }
                                else
                                {
                                    message.ConversationId = dialog.Id;
                                    existingDialogMessages.Add(message);
                                }
                            }
                        }
                        if (existingDialogMessages.Any())
                        {
                            var messagesCondition = PredicateBuilder.New<Message>();
                            messagesCondition = existingDialogMessages.Aggregate(messagesCondition,
                                (current, value) => current.Or(opt => opt.GlobalId == value.GlobalId && opt.DialogId == value.ConversationId).Expand());
                            List<Message> existingMessages = await context.Messages.Where(messagesCondition).ToListAsync().ConfigureAwait(false);
                            var additionalMessages = MessageConverter.GetMessages(
                                    existingDialogMessages.Where(
                                        messageDto => !existingMessages.Any(existing => messageDto.GlobalId == existing.GlobalId
                                        && messageDto.ConversationId == existing.DialogId)));
                            newMessages.AddRange(additionalMessages);
                        }
                        foreach (long userId in nonExistingUsersId)
                        {
                            newDialogs.Add(new Dialog
                            {
                                FirstUID = messagesOwnerId,
                                SecondUID = userId
                            });
                            if (messagesOwnerId != userId)
                            {
                                newDialogs.Add(new Dialog
                                {
                                    FirstUID = userId,
                                    SecondUID = messagesOwnerId
                                });
                            }
                        }
                        await context.AddRangeAsync(newDialogs).ConfigureAwait(false);
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        foreach (var message in newDialogsMessages)
                        {
                            Dialog ownerDialog = newDialogs.FirstOrDefault(opt =>
                              opt.FirstUID == messagesOwnerId
                              && ((opt.FirstUID == message.SenderId && opt.SecondUID == message.ReceiverId)
                              || (opt.SecondUID == message.SenderId && opt.FirstUID == message.ReceiverId)));
                            message.ConversationId = ownerDialog.Id;
                        }
                        newMessages.AddRange(MessageConverter.GetMessages(newDialogsMessages));
                        await context.AddRangeAsync(newMessages.OrderBy(opt => opt.SendingTime)).ConfigureAwait(false);
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        transaction.Commit();
                        var groupedMessages = newMessages.GroupBy(opt => opt.DialogId.GetValueOrDefault());
                        foreach (var group in groupedMessages)
                        {
                            var lastMessage = group.OrderByDescending(opt => opt.SendingTime).FirstOrDefault();
                            if (lastMessage != null)
                            {
                                ConversationsService.UpdateConversationLastMessageId(group.Key, ConversationType.Dialog, lastMessage.GlobalId);
                            }
                        }
                        return MessageConverter.GetMessagesDto(newMessages);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new MessageException("Save messages error.", ex);
                    }
                }
            }
        }
        private async Task<List<MessageDto>> SaveChatMessagesAsync(IEnumerable<MessageDto> messages)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messagesCondition = PredicateBuilder.New<Message>();
                messagesCondition = messages.Aggregate(messagesCondition,
                    (current, value) => current.Or(opt => opt.ChatId == value.ConversationId && opt.GlobalId == value.GlobalId).Expand());
                var existingMessages = await context.Messages.Where(messagesCondition).ToListAsync().ConfigureAwait(false);                
                var nonExistingMessages = messages.Where(opt => !existingMessages.Any(p => p.GlobalId == opt.GlobalId));                
                var newMessages = MessageConverter.GetMessages(nonExistingMessages);
                await context.Messages.AddRangeAsync(newMessages).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                var groupedMessages = newMessages.GroupBy(opt => opt.ChatId.GetValueOrDefault());
                foreach (var group in groupedMessages)
                {
                    var lastMessage = group.OrderByDescending(opt => opt.SendingTime).FirstOrDefault();
                    if (lastMessage != null)
                    {
                        ConversationsService.UpdateConversationLastMessageId(group.Key, ConversationType.Chat, lastMessage.GlobalId);
                    }
                }
                return messages.ToList();
            }
        }
        public async void DownloadMessageHistoryAsync(long nodeId, long conversationId, ConversationType conversationType, Guid? messageId, bool direction = true, int length = 1000)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    var nodeConnection = NodeData.ConnectionsService.GetNodeConnection(nodeId);
                    if (nodeConnection != null)
                    {
                        List<MessageDto> loadedMessages = await NodeRequestSender.GetMessagesAsync(nodeConnection, conversationId, conversationType, messageId, null, direction, length)
                            .ConfigureAwait(false);
                        while (loadedMessages.Any())
                        {
                            IEnumerable<Message> messages = MessageConverter.GetMessages(loadedMessages);
                            var messagesCondition = PredicateBuilder.New<Message>();
                            messagesCondition = messages.Aggregate(messagesCondition,
                                (current, value) => current.Or(opt => opt.GlobalId == value.GlobalId).Expand());
                            List<Message> existingMessages = await context.Messages.Where(messagesCondition).ToListAsync().ConfigureAwait(false);
                            messages = messages.Where(opt => existingMessages.All(p => p.GlobalId != opt.GlobalId));
                            var navigationMessage = direction ? messages.LastOrDefault() : messages.FirstOrDefault();
                            loadedMessages = await NodeRequestSender.GetMessagesAsync(nodeConnection, conversationId, conversationType, navigationMessage.GlobalId, null, direction, length).ConfigureAwait(false);
                            await context.AddRangeAsync(messages).ConfigureAwait(false);
                            await context.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async void SaveMessageAsync(MessageDto messageDto)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    Message newMessage = MessageConverter.GetMessage(messageDto);
                    await context.Messages.AddAsync(newMessage).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    ConversationsService.UpdateConversationLastMessageId(messageDto.ConversationId, messageDto.ConversationType, messageDto.GlobalId);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async Task<MessageDto> CreateChatMessageAsync(MessageDto message, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var chatQuery = from chat in context.Chats
                                join chatUser in context.ChatUsers on chat.Id equals chatUser.ChatId into jointable
                                from chatUser in jointable.DefaultIfEmpty()
                                where
                                    chatUser.UserId == userId &&
                                    chatUser.Banned == false &&
                                    chatUser.Deleted == false &&
                                    chat.Deleted == false &&
                                    chat.Id == message.ConversationId
                                select new
                                {
                                    Chat = chat,
                                    ChatUser = chatUser
                                };
                var queryResult = await chatQuery.AsNoTracking().FirstOrDefaultAsync().ConfigureAwait(false);
                if (queryResult?.Chat != null && queryResult.ChatUser != null)
                {
                    message.NodesIds = queryResult.Chat.NodesId?.ToList();
                    SaveMessageAsync(message);
                    return message;
                }
                if (queryResult == null)
                {
                    Chat chat = await context.Chats.FirstOrDefaultAsync(opt => opt.Id == message.ConversationId).ConfigureAwait(false);
                    if (chat == null)
                    {
                        throw new ConversationNotFoundException(message.ConversationId);
                    }

                    throw new UserIsNotInConversationException(userId, message.ConversationId);
                }
            }
            throw new MessageException();
        }
        public async Task<MessageDto> CreateChannelMessageAsync(MessageDto message)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    ChannelUser senderChannelUser = await context.ChannelUsers
                    .Include(opt => opt.Channel)
                    .FirstOrDefaultAsync(opt =>
                           opt.ChannelId == message.ConversationId
                        && opt.UserId == message.SenderId
                        && opt.ChannelUserRole >= ChannelUserRole.Administrator)
                    .ConfigureAwait(false);
                    if (senderChannelUser == null)
                    {
                        UserVm user = await LoadUsersService.GetUserAsync(message.SenderId.GetValueOrDefault()).ConfigureAwait(false);
                        if (user == null)
                        {
                            throw new UserNotFoundException(message.SenderId.GetValueOrDefault());
                        }
                        ChannelVm channel = await LoadChannelsService.GetChannelByIdAsync(message.ConversationId).ConfigureAwait(false);
                        if (channel == null)
                        {
                            throw new ConversationNotFoundException(message.ConversationId);
                        }
                        throw new PermissionDeniedException();
                    }
                    if (senderChannelUser.ChannelUserRole == ChannelUserRole.Administrator && (senderChannelUser.Banned || senderChannelUser.Deleted))
                    {
                        throw new PermissionDeniedException();
                    }
                    message.NodesIds = senderChannelUser.Channel.NodesId?.ToList();
                    SaveMessageAsync(message);
                    return message;
                }
            }
            catch (PostgresException ex)
            {
                if (ex.ConstraintName == "FK_Messages_Channels_ChannelId")
                {
                    throw new ConversationNotFoundException(message.ConversationId);
                }
                else if (ex.ConstraintName == "Messages_SenderId_fkey")
                {
                    throw new UserNotFoundException(message.SenderId.GetValueOrDefault());
                }
                throw new MessageException("Database error.", ex);
            }
        }
        public async Task<List<MessageDto>> CreateDialogMessageAsync(MessageDto message)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                try
                {
                    NpgsqlParameter sender_id = new NpgsqlParameter("sender_id", message.SenderId);
                    NpgsqlParameter receiver_id = new NpgsqlParameter("receiver_id", message.ReceiverId);
                    NpgsqlParameter datesend = new NpgsqlParameter("datesend", message.SendingTime);
                    NpgsqlParameter replyto;
                    NpgsqlParameter lifetime;
                    NpgsqlParameter mtext;
                    if (message.Text == null || string.IsNullOrWhiteSpace(message.Text))
                    {
                        mtext = new NpgsqlParameter("mtext", DBNull.Value);
                    }
                    else
                    {
                        mtext = new NpgsqlParameter("mtext", message.Text) { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar };
                    }

                    if (message.Replyto != null)
                    {
                        replyto = new NpgsqlParameter("replyto", message.Replyto.Value);
                    }
                    else
                    {
                        replyto = new NpgsqlParameter("replyto", DBNull.Value);
                    }

                    if (message.Lifetime == null)
                    {
                        lifetime = new NpgsqlParameter("lifetime", DBNull.Value);
                    }
                    else
                    {
                        lifetime = new NpgsqlParameter("lifetime", DateTime.UtcNow.ToUnixTime() + message.Lifetime);
                    }

                    NpgsqlParameter global_id = new NpgsqlParameter("global_id", message.GlobalId);
                    List<Message> newMessages;
                    newMessages = await context.Messages.Include(opt => opt.Dialog)
                        .FromSql("SELECT * FROM insert_dialog_messages(@sender_id, @receiver_id, @datesend, @mtext, @global_id, @replyto, @lifetime)",
                            sender_id, receiver_id, datesend, mtext, replyto, global_id, lifetime).ToListAsync().ConfigureAwait(false);
                    newMessages = newMessages.OrderByDescending(opt => opt.Dialog?.FirstUID == message.SenderId).ToList();
                    var nodesIds = await ConversationsService.GetConversationNodesIdsAsync(ConversationType.Dialog, newMessages[0].DialogId.Value).ConfigureAwait(false);
                    newMessages.ForEach(opt => opt.NodesIds = nodesIds?.ToArray());
                    List<MessageDto> resultMessages = MessageConverter.GetMessagesDto(newMessages);
                    if (message.Attachments != null && message.Attachments.Any() && newMessages != null && newMessages.Any())
                    {
                        List<AttachmentDto> attachments = await AttachmentsService.SaveMessageAttachmentsAsync(message.Attachments, newMessages.Select(opt => opt.Id).ToList()).ConfigureAwait(false);
                        resultMessages.ForEach(mess => mess.Attachments = message.Attachments);
                    }
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    foreach (var mess in resultMessages)
                    {
                        ConversationsService.UpdateConversationLastMessageId(mess.ConversationId, ConversationType.Dialog, mess.GlobalId);
                    }
                    return resultMessages;
                }
                catch (PostgresException ex)
                {
                    if (ex.SqlState == "23503")
                    {
                        if (ex.ConstraintName == "Dialogs_FirstUID_fkey")
                        {
                            throw new UserNotFoundException(message.SenderId.GetValueOrDefault());
                        }

                        if (ex.ConstraintName == "Dialogs_SecondUID_fkey")
                        {
                            throw new UserNotFoundException(message.ReceiverId.GetValueOrDefault());
                        }
                    }
                    else if (ex.SqlState == "BLOCK")
                    {
                        throw new UserBlockedException(ex.MessageText);
                    }
                    throw new MessageException("Database error.", ex);
                }
                catch (Exception ex)
                {
                    context.Database.CloseConnection();
                    throw new MessageException("An error occurred while sending the message.", ex);
                }
            }
        }
    }
}