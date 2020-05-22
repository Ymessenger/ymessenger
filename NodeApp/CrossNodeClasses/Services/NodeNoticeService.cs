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
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.Services
{
    public class NodeNoticeService : INodeNoticeService
    {
        private readonly IConnectionsService _connectionsService;
        private readonly IUpdateMessagesService _updateMessagesService;
        private readonly ILoadChatsService _loadChatsService;
        private readonly ILoadDialogsService _loadDialogsService;
        private readonly ILoadChannelsService _loadChannelsService;
        private readonly IPendingMessagesService _pendingMessagesService;
        private readonly INodesService _nodesService;
        public NodeNoticeService(IAppServiceProvider appServiceProvider)
        {
            _connectionsService = appServiceProvider.ConnectionsService;
            _updateMessagesService = appServiceProvider.UpdateMessagesService;
            _loadChatsService = appServiceProvider.LoadChatsService;
            _loadDialogsService = appServiceProvider.LoadDialogsService;
            _loadChannelsService = appServiceProvider.LoadChannelsService;
            _pendingMessagesService = appServiceProvider.PendingMessagesService;
            _nodesService = appServiceProvider.NodesService;
        }
        public async void SendConverationActionNodeNoticeAsync(long userId, long? dialogUserId, long conversationId, ConversationType conversationType, ConversationAction conversationAction, List<long> nodesIds)
        {
            try
            {
                ConversationActionNodeNotice notice = new ConversationActionNodeNotice(conversationType, conversationAction, conversationId, userId, dialogUserId);
                await SendNoticeToNodesAsync(notice, nodesIds).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async void SendNewUsersNodeNoticeAsync(IEnumerable<ShortUser> users, IEnumerable<BlockSegmentVm> blockSegments)
        {
            try
            {
                CreateOrEditUsersNodeNotice notice = new CreateOrEditUsersNodeNotice(Enums.NodeNoticeCode.NewUsers, users, blockSegments);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendNewUsersNodeNoticeAsync(ShortUser user, BlockSegmentVm blockSegment)
        {
            try
            {
                CreateOrEditUsersNodeNotice notice = new CreateOrEditUsersNodeNotice(Enums.NodeNoticeCode.NewUsers, user, blockSegment);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendAllMessagesDeletedNodeNoticeAsync(long userId, long? conversationId, ConversationType? conversationType)
        {
            try
            {
                DeleteAllUserMessagesNodeNotice notice = new DeleteAllUserMessagesNodeNotice(userId, conversationId, conversationType);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async void SendChangeChatUsersNodeNoticeAsync(IEnumerable<ChatUserVm> chatUsers, long chatId, long requestorId, ChatVm chat)
        {
            try
            {
                ChangeUsersChatNodeNotice notice = new ChangeUsersChatNodeNotice(chatUsers, chatId, requestorId);
                await SendNoticeToNodesAsync(notice, chat.NodesId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendMessagesUpdateNodeNoticeAsync(List<MessageDto> messages, long conversationId, ConversationType conversationType, long editorUserId)
        {
            try
            {
                List<long> nodesId = new List<long>();
                foreach (var message in messages)
                {
                    nodesId.AddRange(message.NodesIds);
                }
                nodesId = nodesId.Distinct().ToList();
                MessagesUpdatedNodeNotice notice = new MessagesUpdatedNodeNotice(messages, editorUserId);
                await SendNoticeToNodesAsync(notice, nodesId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async Task SendNewNodeKeysNodeNoticeAsync(byte[] publicKey, byte[] signPublicKey, long keyId, long expirationTime)
        {
            try
            {
                NewNodeKeysNodeNotice notice = new NewNodeKeysNodeNotice(publicKey, signPublicKey, expirationTime, keyId);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendChannelUsersNodeNoticeAsync(IEnumerable<ChannelUserVm> channelUsers, long requestorId, ChannelVm channel)
        {
            try
            {
                ChannelUsersNodeNotice notice = new ChannelUsersNodeNotice(channelUsers, requestorId, channel.ChannelId.GetValueOrDefault());
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendDeleteUserKeysNodeNoticeAsync(IEnumerable<long> keysId, long userId)
        {
            try
            {
                DeleteUserKeysNodeNotice notice = new DeleteUserKeysNodeNotice(keysId, userId);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendUsersRemovedFromBlacklistNodeNoticeAsync(IEnumerable<long> usersId, long userId)
        {
            try
            {
                UsersRemovedFromUserBlacklistNodeNotice notice = new UsersRemovedFromUserBlacklistNodeNotice(userId, usersId);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendClientDisconnectedNodeNoticeAsync(long userId, WebSocket nodeWebSocket)
        {
            try
            {
                ClientDisconnectedNodeNotice notice = new ClientDisconnectedNodeNotice(userId);
                NodeConnection nodeConnection = _connectionsService.GetNodeConnections()
                    .FirstOrDefault(opt => opt.NodeWebSocket == nodeWebSocket);
                await SendNoticeToNodeAsync(nodeConnection, notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendNewKeysBlockNoticeAsync(IEnumerable<KeyVm> keys, long userId)
        {
            try
            {
                NewUserKeysNodeNotice notice = new NewUserKeysNodeNotice(keys, userId);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendAddUsersToChatNodeNoticeAsync(ChatVm chat, long requestorId)
        {
            try
            {
                List<long> newUsersId = chat.Users.Select(chatUser => chatUser.UserId).ToList();
                AddUsersChatNodeNotice notice = new AddUsersChatNodeNotice(chat.Id.Value, newUsersId, requestorId);
                await SendNoticeToNodesAsync(notice, chat.NodesId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendBlockSegmentsNodeNoticeAsync(IEnumerable<BlockSegmentVm> blockSegments)
        {
            try
            {
                BlockSegmentsNotice notice = new BlockSegmentsNotice(blockSegments);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendNewFilesNodeNoticeAsync(FileInfoVm fileInfo, byte[] privateData, long keyId)
        {
            try
            {
                NewFilesNodeNotice notice = new NewFilesNodeNotice(fileInfo, privateData, keyId);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendDialogMessagesReadNoticeAsync(IEnumerable<Guid> messagesId, UserVm senderUser, UserVm receiverUser)
        {
            try
            {
                List<long> nodesId = new List<long>
                {
                    senderUser.NodeId.GetValueOrDefault(),
                    receiverUser.NodeId.GetValueOrDefault()
                };
                MessagesReadNodeNotice notice = new MessagesReadNodeNotice(
                    messagesId, ConversationType.Dialog, receiverUser.Id.GetValueOrDefault(), senderUser.Id.GetValueOrDefault());
                await SendNoticeToNodesAsync(notice, nodesId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendUserNodeChangedNodeNoticeAsync(long userId, long newNodeId)
        {
            try
            {
                UserNodeChangedNodeNotice notice = new UserNodeChangedNodeNotice(userId, newNodeId);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendNewChatMessageNodeNoticeAsync(MessageVm newMessage)
        {
            try
            {
                IEnumerable<long> nodesIds = await _loadChatsService.GetChatNodeListAsync(newMessage.ConversationId.GetValueOrDefault()).ConfigureAwait(false);
                newMessage.NodesId = nodesIds;
                if (newMessage.Attachments != null && newMessage.Attachments.Any())
                {
                    var forwardedMessagesAttachment = newMessage.Attachments.FirstOrDefault(message => message.Type == AttachmentType.ForwardedMessages);
                    if (forwardedMessagesAttachment != null)
                    {
                        await _updateMessagesService.UpdateMessagesNodesIdsAsync((List<MessageVm>)forwardedMessagesAttachment.Payload, nodesIds).ConfigureAwait(false);
                    }
                }
                NewMessagesNodeNotice notice = new NewMessagesNodeNotice(newMessage);
                await SendNoticeToNodesAsync(notice, nodesIds).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendNewDialogMessageNodeNoticeAsync(MessageVm newMessage)
        {
            try
            {
                IEnumerable<long> nodesId = await _loadDialogsService.GetDlalogNodesAsync(newMessage.ConversationId.GetValueOrDefault()).ConfigureAwait(false);
                if (newMessage.Attachments != null && newMessage.Attachments.Any())
                {
                    var forwardedMessagesAttachment = newMessage.Attachments.FirstOrDefault(message => message.Type == AttachmentType.ForwardedMessages);
                    if (forwardedMessagesAttachment != null)
                    {
                        await _updateMessagesService.UpdateMessagesNodesIdsAsync((List<MessageVm>)forwardedMessagesAttachment.Payload, nodesId).ConfigureAwait(false);
                    }
                }
                NewMessagesNodeNotice notice = new NewMessagesNodeNotice(newMessage);
                await SendNoticeToNodesAsync(notice, nodesId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendNewChatsNodeNoticeAsync(List<ChatVm> chats)
        {
            try
            {
                CreateOrEditChatsNodeNotice notice = new CreateOrEditChatsNodeNotice(Enums.NodeNoticeCode.NewChats, chats);
                List<long> nodesIds = new List<long>();
                foreach (var chat in chats)
                {
                    nodesIds.AddRange(chat.NodesId);
                }
                if (nodesIds.Any())
                {
                    nodesIds.Distinct();
                    await SendNoticeToNodesAsync(notice, nodesIds).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendEditChatsNodeNoticeAsync(List<ChatVm> chats)
        {
            try
            {
                CreateOrEditChatsNodeNotice notice = new CreateOrEditChatsNodeNotice(Enums.NodeNoticeCode.EditChats, chats);
                List<long> nodesIds = new List<long>();
                foreach (var chat in chats)
                {
                    nodesIds.AddRange(chat.NodesId);
                }
                if (nodesIds.Any())
                {
                    nodesIds.Distinct();
                    await SendNoticeToNodesAsync(notice, nodesIds).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendEditUsersNodeNoticeAsync(List<ShortUser> users, List<BlockSegmentVm> blockSegments)
        {
            try
            {
                CreateOrEditUsersNodeNotice notice = new CreateOrEditUsersNodeNotice(Enums.NodeNoticeCode.EditUsers, users, blockSegments);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendEditUsersNodeNoticeAsync(ShortUser user, BlockSegmentVm blockSegment)
        {
            try
            {
                CreateOrEditUsersNodeNotice notice = new CreateOrEditUsersNodeNotice(Enums.NodeNoticeCode.EditUsers, user, blockSegment);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendDeleteConversationsNodeNoticeAsync(long conversationId, ConversationType conversationType, long requestorId)
        {
            try
            {
                DeleteConversationsNodeNotice notice = new DeleteConversationsNodeNotice(conversationId, conversationType, NodeSettings.Configs.Node.Id, requestorId);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendDeleteFilesNodeNoticeAsync(List<long> filesId)
        {
            try
            {
                DeleteFilesNodeNotice notice = new DeleteFilesNodeNotice(filesId, NodeSettings.Configs.Node.Id);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendDeleteUsersNodeNoticeAsync(List<long> usersId)
        {
            try
            {
                DeleteUsersNodeNotice notice = new DeleteUsersNodeNotice(usersId, NodeSettings.Configs.Node.Id);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendDeleteUsersNodeNoticeAsync(long userId)
        {
            try
            {
                DeleteUsersNodeNotice notice = new DeleteUsersNodeNotice(userId, NodeSettings.Configs.Node.Id);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendChannelNodeNoticeAsync(ChannelVm channel, long requestorId, IEnumerable<ChannelUserVm> subscribers)
        {
            try
            {
                ChannelNodeNotice notice = new ChannelNodeNotice(channel, requestorId, subscribers);
                await SendNoticeToNodesAsync(notice, channel.NodesId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendChatMessagesReadNodeNoticeAsync(List<MessageDto> messages, long chatId, long requestorId)
        {
            try
            {
                IEnumerable<long> nodesIds = await _loadChatsService.GetChatNodeListAsync(chatId).ConfigureAwait(false);
                MessagesReadNodeNotice notice = new MessagesReadNodeNotice(messages.Select(message => message.GlobalId).ToList(), ConversationType.Chat, chatId, requestorId);
                await SendNoticeToNodesAsync(notice, nodesIds).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendNewBlockNodeNoticeAsync(BlockVm block)
        {
            try
            {
                BlocksNodeNotice notice = new BlocksNodeNotice(Enums.NodeNoticeCode.NewBlocks, block);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendMessagesDeletedNodeNoticeAsync(long conversationId, ConversationType conversationType, List<MessageVm> messages, long requestorId)
        {
            try
            {
                MessagesDeletedNodeNotice notice = new MessagesDeletedNodeNotice(conversationId, conversationType, messages, requestorId);
                List<long> nodesIds = messages.FirstOrDefault()?.NodesId?.ToList();
                await SendNoticeToNodesAsync(notice, nodesIds).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendNewChannelMessageNodeNoticeAsync(MessageVm message)
        {
            try
            {
                NewMessagesNodeNotice notice = new NewMessagesNodeNotice(message);
                IEnumerable<long> nodesIds = await _loadChannelsService.GetChannelNodesIdAsync(message.ConversationId.GetValueOrDefault()).ConfigureAwait(false);
                if (message.Attachments != null && message.Attachments.Any())
                {
                    var forwardedMessagesAttachment = message.Attachments.FirstOrDefault(opt => opt.Type == AttachmentType.ForwardedMessages);
                    if (forwardedMessagesAttachment != null)
                    {
                        await _updateMessagesService.UpdateMessagesNodesIdsAsync((List<MessageVm>)forwardedMessagesAttachment.Payload, nodesIds).ConfigureAwait(false);
                    }
                }
                await SendNoticeToNodesAsync(notice, nodesIds).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendUsersAddedToUserBlacklistNodeNoticeAsync(List<long> usersId, long userId)
        {
            try
            {
                UsersAddedToUserBlacklistNodeNotice notice = new UsersAddedToUserBlacklistNodeNotice(userId, usersId);
                await SendNoticeToNodesAsync(notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private async Task SendNoticeToNodeAsync(NodeConnection nodeConnection, NodeNotice notice)
        {
            await SendNoticeToNodesAsync(new List<NodeConnection> { nodeConnection }, notice).ConfigureAwait(false);
        }
        private async Task SendNoticeToNodesAsync(NodeNotice notice, IEnumerable<long> nodesIds = null)
        {
            List<NodeConnection> nodesConnections;
            if (nodesIds == null || !nodesIds.Any())
            {
                var nodes = await _nodesService.GetAllNodesInfoAsync().ConfigureAwait(false);
                nodesIds = nodes.Select(opt => opt.Id);
            }
            if (nodesIds != null && nodesIds.Any())
            {
                nodesConnections = _connectionsService.GetNodeConnections(nodesIds);
                IEnumerable<long> notConnectedNodesIds;
                if (nodesConnections != null && nodesConnections.Any())
                {
                    notConnectedNodesIds = nodesIds.Where(id => !nodesConnections.Any(node => node?.Node?.Id == id));
                }
                else
                {
                    notConnectedNodesIds = nodesIds;
                }

                if (notConnectedNodesIds != null && notConnectedNodesIds.Any())
                {
                    foreach (var nodeId in notConnectedNodesIds)
                    {
                        await _pendingMessagesService.AddNodePendingMessageAsync(nodeId, notice, TimeSpan.FromDays(10)).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                nodesConnections = _connectionsService.GetNodeConnections();
            }
            await SendNoticeToNodesAsync(nodesConnections, notice).ConfigureAwait(false);
        }
        public async Task SendPendingMessagesAsync(long nodeId)
        {
            try
            {
                var nodeConnection = _connectionsService.GetNodeConnection(nodeId);
                if (nodeConnection != null)
                {
                    var pendingMessages = await _pendingMessagesService.GetNodePendingMessagesAsync(nodeId).ConfigureAwait(false);
                    if (pendingMessages != null && pendingMessages.Any())
                    {
                        foreach (var message in pendingMessages)
                        {
                            await SendNoticeToNodeAsync(
                                nodeConnection,
                                ObjectSerializer.ByteArrayToObject<NodeNotice>(Convert.FromBase64String(message.Content))).ConfigureAwait(false);
                        }
                        await _pendingMessagesService.RemovePendingMessagesAsync(pendingMessages.Select(message => message.Id)).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        private async Task SendNoticeToNodesAsync(IEnumerable<NodeConnection> nodes, NodeNotice notice)
        {
            try
            {
                foreach (var node in nodes)
                {
                    if (node.NodeWebSocket.State != WebSocketState.Open && node.Node != null)
                    {
                        await _pendingMessagesService.AddNodePendingMessageAsync(node.Node.Id, notice, TimeSpan.FromDays(10)).ConfigureAwait(false);
                        continue;
                    }
                    try
                    {
                        byte[] noticeData;
                        if (node.IsEncryptedConnection)
                        {
                            noticeData = Encryptor.SymmetricDataEncrypt(
                                ObjectSerializer.ObjectToByteArray(notice),
                                NodeData.Instance.NodeKeys.SignPrivateKey,
                                node.SymmetricKey,
                                MessageDataType.Notice,
                                NodeData.Instance.NodeKeys.Password);
                        }
                        else
                        {
                            noticeData = ObjectSerializer.ObjectToByteArray(notice);
                        }
                        await node.NodeWebSocket.SendAsync(
                            noticeData,
                            WebSocketMessageType.Binary,
                            true,
                            CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (WebSocketException)
                    {
                        Logger.WriteLog("Connection was closed.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex, notice.ToString());
            }
        }
        public async void SendPollingNodeNoticeAsync(Guid pollId, long conversationId, ConversationType conversationType, List<PollVoteVm> signedOptions, long votedUserId, List<long> nodesId)
        {
            try
            {
                PollingNodeNotice notice = new PollingNodeNotice(pollId, conversationId, conversationType, signedOptions, votedUserId);
                await SendNoticeToNodesAsync(notice, nodesId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendPollingNodeNoticeAsync(Guid pollId, long conversationId, ConversationType conversationType, List<byte> optionsId, long votedUserId, List<long> nodesId)
        {
            try
            {
                PollingNodeNotice notice = new PollingNodeNotice(pollId, conversationId, conversationType, optionsId, votedUserId);
                await SendNoticeToNodesAsync(notice, nodesId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendProxyUsersNotificationsNodeNoticeAsync(byte[] communicationData, long userId, byte[] publicKey, NodeConnection nodeConnection)
        {
            try
            {
                ProxyUsersNotificationsNodeNotice notice = new ProxyUsersNotificationsNodeNotice(userId, publicKey, communicationData);
                await SendNoticeToNodeAsync(nodeConnection, notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
    }
}