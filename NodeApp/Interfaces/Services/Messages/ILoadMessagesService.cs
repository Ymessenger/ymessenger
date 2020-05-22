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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NodeApp.HttpServer.Models.ViewModels;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;

namespace NodeApp.Interfaces.Services.Messages
{
    public interface ILoadMessagesService
    {
        Task<bool> CanUserGetMessageAsync(ConversationType conversationType, long? conversationId, long? userId);
        Task<List<MessageDto>> GetChannelMessagesAsync(long channelId, long userId, Guid? navigationMessageId, List<AttachmentType> attachmentsTypes = null, bool direction = true, byte limit = 30);
        Task<List<KeyValuePair<long, int>>> GetChannelsUnreadedMessagesCountAsync(List<long> channelsId, long userId);
        Task<List<MessageDto>> GetChatMessagesAsync(long chatId, long userId, Guid? navMessageId, List<AttachmentType> attachmentsTypes = null, bool direction = true, byte messagesLimit = 30);
        Task<List<KeyValuePair<long, int>>> GetChatUnreadedMessagesCountAsync(List<long> chatsId, long userId);
        Task<List<MessageDto>> GetDialogMessagesAsync(long dialogId, long userId, Guid? lastId = null, List<AttachmentType> attachmentsTypes = null, bool direction = true, short limit = 30);
        Task<List<KeyValuePair<long, int>>> GetDialogUnreadedMessagesCountAsync(List<long> dialogsId, long userId);
        Task<Message> GetLastValidChannelMessageAsync(long channelId);
        Task<Message> GetLastValidChatMessageAsync(long chatId);
        Task<Message> GetLastValidDialogMessageAsync(long dialogId);
        Task<List<MessageDto>> GetMessageEditHistoryAsync(long conversationId, ConversationType conversationType, Guid messageId);
        Task<List<MessageDto>> GetMessagesAsync(long conversationId, ConversationType conversationType, bool direction, Guid? messageId, List<AttachmentType> attachmentsTypes, int limit);
        Task<List<MessageDto>> GetMessagesByIdAsync(IEnumerable<Guid> messagesId, ConversationType conversationType, long conversationId, long? userId);
        Task<List<MessageDto>> GetUserUpdatedMessagesAsync(long userId, long updatedTime, long? conversationId, ConversationType? conversationType, Guid? messageId, int limit = 1000);
        Task<bool> IsChannelMessageExistsAsync(Guid messageId, long conversationId);
        Task<bool> IsChatMessageExistsAsync(Guid messageId, long chatId);
        Task<bool> IsDialogMessageExistsAsync(Guid messageId, long senderId, long receiverId);
        Task<bool> IsReplyMessageExistsAsync(MessageVm message);
        Task<List<MessageDto>> SearchMessagesAsync(string query, ConversationType? conversationType, long? conversationId, ConversationType? navConversationType, long? navConversationId, Guid? navMessageId, long? userId, int limit = 30);
        Task<MessageDto> GetLastValidConversationMessage(ConversationType conversationType, long conversationId);
        Task<MessagesPageViewModel> GetMessagesPageAsync(long conversationId, ConversationType conversationType, int pageNumber, int limit);
    }
}