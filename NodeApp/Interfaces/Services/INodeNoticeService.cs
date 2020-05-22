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
using System.Net.WebSockets;
using System.Threading.Tasks;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;

namespace NodeApp.Interfaces
{
    public interface INodeNoticeService
    {
        void SendAddUsersToChatNodeNoticeAsync(ChatVm chat, long requestorId);
        void SendBlockSegmentsNodeNoticeAsync(IEnumerable<BlockSegmentVm> blockSegments);
        void SendChangeChatUsersNodeNoticeAsync(IEnumerable<ChatUserVm> chatUsers, long chatId, long requestorId, ChatVm chat);
        void SendChannelNodeNoticeAsync(ChannelVm channel, long requestorId, IEnumerable<ChannelUserVm> subscribers);
        void SendChannelUsersNodeNoticeAsync(IEnumerable<ChannelUserVm> channelUsers, long requestorId, ChannelVm channel);
        void SendChatMessagesReadNodeNoticeAsync(List<MessageDto> messages, long chatId, long requestorId);
        void SendClientDisconnectedNodeNoticeAsync(long userId, WebSocket nodeWebSocket);
        void SendDeleteConversationsNodeNoticeAsync(long conversationId, ConversationType conversationType, long requestorId);
        void SendDeleteFilesNodeNoticeAsync(List<long> filesId);
        void SendDeleteUserKeysNodeNoticeAsync(IEnumerable<long> keysId, long userId);
        void SendDeleteUsersNodeNoticeAsync(List<long> usersId);
        void SendDeleteUsersNodeNoticeAsync(long userId);
        void SendDialogMessagesReadNoticeAsync(IEnumerable<Guid> messagesId, UserVm senderUser, UserVm receiverUser);
        void SendEditChatsNodeNoticeAsync(List<ChatVm> chats);
        void SendEditUsersNodeNoticeAsync(List<ShortUser> users, List<BlockSegmentVm> blockSegments);
        void SendEditUsersNodeNoticeAsync(ShortUser user, BlockSegmentVm blockSegment);
        void SendMessagesDeletedNodeNoticeAsync(long conversationId, ConversationType conversationType, List<MessageVm> messages, long requestorId);
        void SendMessagesUpdateNodeNoticeAsync(List<MessageDto> messages, long conversationId, ConversationType conversationType, long editorUserId);
        void SendNewBlockNodeNoticeAsync(BlockVm block);
        void SendNewChannelMessageNodeNoticeAsync(MessageVm message);
        void SendNewChatMessageNodeNoticeAsync(MessageVm newMessage);
        void SendNewChatsNodeNoticeAsync(List<ChatVm> chats);
        void SendNewDialogMessageNodeNoticeAsync(MessageVm newMessage);
        void SendNewFilesNodeNoticeAsync(FileInfoVm fileInfo, byte[] privateData, long keyId);
        void SendNewKeysBlockNoticeAsync(IEnumerable<KeyVm> keys, long userId);
        Task SendNewNodeKeysNodeNoticeAsync(byte[] publicKey, byte[] signPublicKey, long keyId, long expirationTime);
        void SendNewUsersNodeNoticeAsync(IEnumerable<ShortUser> users, IEnumerable<BlockSegmentVm> blockSegments);
        void SendNewUsersNodeNoticeAsync(ShortUser user, BlockSegmentVm blockSegment);
        void SendPollingNodeNoticeAsync(Guid pollId, long conversationId, ConversationType conversationType, List<byte> optionsId, long votedUserId, List<long> nodesId);
        void SendPollingNodeNoticeAsync(Guid pollId, long conversationId, ConversationType conversationType, List<PollVoteVm> signedOptions, long votedUserId, List<long> nodesId);
        void SendProxyUsersNotificationsNodeNoticeAsync(byte[] communicationData, long userId, byte[] publicKey, NodeConnection nodeConnection);
        void SendUserNodeChangedNodeNoticeAsync(long userId, long newNodeId);
        void SendUsersAddedToUserBlacklistNodeNoticeAsync(List<long> usersId, long userId);
        void SendUsersRemovedFromBlacklistNodeNoticeAsync(IEnumerable<long> usersId, long userId);
        Task SendPendingMessagesAsync(long nodeId);
        void SendAllMessagesDeletedNodeNoticeAsync(long userId, long? conversationId, ConversationType? conversationType);
        void SendConverationActionNodeNoticeAsync(long userId, long? dialogUserId, long conversationId, ConversationType conversationType, ConversationAction conversationAction, List<long> nodesIds);
    }
}