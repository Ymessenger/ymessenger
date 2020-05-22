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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class SendMessagesRequestHandler : IRequestHandler
    {
        private readonly SendMessagesRequest request;
        private readonly long userId;
        private readonly ClientConnection current;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IAttachmentsService attachmentsService;
        private readonly ICreateMessagesService createMessagesService;
        private readonly ILoadDialogsService loadDialogsService;


        public SendMessagesRequestHandler(
            Request request,
            ClientConnection current,
            INodeNoticeService nodeNoticeService,
            IConversationsNoticeService conversationsNoticeService,
            IAttachmentsService attachmentsService,
            ICreateMessagesService createMessagesService,
            ILoadDialogsService loadDialogsService)
        {
            this.request = (SendMessagesRequest)request;
            userId = current.UserId ?? 0;
            this.current = current;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.attachmentsService = attachmentsService;
            this.createMessagesService = createMessagesService;
            this.loadDialogsService = loadDialogsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            var groupingMessages = request.Messages.GroupBy(opt => opt.ConversationType);
            List<MessageVm> resultMessages = new List<MessageVm>();
            try
            {
                foreach (var group in groupingMessages)
                {
                    switch (group.Key)
                    {
                        case ConversationType.Dialog:
                            resultMessages.AddRange(await SendDialogMessagesAsync(group).ConfigureAwait(false));
                            break;
                        case ConversationType.Chat:
                            resultMessages.AddRange(await SendChatMessagesAsync(group).ConfigureAwait(false));
                            break;
                        case ConversationType.Channel:
                            resultMessages.AddRange(await SendChannelMessagesAsync(group).ConfigureAwait(false));
                            break;
                    }
                }
                return new MessagesResponse(request.RequestId, resultMessages);
            }
            catch (MessageException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Error sending message.", ErrorCode.SendMessageProblem);
            }
            catch (ConversationNotFoundException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Conversation not found.", ErrorCode.ChatIsNotValid);
            }
            catch (UserNotFoundException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, $"User with ID:{ex.UsersId.FirstOrDefault()} not found.", ErrorCode.UserNotFound);
            }
            catch (UserIsNotInConversationException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "User is not conversation.", ErrorCode.UserIsNotInChat);
            }
            catch (UserBlockedException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "User is blocked in conversation.", ErrorCode.UserBlocked);
            }
            catch(EncryptionForbiddenException ex)
            {
                return new ResultResponse(request.RequestId, ex.Message, ErrorCode.InvalidAttachment);
            }
            catch (InvalidAttachmentsException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Invalid attachments.", ErrorCode.InvalidAttachment);
            }
            catch (ConversationIsNotValidException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Conversation deleted.", ErrorCode.ChatIsNotValid);
            }
            catch (PermissionDeniedException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "The user does not have access to the conversation.", ErrorCode.PermissionDenied);
            }
        }

        private async Task<List<MessageVm>> SendDialogMessagesAsync(IEnumerable<MessageVm> messages)
        {
            List<MessageDto> resultMessages = new List<MessageDto>();
            foreach (var message in messages)
            {
                message.SenderId = userId;
                message.GlobalId = RandomExtensions.NextGuid();
                message.ConversationType = ConversationType.Dialog;
                await attachmentsService.ThrowIfAttachmentsInvalidAsync(message, false).ConfigureAwait(false);
                List<MessageDto> sentMessages = default;
                bool saveMessageFlag = true;
                if (message.Attachments?.Any() ?? false)
                {
                    var attachment = message.Attachments.FirstOrDefault(opt => opt.Type == AttachmentType.EncryptedMessage);
                    if (attachment != null)
                    {
                        var ecnryptedMessage = ObjectSerializer.JsonToObject<EncryptedMessage>(attachment.Payload.ToString());
                        saveMessageFlag = ecnryptedMessage.SaveFlag > 0;
                    }
                }
                message.SendingTime = DateTime.UtcNow.ToUnixTime();
                if (saveMessageFlag)
                {
                    sentMessages = await createMessagesService.CreateDialogMessageAsync(MessageConverter.GetMessageDto(message)).ConfigureAwait(false);
                    var dialogs = await loadDialogsService.GetUsersDialogsAsync(message.SenderId.Value, message.ReceiverId.Value).ConfigureAwait(false);
                    var receiverDialog = dialogs.FirstOrDefault(dialog => dialog.FirstUserId == message.ReceiverId);
                    UsersConversationsCacheService.Instance.NewMessageUpdateUserDialogsAsync(MessageConverter.GetMessageVm(sentMessages[0], current.UserId), receiverDialog.Id);
                }
                else
                {
                    List<long> dialogsId = await loadDialogsService.GetDialogsIdByUsersIdPairAsync(message.SenderId.GetValueOrDefault(), message.ReceiverId.GetValueOrDefault()).ConfigureAwait(false);
                    sentMessages = dialogsId.Select(opt =>
                    {
                        MessageDto tempMessage = new MessageDto(MessageConverter.GetMessageDto(message))
                        {
                            ConversationId = opt
                        };
                        return tempMessage;
                    })
                    .ToList();
                }
                if (!sentMessages.Any())
                {
                    throw new MessageException();
                }
                conversationsNoticeService.SendNewMessageNoticeToDialogUsers(
                    sentMessages,
                    current,
                    message.ReceiverId.GetValueOrDefault(),
                    saveMessageFlag);
                nodeNoticeService.SendNewDialogMessageNodeNoticeAsync(MessageConverter.GetMessageVm(sentMessages.FirstOrDefault(), current.UserId));
                IEnumerable<ConversationPreviewVm> senderDialogs = await UsersConversationsCacheService.Instance.GetUserConversationsAsync(userId, ConversationType.Dialog).ConfigureAwait(false);
                if (senderDialogs == null || !senderDialogs.Any(opt => opt.SecondUid == message.ReceiverId))
                {
                    senderDialogs = await loadDialogsService.GetUserDialogsPreviewAsync(userId).ConfigureAwait(false);
                    UsersConversationsCacheService.Instance.UpdateUserConversations(userId, senderDialogs);
                }
                ConversationPreviewVm currentDialog = senderDialogs.FirstOrDefault(dialog => dialog.SecondUid == message.ReceiverId);
                resultMessages.Add(sentMessages.FirstOrDefault(mess => mess.ConversationId == currentDialog.ConversationId));
            }
            return MessageConverter.GetMessagesVm(resultMessages, current.UserId);
        }

        private async Task<IEnumerable<MessageVm>> SendChatMessagesAsync(IEnumerable<MessageVm> messages)
        {
            List<MessageVm> resultMessages = new List<MessageVm>();
            foreach (var message in messages)
            {
                message.SendingTime = DateTime.UtcNow.ToUnixTime();
                message.SenderId = userId;
                message.GlobalId = RandomExtensions.NextGuid();
                await attachmentsService.ThrowIfAttachmentsInvalidAsync(message, false).ConfigureAwait(false);
                MessageDto savedMessage = await createMessagesService.CreateChatMessageAsync(MessageConverter.GetMessageDto(message), userId).ConfigureAwait(false);
                MessageVm savedMessageVm = MessageConverter.GetMessageVm(savedMessage, current.UserId);
                resultMessages.Add(savedMessageVm);
                conversationsNoticeService.SendNewMessageNoticeToChatUsersAsync(savedMessage, current);
                message.Attachments = savedMessageVm.Attachments;
                nodeNoticeService.SendNewChatMessageNodeNoticeAsync(message);
                UsersConversationsCacheService.Instance.NewMessageUpdateUserChatsAsync(message);
            }
            return resultMessages;
        }

        private async Task<IEnumerable<MessageVm>> SendChannelMessagesAsync(IEnumerable<MessageVm> messages)
        {
            List<MessageVm> resultMessages = new List<MessageVm>();
            foreach (var message in messages)
            {
                message.SendingTime = DateTime.UtcNow.ToUnixTime();
                message.SenderId = userId;
                message.GlobalId = RandomExtensions.NextGuid();
                await attachmentsService.ThrowIfAttachmentsInvalidAsync(message, false).ConfigureAwait(false);
                MessageDto newMessage = await createMessagesService.CreateChannelMessageAsync(MessageConverter.GetMessageDto(message)).ConfigureAwait(false);
                MessageVm savedMessageVm = MessageConverter.GetMessageVm(newMessage, current.UserId);
                resultMessages.Add(savedMessageVm);
                conversationsNoticeService.SendNewMessageNoticeToChannelUsersAsync(newMessage, current);
                message.Attachments = savedMessageVm.Attachments;
                nodeNoticeService.SendNewChannelMessageNodeNoticeAsync(message);
                UsersConversationsCacheService.Instance.NewMessageUpdateUsersChannelsAsync(savedMessageVm);
            }
            return resultMessages;
        }

        public bool IsRequestValid()
        {
            if (userId == 0)
            {
                throw new UnauthorizedUserException();
            }

            if (!current.Confirmed)
            {
                throw new PermissionDeniedException("User is not confirmed.");
            }

            if (request.Messages == null || !request.Messages.Any())
            {
                return false;
            }
            foreach (var message in request.Messages)
            {
                if (string.IsNullOrWhiteSpace(message.Text))
                {
                    message.Text = null;
                }
                if (string.IsNullOrWhiteSpace(message.Text) && (message.Attachments == null || !message.Attachments.Any()))
                {
                    return false;
                }
                if ((message.ReceiverId != null && (message.ConversationType == ConversationType.Chat || message.ConversationType == ConversationType.Channel))
                   || (message.ReceiverId == null && message.ConversationType == ConversationType.Dialog))
                {
                    return false;
                }

                if (message.Attachments?.Where(opt => opt.Type == AttachmentType.Poll)?.Count() > 1)
                {
                    return false;
                }

                if (message.Attachments?.Count > 10)
                {
                    return false;
                }
            }
            return true;
        }
    }
}