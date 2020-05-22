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
using NodeApp.Extensions;
using NodeApp.Helpers;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Enums;
using ObjectsLibrary.NoticeClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp.NotificationServices
{
    public class ConversationsNoticeService : IConversationsNoticeService
    {
        private readonly IConnectionsService connectionsService;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IPushNotificationsService pushNotificationsService;
        private readonly ILoadChatsService loadChatsService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly ILoadDialogsService loadDialogsService;
        private readonly IPendingMessagesService pendingMessagesService;
        private readonly ILoadUsersService loadUsersService;
        private readonly IPrivacyService privacyService;
        public ConversationsNoticeService(IAppServiceProvider appServiceProvider)
        {
            this.connectionsService = appServiceProvider.ConnectionsService;
            this.nodeNoticeService = appServiceProvider.NodeNoticeService;
            this.pushNotificationsService = appServiceProvider.PushNotificationsService;
            this.loadChatsService = appServiceProvider.LoadChatsService;
            this.loadChannelsService = appServiceProvider.LoadChannelsService;
            this.loadDialogsService = appServiceProvider.LoadDialogsService;
            this.pendingMessagesService = appServiceProvider.PendingMessagesService;
            this.loadUsersService = appServiceProvider.LoadUsersService;
            this.privacyService = appServiceProvider.PrivacyService;
        }
        public async void SendConversationActionNoticeAsync(long userId, ConversationType conversationType, long conversationId, ConversationAction action)
        {
            try
            {
                List<long> usersIds = null;
                ConversationActionNotice notice = null;
                switch (conversationType)
                {
                    case ConversationType.Dialog:
                        {
                            var users = await loadDialogsService.GetDialogUsersAsync(conversationId).ConfigureAwait(false);
                            var secondUser = users.FirstOrDefault(opt => opt.Id != userId);
                            usersIds = new List<long> { secondUser.Id.Value };
                            var dialogId = await loadDialogsService.GetMirrorDialogIdAsync(conversationId);
                            notice = new ConversationActionNotice(dialogId, ConversationType.Dialog, userId, action);
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            usersIds = await loadChatsService.GetChatUsersIdAsync(conversationId);
                            notice = new ConversationActionNotice(conversationId, ConversationType.Chat, userId, action);
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            usersIds = await loadChannelsService.GetChannelUsersIdAsync(conversationId);
                            notice = new ConversationActionNotice(conversationId, ConversationType.Channel, null, action);
                        }
                        break;
                    default:
                        return;
                }
                usersIds.Remove(userId);
                var clientConnections = connectionsService.GetClientConnections(usersIds);
                await SendNoticeToClientsAsync(clientConnections, notice);
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
            }

        }
        public async void SendNewMessageNoticeToChatUsersAsync(MessageDto newMessage, ClientConnection connection, bool sendPush = true)
        {
            try
            {                
                List<ChatUserVm> chatUsers = await loadChatsService.GetChatUsersAsync(newMessage.ConversationId, null).ConfigureAwait(false);
                if (sendPush)
                {
                    pushNotificationsService.SendMessageNotificationAsync(
                        newMessage,
                        chatUsers.Select(opt => new NotificationUser(opt.UserId, opt.IsMuted.Value)).ToList());
                }
                List<Task> noticeTasks = new List<Task>();
                foreach (var chatUser in chatUsers)
                {
                    var clientConnections = connectionsService.GetUserClientConnections(chatUser.UserId);
                    if (clientConnections != null)
                    {
                        noticeTasks.Add(Task.Run(async () =>
                        {
                            NewMessageNotice notice = new NewMessageNotice(
                                MessageConverter.GetMessageVm(newMessage, chatUser.UserId),
                                MarkdownHelper.ContainsMarkdownUserCalling(newMessage.Text, chatUser.UserId));
                            await SendNoticeToClientsAsync(clientConnections.Where(clientConnection => clientConnection != connection), notice).ConfigureAwait(false);
                        }));
                    }
                }
                await Task.WhenAll(noticeTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendChannelNoticeAsync(ChannelVm channel, List<long> usersId, ClientConnection clientConnection = null)
        {
            try
            {
                List<ClientConnection> clientsConnections = new List<ClientConnection>();
                foreach (long userId in usersId)
                {
                    var clientConnections = connectionsService.GetUserClientConnections(userId);
                    if (clientConnections != null)
                    {
                        clientsConnections.AddRange(clientConnections.Where(opt => opt != clientConnection));
                    }
                }
                ChannelVm channelClone = (ChannelVm)channel.Clone();
                channelClone.ChannelUsers = null;
                channelClone.UserRole = null;
                ChannelNotice notice = new ChannelNotice(channelClone);
                await SendNoticeToClientsAsync(clientsConnections, notice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendNewChannelNoticesAsync(IEnumerable<ChannelUserVm> channelUsers, long channelId, ClientConnection clientConnection)
        {
            try
            {
                ChannelVm channel = await loadChannelsService.GetChannelByIdAsync(channelId).ConfigureAwait(false);
                ChannelNotice notice = new ChannelNotice(channel);
                foreach (var channelUser in channelUsers)
                {
                    var clientConnections = connectionsService.GetUserClientConnections(channelUser.UserId);
                    if (clientConnections != null)
                    {
                        await SendNoticeToClientsAsync(clientConnections.Where(opt => opt != clientConnection), notice).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendNewMessageNoticeToDialogUsers(IEnumerable<MessageDto> messages, ClientConnection senderClientConnection, long receiverId, bool saveMessageFlag = true)
        {
            try
            {
                long senderId = messages.FirstOrDefault().SenderId.GetValueOrDefault();
                var dialogs = await loadDialogsService.GetUsersDialogsAsync(senderId, receiverId);                
                var currentDialog = dialogs.FirstOrDefault(dial => dial.FirstUserId == receiverId);
                Notice senderNotice = default;
                if (senderId != receiverId)
                {
                    var message = messages.FirstOrDefault(mess => mess.ConversationId != currentDialog.Id);
                    senderNotice = new NewMessageNotice(
                        MessageConverter.GetMessageVm(message, senderId),
                        MarkdownHelper.ContainsMarkdownUserCalling(message.Text, receiverId));
                }
                else
                {
                    var message = messages.FirstOrDefault();
                    senderNotice = new NewMessageNotice(
                        MessageConverter.GetMessageVm(message, senderId),
                        MarkdownHelper.ContainsMarkdownUserCalling(message.Text, receiverId));
                }
                var senderClients = connectionsService.GetUserClientConnections(messages.ElementAt(0).SenderId.GetValueOrDefault());
                if (senderClients != null)
                {
                    IEnumerable<ClientConnection> senderConnectionsExceptCurrent = senderClients.Where(connection => !Equals(senderClientConnection, connection));
                    await SendNoticeToClientsAsync(senderConnectionsExceptCurrent, senderNotice).ConfigureAwait(false);
                }
                if (messages.Count() == 2)
                {
                    var message = messages.FirstOrDefault(mess => mess.ConversationId == currentDialog.Id);
                    var receiverClients = connectionsService.GetUserClientConnections(receiverId);
                    Notice receiverNotice = new NewMessageNotice(
                        MessageConverter.GetMessageVm(message, receiverId),
                        MarkdownHelper.ContainsMarkdownUserCalling(message.Text, receiverId));
                    if (receiverClients != null && receiverClients.Any())
                    {
                        await SendNoticeToClientsAsync(receiverClients, receiverNotice).ConfigureAwait(false);
                    }
                    else
                    {
                        var receiver = await loadUsersService.GetUserAsync(receiverId).ConfigureAwait(false);
                        if (receiver.NodeId == NodeSettings.Configs.Node.Id && !saveMessageFlag)
                        {
                            await pendingMessagesService.AddUserPendingMessageAsync(receiverId, receiverNotice, message.GlobalId).ConfigureAwait(false);
                        }
                    }
                }
                DialogDto dialog = await loadDialogsService.GetDialogAsync(currentDialog.Id).ConfigureAwait(false);
                pushNotificationsService.SendMessageNotificationAsync(
                    messages.FirstOrDefault(opt => opt.ConversationId == currentDialog.Id),
                    new List<NotificationUser> { new NotificationUser(dialog.FirstUserId, dialog.IsMuted) });
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendNewUsersAddedToChatNoticeAsync(ChatVm chat, ClientConnection clientConnection)
        {
            try
            {
                List<ChatUserVm> filteredUsers = new List<ChatUserVm>(privacyService.ApplyPrivacySettings(chat.Users));
                List<long> newUsersId = new List<long>(chat.Users.Select(chatUser => chatUser.UserId));
                List<long> allUsersId = new List<long>((await loadChatsService.GetChatUsersIdAsync(chat.Id.Value).ConfigureAwait(false)).Except(newUsersId));
                foreach (long chatUserId in allUsersId)
                {
                    var clientConnections = connectionsService.GetUserClientConnections(chatUserId);
                    if (clientConnections != null)
                    {
                        if (clientConnection != null)
                        {
                            clientConnections = clientConnections.Where(opt => opt != clientConnection).ToList();
                        }
                        UsersAddedNotice notice = new UsersAddedNotice(filteredUsers, chat);
                        await SendNoticeToClientsAsync(clientConnections, notice).ConfigureAwait(false);
                    }
                }
                foreach (long chatUserId in newUsersId)
                {
                    var clientConnections = connectionsService.GetUserClientConnections(chatUserId);
                    if (clientConnections != null)
                    {
                        NewChatNotice notice = new NewChatNotice(chat);
                        await SendNoticeToClientsAsync(clientConnections, notice).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendMessagesReadedNoticeAsync(IEnumerable<MessageDto> readedMessages, long conversationId, ConversationType conversationType, long userId, ClientConnection clientConnection = null)
        {
            try
            {
                if (readedMessages.IsNullOrEmpty())
                {
                    return;
                }

                switch (conversationType)
                {
                    case ConversationType.Dialog:
                        {
                            await SendDialogMessagesReadedNoticeAsync(readedMessages.Select(opt => opt.GlobalId), conversationId, userId).ConfigureAwait(false);
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            await SendChatMessagesReadedNoticeAsync(readedMessages, conversationId, userId).ConfigureAwait(false);
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            await SendChannelMessagesReadedNoticeAsync(readedMessages.Select(opt => opt.GlobalId), conversationId, userId, clientConnection).ConfigureAwait(false);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendMessagesUpdatedNoticeAsync(
            long conversationId, ConversationType conversationType, IEnumerable<MessageDto> messages, long userId, bool deleted, ClientConnection clientConnection)
        {
            try
            {
                switch (conversationType)
                {
                    case ConversationType.Dialog:
                        await SendDialogMessagesUpdatedNoticeAsync(messages, clientConnection, userId, conversationId, deleted).ConfigureAwait(false);
                        break;
                    case ConversationType.Chat:
                        await SendChatMessagesUpdatedNoticeAsync(messages, conversationId, clientConnection, deleted).ConfigureAwait(false);
                        break;
                    case ConversationType.Channel:
                        await SendChannelMessagesUpdatedNoticeAsync(messages, conversationId, clientConnection, deleted).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private async Task SendChannelMessagesUpdatedNoticeAsync(IEnumerable<MessageDto> messages, long conversationId, ClientConnection clientConnection, bool deleted)
        {

            MessageInfo messageInfo = new MessageInfo(conversationId, ConversationType.Channel, messages.Select(opt => opt.GlobalId));
            MessagesUpdatedNotice notice = deleted
                ? new MessagesUpdatedNotice(messageInfo, null)
                : new MessagesUpdatedNotice(null, MessageConverter.GetMessagesVm(messages, clientConnection?.UserId));
            List<ClientConnection> clientsConnections = new List<ClientConnection>();
            IEnumerable<long> usersId = await loadChannelsService.GetChannelUsersIdAsync(conversationId).ConfigureAwait(false);
            foreach (long userId in usersId)
            {
                var clientConnections = connectionsService.GetUserClientConnections(userId);
                if (clientConnections != null)
                {
                    clientsConnections.AddRange(clientConnections);
                }
            }
            clientsConnections.Remove(clientConnection);
            await SendNoticeToClientsAsync(clientsConnections, notice).ConfigureAwait(false);
        }

        private async Task SendChannelMessagesReadedNoticeAsync(IEnumerable<Guid> readedMessagesId, long channelId, long userId, ClientConnection clientConnection)
        {
            MessagesReadedNotice notice = new MessagesReadedNotice(readedMessagesId, ConversationType.Channel, channelId);
            var clientConnections = connectionsService.GetUserClientConnections(userId);
            if (clientConnections != null)
            {
                await SendNoticeToClientsAsync(clientConnections.Where(opt => opt != clientConnection), notice).ConfigureAwait(false);
            }
        }

        public async void SendEditChatNoticeAsync(ChatVm editedChat, ClientConnection clientConnection)
        {
            try
            {
                IEnumerable<long> chatUsersId = await loadChatsService.GetChatUsersIdAsync(editedChat.Id.Value).ConfigureAwait(false);
                EditChatNotice notice = new EditChatNotice(editedChat);
                foreach (long userId in chatUsersId)
                {
                    var clientConnections = connectionsService.GetUserClientConnections(userId);
                    if (clientConnections != null)
                    {
                        await SendNoticeToClientsAsync(clientConnections.Where(opt => opt != clientConnection), notice).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private async Task SendChatMessagesReadedNoticeAsync(IEnumerable<MessageDto> messages, long chatId, long requestorId)
        {
            List<ChatUserDto> chatUsers = await loadChatsService.GetChatUsersAsync(messages.Select(message => message.SenderId.GetValueOrDefault()).Append(requestorId), chatId).ConfigureAwait(false);
            ChatUserDto requestorChatUser = chatUsers.FirstOrDefault(chatUser => chatUser.UserId == requestorId);
            foreach (var groupBySender in messages.GroupBy(message => message.SenderId))
            {
                var clients = connectionsService.GetUserClientConnections(groupBySender.Key.GetValueOrDefault());
                if (clients != null && requestorId != groupBySender.Key)
                {
                    if (groupBySender.Any())
                    {
                        MessagesReadedNotice notice = new MessagesReadedNotice(
                            groupBySender.Select(opt => opt.GlobalId),
                            ConversationType.Chat,
                            chatId);
                        await SendNoticeToClientsAsync(clients, notice).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task SendDialogMessagesReadedNoticeAsync(IEnumerable<Guid> messagesId, long dialogId, long senderId)
        {
            var clients = connectionsService.GetUserClientConnections(senderId);
            if (clients != null)
            {
                MessagesReadedNotice notice = new MessagesReadedNotice(messagesId, ConversationType.Dialog, dialogId);
                await SendNoticeToClientsAsync(clients, notice).ConfigureAwait(false);
            }
        }

        public async void SendNewChatNoticeAsync(ChatVm newChat, ClientConnection requestorConnection)
        {
            try
            {
                if (newChat.Users == null)
                {
                    return;
                }

                foreach (var chatUser in newChat.Users)
                {
                    var clients = connectionsService.GetUserClientConnections(chatUser.UserId);
                    if (clients != null)
                    {
                        IEnumerable<ClientConnection> clientConnectionsWithoutCurrent = clients.Where(connection => requestorConnection != connection);
                        NewChatNotice newChatNotice = new NewChatNotice(newChat);
                        await SendNoticeToClientsAsync(clientConnectionsWithoutCurrent, newChatNotice).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendChangeChatUsersNoticeAsync(IEnumerable<ChatUserVm> changedChatUsers, long chatId, ClientConnection clientConnection = null)
        {
            try
            {
                IEnumerable<long> chatUsersId = await loadChatsService.GetChatUsersIdAsync(chatId).ConfigureAwait(false);
                IEnumerable<long> deletedOrBannedChatUsersId = changedChatUsers.Where(opt => opt.Deleted == true || opt.Banned == true).Select(opt => opt.UserId);
                chatUsersId = chatUsersId.Concat(deletedOrBannedChatUsersId);
                foreach (long userId in chatUsersId)
                {
                    var clients = connectionsService.GetUserClientConnections(userId);
                    if (clients != null)
                    {
                        clients = clients.Where(opt => opt != clientConnection).ToList();
                        ChatUsersChangedNotice notice = new ChatUsersChangedNotice(changedChatUsers);
                        await SendNoticeToClientsAsync(clients, notice).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private async Task SendDialogMessagesUpdatedNoticeAsync(IEnumerable<MessageDto> messages, ClientConnection clientConnection, long requestorId, long requestorDialogId, bool deleted)
        {
            IEnumerable<UserVm> users = await loadDialogsService.GetDialogUsersAsync(requestorDialogId).ConfigureAwait(false);
            UserVm receiver = users.FirstOrDefault(opt => opt.Id != requestorId);
            IEnumerable<MessageDto> receiverMessages = messages.Where(opt => opt.ConversationId != requestorDialogId);
            long? receiverDialogId = receiverMessages?.FirstOrDefault()?.ConversationId;
            var requestorMessages = receiverMessages != null ? messages.Except(receiverMessages) : messages;
            MessageInfo requestorMessageInfo = new MessageInfo(
                    requestorDialogId,
                    ConversationType.Dialog,
                    requestorMessages.Select(opt => opt.GlobalId));
            MessageInfo receiverMessageInfo = new MessageInfo(
                    receiverDialogId.GetValueOrDefault(),
                    ConversationType.Dialog,
                    receiverMessages?.Select(opt => opt.GlobalId));
            MessagesUpdatedNotice requestorMessagesUpdatedNotice;
            MessagesUpdatedNotice receiverMessagesUpdatedNotice;
            if (deleted)
            {
                requestorMessagesUpdatedNotice = new MessagesUpdatedNotice(requestorMessageInfo, null);
                receiverMessagesUpdatedNotice = new MessagesUpdatedNotice(receiverMessageInfo, null);
            }
            else
            {
                requestorMessagesUpdatedNotice = new MessagesUpdatedNotice(null,
                    MessageConverter.GetMessagesVm(requestorMessages, clientConnection?.UserId));
                receiverMessagesUpdatedNotice = new MessagesUpdatedNotice(null,
                    MessageConverter.GetMessagesVm(receiverMessages, receiver.Id));
            }
            var requestorClients = connectionsService.GetUserClientConnections(requestorId);
            if (requestorClients != null)
            {
                await SendNoticeToClientsAsync(requestorClients.Where(opt => opt != clientConnection), requestorMessagesUpdatedNotice).ConfigureAwait(false);
            }
            if (receiver != null)
            {
                var receiverClients = connectionsService.GetUserClientConnections(receiver.Id.Value);
                if (receiverClients != null)
                {
                    await SendNoticeToClientsAsync(receiverClients, receiverMessagesUpdatedNotice).ConfigureAwait(false);
                }
            }
        }

        public async void SendNewMessageNoticeToChannelUsersAsync(MessageDto newMessage, ClientConnection clientConnection = null, bool sendPush = true)
        {
            try
            {
                var channelUsers = await loadChannelsService.GetChannelUsersAsync(newMessage.ConversationId, null, null).ConfigureAwait(false);
                if (sendPush)
                {
                    pushNotificationsService.SendMessageNotificationAsync(
                        newMessage,
                        channelUsers.Select(opt => new NotificationUser(opt.UserId, opt.IsMuted.Value)).ToList());
                }
                var administrationUsers = await loadChannelsService.GetAdministrationChannelUsersAsync(newMessage.ConversationId).ConfigureAwait(false);
                foreach (var channelUser in channelUsers)
                {
                    var clientConnections = connectionsService.GetUserClientConnections(channelUser.UserId);
                    if (clientConnections != null)
                    {
                        clientConnections = clientConnections.Where(opt => opt != clientConnection).ToList();
                        NewMessageNotice notice;
                        if (channelUser.UserId == newMessage.SenderId || administrationUsers.Any(opt => opt.UserId == channelUser.UserId))
                        {
                            notice = new NewMessageNotice(
                                MessageConverter.GetMessageVm(newMessage, channelUser.UserId),
                                MarkdownHelper.ContainsMarkdownUserCalling(newMessage.Text, channelUser.UserId));
                        }
                        else
                        {
                            MessageVm tempMessage = new MessageVm(MessageConverter.GetMessageVm(newMessage, channelUser.UserId))
                            {
                                SenderId = null
                            };
                            notice = new NewMessageNotice(
                                tempMessage,
                                MarkdownHelper.ContainsMarkdownUserCalling(tempMessage.Text, channelUser.UserId));
                        }
                        await SendNoticeToClientsAsync(clientConnections, notice).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async void SendSystemMessageNoticeAsync(MessageDto message)
        {            
            
            switch (message.ConversationType)
            {                
                case ConversationType.Dialog:
                    {                        
                        var mirrorDialog = await loadDialogsService.GetDialogAsync(message.ConversationId).ConfigureAwait(false);
                        var secondMessage = (MessageDto) message.Clone();
                        secondMessage.ConversationId = mirrorDialog.Id;                        
                        var firstUserConnections = connectionsService.GetUserClientConnections(mirrorDialog.FirstUserId);
                        var secondUserConnections = connectionsService.GetUserClientConnections(mirrorDialog.SecondUserId);
                        var firstUserNotice = new NewMessageNotice(MessageConverter.GetMessageVm(secondMessage, null), false);
                        var secondUserNotice = new NewMessageNotice(MessageConverter.GetMessageVm(message, null), false);
                        await SendNoticeToClientsAsync(firstUserConnections, firstUserNotice).ConfigureAwait(false);
                        await SendNoticeToClientsAsync(secondUserConnections, secondUserNotice).ConfigureAwait(false);
                    }
                    break;
                case ConversationType.Chat:
                    {
                        var usersIds = await loadChatsService.GetChatUsersIdAsync(message.ConversationId).ConfigureAwait(false);
                        var notice = new NewMessageNotice(MessageConverter.GetMessageVm(message, null), false);
                        var clientsConnections = connectionsService.GetClientConnections(usersIds);
                        await SendNoticeToClientsAsync(clientsConnections, notice).ConfigureAwait(false);
                    }
                    break;
                case ConversationType.Channel:
                    {
                        var usersIds = await loadChannelsService.GetChannelUsersIdAsync(message.ConversationId).ConfigureAwait(false);
                        var notice = new NewMessageNotice(MessageConverter.GetMessageVm(message, null), false);
                        var clientsConnections = connectionsService.GetClientConnections(usersIds);
                        await SendNoticeToClientsAsync(clientsConnections, notice).ConfigureAwait(false);
                    }
                    break;
            }
        }

        private async Task SendChatMessagesUpdatedNoticeAsync(IEnumerable<MessageDto> messages, long chatId, ClientConnection clientConnection, bool deleted)
        {
            IEnumerable<long> chatUsersId = await loadChatsService.GetChatUsersIdAsync(chatId).ConfigureAwait(false);
            MessageInfo messageInfo = new MessageInfo(chatId, ConversationType.Chat, messages.Select(opt => opt.GlobalId));
            MessagesUpdatedNotice notice;
            if (deleted)
            {
                notice = new MessagesUpdatedNotice(messageInfo, null);
            }
            else
            {
                notice = new MessagesUpdatedNotice(null, MessageConverter.GetMessagesVm(messages, clientConnection?.UserId));
            }
            foreach (long userId in chatUsersId)
            {
                var clientConnections = connectionsService.GetUserClientConnections(userId);
                if (clientConnections != null)
                {
                    await SendNoticeToClientsAsync(clientConnections.Where(opt => opt != clientConnection), notice).ConfigureAwait(false);
                }
            }
        }
        private async Task SendNoticeToClientsAsync(IEnumerable<ClientConnection> clients, Notice notice)
        {
            try
            {
                if (clients.IsNullOrEmpty())
                {
                    return;
                }
                foreach (var client in clients)
                {
                    try
                    {
                        if (client.IsProxiedClientConnection && client.ClientSocket == null && client.ProxyNodeWebSocket != null)
                        {
                            NodeConnection nodeConnection = connectionsService.GetNodeConnections().FirstOrDefault(opt => opt.NodeWebSocket == client.ProxyNodeWebSocket);
                            byte[] noticeData = ObjectSerializer.CommunicationObjectToBytes(notice);
                            if (client.IsEncryptedConnection)
                            {
                                noticeData = Encryptor.SymmetricDataEncrypt(
                                    noticeData,
                                    NodeData.Instance.NodeKeys.SignPrivateKey,
                                    client.SymmetricKey,
                                    MessageDataType.Notice,
                                    NodeData.Instance.NodeKeys.Password);
                            }
                            nodeNoticeService.SendProxyUsersNotificationsNodeNoticeAsync(
                                noticeData,
                                client.UserId.GetValueOrDefault(),
                                client.PublicKey,
                                nodeConnection);
                        }
                        else if (client.ClientSocket != null)
                        {
                            byte[] noticeData = ObjectSerializer.NoticeToBytes(notice);
                            if (client.IsEncryptedConnection)
                            {
                                noticeData = Encryptor.SymmetricDataEncrypt(
                                    noticeData,
                                    NodeData.Instance.NodeKeys.SignPrivateKey,
                                    client.SymmetricKey,
                                    MessageDataType.Binary,
                                    NodeData.Instance.NodeKeys.Password);
                            }
                            await client.ClientSocket.SendAsync(
                                noticeData,
                                WebSocketMessageType.Binary,
                                true,
                                CancellationToken.None)
                                .ConfigureAwait(false);
                        }
                    }
                    catch (WebSocketException)
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex, notice.ToString());
            }
        }
    }
}