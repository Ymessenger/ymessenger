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
using NodeApp.Converters;
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class NewMessagesNoticeHandler : ICommunicationHandler
    {
        private readonly NewMessagesNodeNotice notice;
        private readonly NodeConnection current;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IAttachmentsService attachmentsService;
        private readonly ICreateMessagesService createMessagesService;
        private readonly ICreateChannelsService createChannelsService;
        private readonly INodeRequestSender nodeRequestSender;
        private readonly ICrossNodeService crossNodeService;
        private readonly ILoadDialogsService loadDialogsService;
        public NewMessagesNoticeHandler(CommunicationObject @object,
                                        NodeConnection current,
                                        IConversationsNoticeService conversationsNoticeService,
                                        IAttachmentsService attachmentsService,
                                        ICreateMessagesService createMessagesService,
                                        ICreateChannelsService createChannelsService,
                                        INodeRequestSender nodeRequestSender,
                                        ICrossNodeService crossNodeService,
                                        ILoadDialogsService loadDialogsService)
        {
            notice = (NewMessagesNodeNotice)@object;
            this.current = current;
            this.conversationsNoticeService = conversationsNoticeService;
            this.attachmentsService = attachmentsService;
            this.createMessagesService = createMessagesService;
            this.createChannelsService = createChannelsService;
            this.nodeRequestSender = nodeRequestSender;
            this.crossNodeService = crossNodeService;
            this.loadDialogsService = loadDialogsService;
        }
        public async Task HandleAsync()
        {
            var groupingMessages = notice.Messages.GroupBy(opt => opt.ConversationType);
            foreach (var group in groupingMessages)
            {
                switch (group.Key)
                {
                    case ConversationType.Dialog:
                        {
                            await HandleDialogMessagesAsync(group).ConfigureAwait(false);
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            await HandleChatMessagesAsync(group).ConfigureAwait(false);
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            await HandleChannelMessagesAsync(group).ConfigureAwait(false);
                        }
                        break;
                }
            }
        }

        private async Task HandleChannelMessagesAsync(IEnumerable<MessageVm> messages)
        {
            foreach (var message in messages)
            {
                bool hasException = true;
                MessageDto sentMessage = null;
                while (hasException)
                {
                    try
                    {
                        if (NodeData.Instance.RoutedMessagesId.Contains(message.GlobalId.GetValueOrDefault()))
                        {
                            hasException = false;
                            continue;
                        }
                        if (message.Attachments != null)
                        {
                            foreach (var attachment in message.Attachments)
                            {
                                attachment.MessageId = 0;
                            }
                            await attachmentsService.DownloadAttachmentsPayloadAsync(message.Attachments, current).ConfigureAwait(false);
                            await attachmentsService.ThrowIfAttachmentsInvalidAsync(message, true).ConfigureAwait(false);
                        }
                        sentMessage = await createMessagesService.CreateChannelMessageAsync(MessageConverter.GetMessageDto(message)).ConfigureAwait(false);
                        hasException = false;
                    }
                    catch (ConversationNotFoundException ex)
                    {
                        var channel = await nodeRequestSender.GetChannelInformationAsync(ex.ConversationId, current).ConfigureAwait(false);
                        await createChannelsService.CreateOrUpdateUserChannelsAsync(new List<ChannelDto> { channel }).ConfigureAwait(false);
                        hasException = true;
                    }
                    catch (UserNotFoundException ex)
                    {
                        List<UserVm> users = new List<UserVm>(await nodeRequestSender.GetUsersInfoAsync(ex.UsersId.ToList(), null, current).ConfigureAwait(false));
                        await crossNodeService.CreateNewUsersAsync(users).ConfigureAwait(false);
                        hasException = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex);
                        hasException = false;
                    }
                }
                SendChannelNotificationsAsync(sentMessage);
            }
        }

        private async Task HandleChatMessagesAsync(IEnumerable<MessageVm> messages)
        {
            foreach (var message in messages)
            {
                if (NodeData.Instance.RoutedMessagesId.Contains(message.GlobalId.GetValueOrDefault()))
                {
                    continue;
                }
                if (message.Attachments != null)
                {
                    foreach (var attachment in message.Attachments)
                    {
                        attachment.MessageId = 0;
                    }
                    await attachmentsService.DownloadAttachmentsPayloadAsync(message.Attachments, current).ConfigureAwait(false);
                    await attachmentsService.ThrowIfAttachmentsInvalidAsync(message, true).ConfigureAwait(false);
                }
                MessageDto sentMessage;
                try
                {
                    sentMessage = await createMessagesService.CreateChatMessageAsync(MessageConverter.GetMessageDto(message), message.SenderId.GetValueOrDefault()).ConfigureAwait(false);
                }
                catch (ConversationNotFoundException ex)
                {
                    ChatVm chat = await nodeRequestSender.GetFullChatInformationAsync(ex.ConversationId, current).ConfigureAwait(false);
                    await crossNodeService.NewOrEditChatAsync(chat).ConfigureAwait(false);
                    sentMessage = await createMessagesService.CreateChatMessageAsync(MessageConverter.GetMessageDto(message), message.SenderId.GetValueOrDefault()).ConfigureAwait(false);
                }
                catch (UserIsNotInConversationException ex)
                {
                    List<ChatUserVm> chatUsers = await nodeRequestSender.GetChatUsersInformationAsync(
                        new List<long> { ex.UserId.GetValueOrDefault() },
                        ex.ChatId.GetValueOrDefault(),
                        current).ConfigureAwait(false);
                    await crossNodeService.AddChatUsersAsync(chatUsers).ConfigureAwait(false);
                    sentMessage = await createMessagesService.CreateChatMessageAsync(MessageConverter.GetMessageDto(message), message.SenderId.GetValueOrDefault()).ConfigureAwait(false);
                }
                SendNotificationsAsync(sentMessage);
                NodeData.Instance.RoutedMessagesId.Add(message.GlobalId.GetValueOrDefault());
            }
        }

        private async Task HandleDialogMessagesAsync(IEnumerable<MessageVm> messages)
        {
            foreach (var message in messages)
            {
                bool saveMessageFlag = true;
                try
                {
                    if (NodeData.Instance.RoutedMessagesId.Contains(message.GlobalId.GetValueOrDefault()))
                    {
                        continue;
                    }
                    if (!message.Attachments.IsNullOrEmpty())
                    {
                        foreach (var attachment in message.Attachments)
                        {
                            attachment.MessageId = 0;
                        }
                        await attachmentsService.DownloadAttachmentsPayloadAsync(message.Attachments, current).ConfigureAwait(false);
                        await attachmentsService.ThrowIfAttachmentsInvalidAsync(message, true).ConfigureAwait(false);
                    }
                    if (!message.Attachments.IsNullOrEmpty())
                    {
                        var attachment = message.Attachments.FirstOrDefault(opt => opt.Type == AttachmentType.EncryptedMessage);
                        if (attachment != null)
                        {
                            var ecnryptedMessage = (EncryptedMessage)attachment.Payload;
                            saveMessageFlag = ecnryptedMessage.SaveFlag > 0;
                        }
                    }
                    List<MessageDto> newMessages = null;
                    if (saveMessageFlag)
                    {
                        newMessages = await createMessagesService.CreateDialogMessageAsync(MessageConverter.GetMessageDto(message)).ConfigureAwait(false);
                    }
                    else
                    {
                        var dialogs = await loadDialogsService.GetUsersDialogsAsync(message.SenderId.Value, message.ReceiverId.Value);
                        var senderMessage = MessageConverter.GetMessageDto(message);
                        senderMessage.ConversationId = dialogs.FirstOrDefault(dialog => dialog.FirstUserId == message.SenderId).Id;
                        var receiverMessage = MessageConverter.GetMessageDto(message);
                        receiverMessage.ConversationId = dialogs.FirstOrDefault(dialog => dialog.FirstUserId == message.ReceiverId).Id;
                        newMessages = new List<MessageDto> { senderMessage, receiverMessage };
                        
                    }
                    newMessages.Reverse();
                    SendDialogNotificationsAsync(newMessages, saveMessageFlag);
                }
                catch (UserNotFoundException ex)
                {
                    if (ex.UsersId != null)
                    {
                        var user = (await nodeRequestSender.GetUsersInfoAsync(
                            new List<long>(ex.UsersId),
                            null,
                            current).ConfigureAwait(false)).FirstOrDefault();
                        if (user != null)
                        {
                            await crossNodeService.NewOrEditUserAsync(new ShortUser
                            {
                                UserId = user.Id.GetValueOrDefault(),
                            }, current.Node.Id).ConfigureAwait(false);
                        }

                        List<MessageDto> newMessages = await createMessagesService.CreateDialogMessageAsync(MessageConverter.GetMessageDto(message)).ConfigureAwait(false);
                        newMessages.Reverse();
                        SendDialogNotificationsAsync(newMessages, saveMessageFlag);
                    }
                }
            }
        }

        public bool IsObjectValid()
        {
            if (notice.Messages == null || !notice.Messages.Any() || current.Node == null)
            {
                return false;
            }

            foreach (var message in notice.Messages)
            {
                if ((message.ReceiverId != null && message.ConversationType == ConversationType.Chat)
                    || (message.ReceiverId == null && message.ConversationType == ConversationType.Dialog))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(message.Text) && (message.Attachments == null || !message.Attachments.Any()))
                {
                    return false;
                }
            }
            return true;
        }

        private void SendDialogNotificationsAsync(List<MessageDto> messages, bool saveMessageFlag)
        {
            var messageVm = MessageConverter.GetMessageVm(messages.ElementAt(0), null);
            conversationsNoticeService.SendNewMessageNoticeToDialogUsers(messages, null, messages.First().ReceiverId.GetValueOrDefault(), saveMessageFlag);
            UsersConversationsCacheService.Instance.NewMessageUpdateUserDialogsAsync(messageVm, messages[1].ConversationId);
        }

        private void SendNotificationsAsync(MessageDto message)
        {
            var messageVm = MessageConverter.GetMessageVm(message, null);
            conversationsNoticeService.SendNewMessageNoticeToChatUsersAsync(message, null);
            UsersConversationsCacheService.Instance.NewMessageUpdateUserChatsAsync(messageVm);
        }

        private void SendChannelNotificationsAsync(MessageDto message)
        {
            var messageVm = MessageConverter.GetMessageVm(message, null);
            conversationsNoticeService.SendNewMessageNoticeToChannelUsersAsync(message);
            UsersConversationsCacheService.Instance.NewMessageUpdateUsersChannelsAsync(messageVm);
        }
    }
}