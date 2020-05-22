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
using Newtonsoft.Json.Linq;
using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Enums;
using ObjectsLibrary.Exceptions;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class AttachmentsService : IAttachmentsService
    {
        private readonly ICreateMessagesService _createMessagesService;
        private readonly ILoadMessagesService _loadMessagesService;
        private readonly ILoadDialogsService _loadDialogsService;
        private readonly IFilesService _filesService;
        private readonly IPollsService _pollsService;
        private readonly INodeRequestSender _nodeRequestSender;
        private readonly ILoadChatsService _loadChatsService;
        private readonly ILoadUsersService _loadUsersService;
        private readonly IConnectionsService _connectionsService;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public AttachmentsService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            _createMessagesService = appServiceProvider.CreateMessagesService;
            _loadMessagesService = appServiceProvider.LoadMessagesService;
            _loadDialogsService = appServiceProvider.LoadDialogsService;
            _filesService = appServiceProvider.FilesService;
            _pollsService = appServiceProvider.PollsService;
            _nodeRequestSender = appServiceProvider.NodeRequestSender;
            _loadChatsService = appServiceProvider.LoadChatsService;
            _loadUsersService = appServiceProvider.LoadUsersService;
            _connectionsService = appServiceProvider.ConnectionsService;
            this.contextFactory = contextFactory;
        }
        public async Task<List<MessageVm>> LoadForwardedMessagesAsync(ForwardedMessagesInfo forwarded, long? requestorId)
        {
            var messagesCondition = PredicateBuilder.New<Message>();
            List<Message> messages = new List<Message>();
            switch (forwarded.ConversationType)
            {
                case ConversationType.Chat:
                    {
                        messagesCondition = forwarded.MessagesGlobalId.Aggregate(messagesCondition,
                            (current, value) => current.Or(opt => opt.GlobalId == value && opt.ChatId == forwarded.ConversationId).Expand());
                        break;
                    }
                case ConversationType.Dialog:
                    {
                        messagesCondition = forwarded.MessagesGlobalId.Aggregate(messagesCondition,
                            (current, value) => current.Or(opt => opt.GlobalId == value && opt.DialogId == forwarded.ConversationId).Expand());
                        break;
                    }
                case ConversationType.Channel:
                    {
                        messagesCondition = forwarded.MessagesGlobalId.Aggregate(messagesCondition,
                            (current, value) => current.Or(opt => opt.GlobalId == value && opt.ChannelId == forwarded.ConversationId).Expand());
                    }
                    break;
                case ConversationType.Unknown:
                    {
                        messagesCondition = forwarded.MessagesGlobalId.Aggregate(messagesCondition,
                            (current, value) => current.Or(opt => opt.GlobalId == value && opt.ChannelId == null && opt.DialogId == null && opt.ChatId == null).Expand());
                    }
                    break;
                default:
                    return null;
            }
            using (MessengerDbContext context = contextFactory.Create())
            {
                messages = await context.Messages
                        .AsNoTracking()
                        .Include(opt => opt.Attachments)
                        .Where(messagesCondition)
                        .ToListAsync().ConfigureAwait(false);
                foreach (var message in messages)
                {
                    if (message.ChannelId != null)
                    {
                        message.SenderId = null;
                    }

                    message.Attachments = message.Attachments.Where(opt => opt.Type != (short)AttachmentType.ForwardedMessages).ToList();
                }
                return MessageConverter.GetMessagesVm(MessageConverter.GetMessagesDto(messages), requestorId);
            }
        }
        public async Task<FileInfoVm> LoadFileInfoAsync(string fileId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var fileInfo = await context.FilesInfo
                    .AsNoTracking()
                    .FirstOrDefaultAsync(file => file.Id == fileId && file.Deleted == false).ConfigureAwait(false);
                if (fileInfo == null)
                {
                    List<FileInfoVm> filesInfo = await _nodeRequestSender.GetFilesInformationAsync(new List<string> { fileId }, null).ConfigureAwait(false);
                    if (!filesInfo.IsNullOrEmpty())
                    {
                        return filesInfo.FirstOrDefault();
                    }
                }
                return FileInfoConverter.GetFileInfoVm(fileInfo);
            }
        }
        public async Task<List<AttachmentDto>> SaveMessageAttachmentsAsync(List<AttachmentDto> attachments, List<long> messagesId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var tempAttachments = new List<Attachment>();
                foreach (long messageId in messagesId)
                {
                    var newAttachments = AttachmentConverter.GetAttachments(attachments, messageId);
                    tempAttachments.AddRange(newAttachments);
                }
                await context.Attachments.AddRangeAsync(tempAttachments).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return attachments;
            }
        }
        public async Task<bool> ValidateAttachmentsAsync(IEnumerable<AttachmentVm> attachments, MessageVm message, long? userId, bool anotherNodeMessage)
        {
            bool isValid;
            foreach (var attachment in attachments)
            {
                switch (attachment.Type)
                {
                    case AttachmentType.ForwardedMessages:
                        {
                            isValid = await ValidateForwardedMessagesAttachment(attachment, userId, anotherNodeMessage).ConfigureAwait(false);
                            attachment.Hash = Array.Empty<byte>();
                        }
                        break;
                    case AttachmentType.Picture:
                    case AttachmentType.File:
                    case AttachmentType.Audio:
                    case AttachmentType.Video:
                        {
                            isValid = await ValidateMediaAttachment(attachment).ConfigureAwait(false);
                        }
                        break;
                    case AttachmentType.EncryptedMessage:
                        {
                            await ThrowIfEncryptionForbiddenAsync(message.SenderId ?? userId.GetValueOrDefault(), message.ReceiverId.Value);
                            isValid = true;
                        }
                        break;
                    case AttachmentType.KeyMessage:
                        {
                            await ThrowIfEncryptionForbiddenAsync(message.SenderId ?? userId.GetValueOrDefault(), message.ReceiverId.Value);
                            isValid = ValidateKeyAttachment(attachment);
                        }
                        break;
                    case AttachmentType.Poll:
                        {
                            if (message.ConversationType == ConversationType.Dialog)
                            {
                                return false;
                            }

                            isValid = await HandleAndValidatePollAttachmentAsync(attachment, message).ConfigureAwait(false);
                        }
                        break;
                    case AttachmentType.VoiceMessage:
                        {
                            isValid = await ValidateVoiceMessageAttachmentAsync(attachment).ConfigureAwait(false);
                        }
                        break;
                    case AttachmentType.VideoMessage:
                        {
                            isValid = false;
                        }
                        break;
                    default:
                        {
                            isValid = false;
                        }
                        break;
                }
                if (!isValid)
                {
                    return false;
                }
            }
            return true;
        }

        private async Task<bool> ValidateVoiceMessageAttachmentAsync(AttachmentVm attachment)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                if (attachment.Payload is string)
                {
                    var existingFile = await context.FilesInfo
                       .FirstOrDefaultAsync(file => file.Id == (string)attachment.Payload && file.Deleted == false).ConfigureAwait(false);
                    return existingFile?.Size <= 1024 * 1024 * 2;
                }
                else if (attachment.Payload is FileInfoVm fileInfo)
                {
                    var existingFile = await context.FilesInfo
                        .FirstOrDefaultAsync(file => file.Id == fileInfo.FileId && file.Deleted == false).ConfigureAwait(false);
                    return existingFile?.Size <= 1024 * 1024 * 2;
                }
                if (attachment.Payload is JObject jsonObject)
                {
                    var fileMessage = jsonObject.ToObject<FileInfoVm>();
                    return !string.IsNullOrEmpty(fileMessage.FileId) && fileMessage.Size.GetValueOrDefault() <= 1024 * 1024 * 2;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task DownloadAttachmentsPayloadAsync(IEnumerable<AttachmentVm> attachments, NodeConnection connection)
        {
            foreach (var attach in attachments)
            {
                await DownloadAttachmentAsync(attach, connection).ConfigureAwait(false);
            }
        }
        private async Task DownloadAttachmentAsync(AttachmentVm attachment, NodeConnection connection)
        {
            switch (attachment.Type)
            {
                case AttachmentType.Audio:
                case AttachmentType.File:
                case AttachmentType.Picture:
                case AttachmentType.Video:
                case AttachmentType.VoiceMessage:
                case AttachmentType.VideoMessage:
                    {
                        await DownloadMediaAttachmentAsync(attachment, connection).ConfigureAwait(false);
                    }
                    break;
                case AttachmentType.Poll:
                    {
                        await DownloadPollAttachmentAsync(attachment, connection).ConfigureAwait(false);
                    }
                    break;
            }
        }
        private async Task DownloadPollAttachmentAsync(AttachmentVm attachment, NodeConnection connection)
        {
            if (attachment.Payload is PollVm pollVm)
            {
                PollDto pollDto = await _nodeRequestSender.GetPollInformationAsync(
                    pollVm.ConversationId.Value,
                    pollVm.ConversationType.Value,
                    pollVm.PollId.Value,
                    connection).ConfigureAwait(false);
                await _pollsService.SavePollAsync(pollDto).ConfigureAwait(false);
            }
        }
        private async Task<bool> ValidateMediaAttachment(AttachmentVm mediaAttachment)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                if (mediaAttachment.Payload is string)
                {
                    return await context.FilesInfo
                        .AnyAsync(file => file.Id == (string)mediaAttachment.Payload && file.Deleted == false).ConfigureAwait(false);
                }
                else if (mediaAttachment.Payload is FileInfoVm fileInfo)
                {
                    return await context.FilesInfo
                        .AnyAsync(file => file.Id == fileInfo.FileId && file.Deleted == false).ConfigureAwait(false);
                }
                if (mediaAttachment.Payload is JObject jsonObject)
                {
                    var fileMessage = jsonObject.ToObject<FileInfoVm>();
                    return !string.IsNullOrEmpty(fileMessage.FileId);
                }
                else
                {
                    return false;
                }
            }
        }
        private async Task<bool> ValidateForwardedMessagesAttachment(AttachmentVm forwardedMessagesAttachment, long? userId, bool anotherNodeMessage)
        {
            ForwardedMessagesInfo fMessagesInfo = null;
            if (forwardedMessagesAttachment.Payload is string)
            {
                fMessagesInfo = ObjectSerializer.JsonToObject<ForwardedMessagesInfo>(forwardedMessagesAttachment.Payload.ToString());
            }
            else if (forwardedMessagesAttachment.Payload is List<MessageVm> messages)
            {
                if (!messages.Any())
                {
                    return true;
                }
                var message = messages.FirstOrDefault();
                if (anotherNodeMessage)
                {
                    await _createMessagesService.SaveForwardedMessagesAsync(MessageConverter.GetMessagesDto(messages)).ConfigureAwait(false);
                    forwardedMessagesAttachment.Payload = new ForwardedMessagesInfo(
                        messages.Select(opt => opt.GlobalId.Value),
                        message.ConversationType == ConversationType.Dialog
                            ? (await _loadDialogsService.GetDialogsIdByUsersIdPairAsync(message.SenderId.Value, message.ReceiverId.Value).ConfigureAwait(false)).FirstOrDefault()
                            : message.ConversationId,
                        message.ConversationType);
                    return true;
                }
                switch (message.ConversationType)
                {
                    case ConversationType.Dialog:
                        {
                            var dialogsId = await _loadDialogsService.GetDialogsIdByUsersIdPairAsync(message.SenderId.Value, message.ReceiverId.Value).ConfigureAwait(false);
                            if (dialogsId.Any())
                            {
                                fMessagesInfo = new ForwardedMessagesInfo(messages.Select(opt => opt.GlobalId.Value), dialogsId[0], ConversationType.Dialog);
                            }
                            else
                            {
                                fMessagesInfo = new ForwardedMessagesInfo(messages.Select(opt => opt.GlobalId.Value), null, ConversationType.Dialog);
                            }
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            var chat = await _loadChatsService.GetChatByIdAsync(message.ConversationId.GetValueOrDefault()).ConfigureAwait(false);
                            if (chat.Type == ChatType.Private)
                            {
                                return false;
                            }
                        }
                        break;
                    case ConversationType.Channel:
                        fMessagesInfo = new ForwardedMessagesInfo(messages.Select(opt => opt.GlobalId.Value), message.ConversationId.GetValueOrDefault(), message.ConversationType);
                        break;
                }
            }
            else
            {
                fMessagesInfo = ObjectSerializer.JsonToObject<ForwardedMessagesInfo>(ObjectSerializer.ObjectToJson(forwardedMessagesAttachment.Payload));
            }
            if (fMessagesInfo == null)
            {
                return false;
            }

            return await _loadMessagesService.CanUserGetMessageAsync(fMessagesInfo.ConversationType, fMessagesInfo.ConversationId, userId).ConfigureAwait(false);
        }
        private async Task DownloadMediaAttachmentAsync(AttachmentVm attachment, NodeConnection connection)
        {
            FileInfoVm fileInfoPayload = (FileInfoVm)attachment.Payload;
            if (fileInfoPayload == null)
            {
                return;
            }

            string fileId = fileInfoPayload.FileId;
            FileInfo fileInfo = await _filesService.GetFileInfoAsync(fileId).ConfigureAwait(false);
            if (fileInfo != null && string.IsNullOrWhiteSpace(fileInfo.Url))
            {
                try
                {
                    await _nodeRequestSender.DownloadFileNodeRequestAsync(fileId, connection).ConfigureAwait(false);
                }
                catch (DownloadFileException ex)
                {
                    Logger.WriteLog(ex);
                }
            }
        }
        private async Task<bool> HandleAndValidatePollAttachmentAsync(AttachmentVm attachment, MessageVm message)
        {
            try
            {
                PollVm pollAttachment;
                if (attachment.Payload.GetType() == typeof(string))
                {
                    pollAttachment = ObjectSerializer.JsonToObject<PollVm>((string)attachment.Payload);
                }
                else
                {
                    pollAttachment = ObjectSerializer.JsonToObject<PollVm>(ObjectSerializer.ObjectToJson(attachment.Payload));
                }

                bool isValid = !string.IsNullOrWhiteSpace(pollAttachment.Title)
                    && pollAttachment.Title.Length < 100
                    && pollAttachment.PollOptions != null
                    && pollAttachment.PollOptions.All(opt => !string.IsNullOrWhiteSpace(opt.Description));
                if (isValid)
                {
                    pollAttachment = await PollConverter.InitPollConversationAsync(pollAttachment, message).ConfigureAwait(false);
                    if (pollAttachment.PollId == null || pollAttachment.PollId == Guid.Empty)
                    {
                        pollAttachment.PollId = RandomExtensions.NextGuid();
                    }
                    attachment.Payload = pollAttachment;
                    await _pollsService.SavePollAsync(PollConverter.GetPollDto(pollAttachment, message.SenderId.GetValueOrDefault())).ConfigureAwait(false);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return false;
            }
        }
        public async Task ThrowIfAttachmentsInvalidAsync(MessageVm message, bool anotherNodeMessage)
        {
            if (message.ReplyTo != null && !await _loadMessagesService.IsReplyMessageExistsAsync(message).ConfigureAwait(false))
            {
                throw new MessageException("Reply message does not exists.");
            }
            if (message.Attachments?.Any() ?? false)
            {
                try
                {
                    if (!await ValidateAttachmentsAsync(message.Attachments, message, message.SenderId, anotherNodeMessage).ConfigureAwait(false))
                    {
                        throw new InvalidAttachmentsException("Attachments is invalid.");
                    }
                }
                catch (SerializationException ex)
                {
                    throw new InvalidAttachmentsException(ex.Message);
                }
            }
        }

        public async Task<bool> CheckEditedMessageAttachmentsAsync(MessageVm message, long userId)
        {
            try
            {
                foreach (var attachment in message.Attachments)
                {
                    switch (attachment.Type)
                    {
                        case AttachmentType.Audio:
                        case AttachmentType.File:
                        case AttachmentType.Picture:
                        case AttachmentType.Video:
                            {
                                if (attachment.Payload is string stringPayload)
                                {
                                    var fileInfo = _filesService.GetFileInfoAsync(stringPayload);
                                    if (fileInfo == null)
                                    {
                                        return false;
                                    }
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            break;
                        case AttachmentType.ForwardedMessages:
                            {
                                if (attachment.Payload is ForwardedMessagesInfo messagesInfo)
                                {
                                    var messages = await _loadMessagesService.GetMessagesByIdAsync(
                                        messagesInfo.MessagesGlobalId,
                                        messagesInfo.ConversationType,
                                        messagesInfo.ConversationId.GetValueOrDefault(),
                                        userId).ConfigureAwait(false);
                                    if (messages == null || !messages.Any())
                                    {
                                        return false;
                                    }
                                }
                                else if (attachment.Payload is List<MessageVm> messages)
                                {
                                    await _createMessagesService.SaveForwardedMessagesAsync(MessageConverter.GetMessagesDto(messages)).ConfigureAwait(false);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            break;
                        case AttachmentType.Poll:
                            {
                                if (attachment.Payload is PollVm poll)
                                {
                                    var existingPoll = await _pollsService.GetPollAsync(
                                        poll.PollId.GetValueOrDefault(),
                                        message.ConversationId.GetValueOrDefault(),
                                        message.ConversationType).ConfigureAwait(false);
                                    if (existingPoll == null)
                                    {
                                        poll = await PollConverter.InitPollConversationAsync(poll, message).ConfigureAwait(false);
                                        poll.PollId = RandomExtensions.NextGuid();
                                        await _pollsService.SavePollAsync(PollConverter.GetPollDto(poll, userId)).ConfigureAwait(false);
                                        attachment.Payload = poll;
                                    }
                                }
                            }
                            break;
                        case AttachmentType.VoiceMessage:
                            {
                                await ValidateVoiceMessageAttachmentAsync(attachment);
                            }
                            break;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return false;
            }
        }
        private bool ValidateKeyAttachment(AttachmentVm attachment)
        {
            if (attachment.Payload is string strKey)
            {
                if (ObjectSerializer.TryDeserializeJson<KeyExchangeMessageVm>(strKey, out var keyMessage))
                {
                    return !keyMessage.EncryptedData.IsNullOrEmpty();
                }
            }
            if (attachment.Payload is JObject jsonObject)
            {
                var keyMessage = jsonObject.ToObject<KeyExchangeMessageVm>();
                return !keyMessage.EncryptedData.IsNullOrEmpty();
            }
            if (attachment.Payload is KeyExchangeMessageVm keyExchangeMessage)
            {
                return !keyExchangeMessage.EncryptedData.IsNullOrEmpty();
            }
            return false;
        }
        private async Task ThrowIfEncryptionForbiddenAsync(long senderId, long receiverId)
        {
            if (NodeSettings.Configs.Node.EncryptionType == EncryptionType.TotallyForbidden)
                throw new EncryptionForbiddenException("Encryption is forbidden by the node settings.");
            var receiver = await _loadUsersService.GetUserAsync(receiverId).ConfigureAwait(false);
            var sender = await _loadUsersService.GetUserAsync(senderId).ConfigureAwait(false);
            if (receiver == null || sender == null)
                return;
            if (receiver.NodeId != sender.NodeId)
            {
                var receiverNode = _connectionsService.GetNodeConnection(receiver.NodeId.Value);
                if (receiverNode == null)
                    return;
                if (receiverNode.Node.EncryptionType == EncryptionType.TotallyForbidden)
                    throw new EncryptionForbiddenException("Encryption is forbidden by the receiver node settings.");
            }
            else
            {
                if (NodeSettings.Configs.Node.EncryptionType == EncryptionType.NodeUsersForbidden)
                    throw new EncryptionForbiddenException("Encryption is forbidden by the node settings.");
            }
        }
    }
}