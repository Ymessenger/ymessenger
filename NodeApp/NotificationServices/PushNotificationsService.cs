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
using NodeApp.Helpers;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Enums;
using ObjectsLibrary.NoticeClasses;
using ObjectsLibrary.NotificationsServerClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NodeApp.NotificationServices
{
    public class PushNotificationsService : IPushNotificationsService
    {
        private readonly IConnectionsService connectionsService;
        private readonly ITokensService tokensService;
        private readonly ILoadChannelsService loadChannelsService;
        public PushNotificationsService(IAppServiceProvider appServiceProvider)
        {
            this.connectionsService = appServiceProvider.ConnectionsService;
            this.tokensService = appServiceProvider.TokensService;
            this.loadChannelsService = appServiceProvider.LoadChannelsService;
        }
        public async void SendMessageNotificationAsync(MessageDto message, List<NotificationUser> notificationUsers)
        {
            try
            {
                switch (message.ConversationType)
                {
                    case ConversationType.Dialog:
                        await SendDialogMessageNotificationAsync(notificationUsers.FirstOrDefault(), message).ConfigureAwait(false);
                        break;
                    case ConversationType.Chat:
                        await SendChatMessageNotificationAsync(message, notificationUsers).ConfigureAwait(false);
                        break;
                    case ConversationType.Channel:
                        await SendChannelMessageNotificationAsyhc(message, notificationUsers).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendNewSessionNoticeAsync(ClientConnection clientConnection)
        {
            try
            {
                if (clientConnection.CurrentToken == null)
                {
                    throw new ArgumentNullException(nameof(clientConnection));
                }

                List<TokenVm> tokens = await tokensService.GetAllUsersTokensAsync(new List<long> { clientConnection.UserId.GetValueOrDefault() }).ConfigureAwait(false);
                var token = clientConnection.CurrentToken;
                var userTokens = GetOfflineUserTokens(tokens, clientConnection.UserId.GetValueOrDefault());
                userTokens = userTokens.Where(opt => opt.AccessToken != token.AccessToken)?.ToList();
                if (userTokens != null && userTokens.Any())
                {
                    NewSessionNotice notice = new NewSessionNotice(new SessionVm
                    {
                        AppName = token.AppName,
                        DeviceName = token.DeviceName,
                        OSName = token.OSName,
                        IP = clientConnection.ClientIP.ToString(),
                        TokenId = token.Id.GetValueOrDefault()
                    });
                    await SendNotificationRequestAsync(userTokens.Select(opt => opt.DeviceTokenId), notice).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private async Task SendDialogMessageNotificationAsync(NotificationUser notificationUser, MessageDto message)
        {
            try
            {
                List<TokenVm> tokens = await tokensService.GetAllUsersTokensAsync(new List<long> { notificationUser.UserId }).ConfigureAwait(false);
                var clientConnections = connectionsService.GetUserClientConnections(notificationUser.UserId);
                if (clientConnections != null)
                {
                    tokens = GetOfflineUserTokens(tokens, notificationUser.UserId);
                }
                if (tokens.Any())
                {
                    var messageVm = MessageConverter.GetMessageVm(message, notificationUser.UserId);
                    if (messageVm.Attachments?.Any(attach => attach.Type == AttachmentType.EncryptedMessage) ?? false)
                    {
                        messageVm.Attachments = null;
                    }

                    bool isCall = MarkdownHelper.ContainsMarkdownUserCalling(message.Text, notificationUser.UserId);
                    if (!notificationUser.IsMuted || isCall)
                    {
                        await SendNotificationRequestAsync(
                            tokens.Select(opt => opt.DeviceTokenId),
                            new NewMessageNotice(messageVm, isCall))
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private async Task SendChatMessageNotificationAsync(MessageDto message, List<NotificationUser> notificationsUsers)
        {
            try
            {
                IEnumerable<TokenVm> tokens = await tokensService.GetAllUsersTokensAsync(
                    notificationsUsers.Where(option => option.UserId != message.SenderId).Select(option => option.UserId),
                    true).ConfigureAwait(false);
                List<Task> noticeTasks = new List<Task>();
                foreach (var user in notificationsUsers)
                {
                    noticeTasks.Add(Task.Run(async () =>
                    {
                        var userTokens = GetOfflineUserTokens(tokens.ToList(), user.UserId);
                        var messageVm = MessageConverter.GetMessageVm(message, user.UserId);
                        if (messageVm.Attachments?.Any(attach => attach.Type == AttachmentType.EncryptedMessage) ?? false)
                        {
                            messageVm.Attachments = null;
                        }

                        bool isCall = MarkdownHelper.ContainsMarkdownUserCalling(message.Text, user.UserId);
                        if (isCall || !user.IsMuted)
                        {
                            await SendNotificationRequestAsync(
                                userTokens.Select(opt => opt.DeviceTokenId),
                                new NewMessageNotice(messageVm, isCall)).ConfigureAwait(false);
                        }
                    }));
                }
                await Task.WhenAll(noticeTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private async Task SendChannelMessageNotificationAsyhc(MessageDto message, List<NotificationUser> notificationsUsers)
        {
            try
            {
                var administratorsChannel = await loadChannelsService.GetAdministrationChannelUsersAsync(message.ConversationId).ConfigureAwait(false);
                IEnumerable<TokenVm> tokens = await tokensService.GetAllUsersTokensAsync(
                    notificationsUsers.Where(user => user.UserId != message.SenderId).Select(user => user.UserId)).ConfigureAwait(false);
                foreach (var user in notificationsUsers)
                {
                    bool isCall = MarkdownHelper.ContainsMarkdownUserCalling(message.Text, user.UserId);
                    var userTokens = GetOfflineUserTokens(tokens.ToList(), user.UserId);
                    List<TokenVm> adminTokens = tokens.Where(token => administratorsChannel.Any(admin => admin.UserId == token.UserId))?.ToList();
                    if (adminTokens != null && adminTokens.Any())
                    {
                        adminTokens = GetOfflineUserTokens(adminTokens, user.UserId);
                        var messageVm = MessageConverter.GetMessageVm(message, user.UserId);
                        if (messageVm.Attachments?.Any(attach => attach.Type == AttachmentType.EncryptedMessage) ?? false)
                        {
                            messageVm.Attachments = null;
                        }

                        if (adminTokens != null && (!user.IsMuted || isCall))
                        {
                            await SendNotificationRequestAsync(
                                adminTokens.Select(opt => opt.DeviceTokenId),
                                new NewMessageNotice(messageVm, isCall))
                                .ConfigureAwait(false);
                        }
                    }
                    else if (!user.IsMuted || isCall)
                    {
                        MessageVm subMessage = MessageConverter.GetMessageVm(message, user.UserId);
                        subMessage.SenderId = null;
                        await SendNotificationRequestAsync(
                            userTokens.Select(opt => opt.DeviceTokenId),
                            new NewMessageNotice(subMessage, isCall))
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private List<TokenVm> GetOfflineUserTokens(List<TokenVm> tokens, long userId)
        {
            try
            {
                var clientConnections = connectionsService.GetUserClientConnections(userId);
                if (clientConnections != null)
                {
                    var currentTokens = clientConnections.Select(opt => opt.CurrentToken);
                    return tokens.Where(token => token.UserId == userId)
                        ?.Where(opt => currentTokens.All(p => p.DeviceTokenId != opt.DeviceTokenId || p.AccessToken != opt.AccessToken))
                        ?.ToList();
                }
                else
                {
                    return tokens.Where(token => token.UserId == userId)?.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return new List<TokenVm>();
            }
        }
        private async Task SendNotificationRequestAsync(IEnumerable<string> deviceTokensId, object content)
        {
            try
            {
                if (deviceTokensId == null || !deviceTokensId.Any())
                {
                    return;
                }

                Notification notification = new Notification
                {
                    DevicesIds = deviceTokensId.ToList(),
                    NotificationContent = content
                };
                WebRequest webRequest = WebRequest.Create(NodeSettings.Configs.NotificationServerURL);
                webRequest.Method = HttpMethod.Post.Method;
                using (var stream = await webRequest.GetRequestStreamAsync().ConfigureAwait(false))
                {
                    using (StreamWriter streamWriter = new StreamWriter(stream))
                    {
                        await streamWriter.WriteAsync(ObjectSerializer.ObjectToJson(notification)).ConfigureAwait(false);
                    }
                }
                (await webRequest.GetResponseAsync().ConfigureAwait(false)).Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return;
            }
        }
    }
}