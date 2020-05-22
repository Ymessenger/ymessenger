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
using System.Collections.Generic;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;

namespace NodeApp.Interfaces
{
    public interface IConversationsNoticeService
    {
        void SendChangeChatUsersNoticeAsync(IEnumerable<ChatUserVm> changedChatUsers, long chatId, ClientConnection clientConnection = null);
        void SendChannelNoticeAsync(ChannelVm channel, List<long> usersId, ClientConnection clientConnection = null);
        void SendConversationActionNoticeAsync(long userId, ConversationType conversationType, long conversationId, ConversationAction action);
        void SendEditChatNoticeAsync(ChatVm editedChat, ClientConnection clientConnection);
        void SendMessagesReadedNoticeAsync(IEnumerable<MessageDto> readedMessages, long conversationId, ConversationType conversationType, long userId, ClientConnection clientConnection = null);
        void SendMessagesUpdatedNoticeAsync(long conversationId, ConversationType conversationType, IEnumerable<MessageDto> messages, long userId, bool deleted, ClientConnection clientConnection);
        void SendNewChannelNoticesAsync(IEnumerable<ChannelUserVm> channelUsers, long channelId, ClientConnection clientConnection);
        void SendNewChatNoticeAsync(ChatVm newChat, ClientConnection requestorConnection);
        void SendNewMessageNoticeToChannelUsersAsync(MessageDto newMessage, ClientConnection clientConnection = null, bool sendPush = true);
        void SendNewMessageNoticeToChatUsersAsync(MessageDto newMessage, ClientConnection connection, bool sendPush = true);
        void SendNewMessageNoticeToDialogUsers(IEnumerable<MessageDto> messages, ClientConnection senderClientConnection, long receiverId, bool saveMessageFlag = true);
        void SendNewUsersAddedToChatNoticeAsync(ChatVm chat, ClientConnection clientConnection);
        void SendSystemMessageNoticeAsync(MessageDto message);
    }
}