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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Enums;
using ObjectsLibrary.Exceptions;
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
    public class NoticeService : INoticeService
    {
        private readonly IConnectionsService connectionsService;
        private readonly IPushNotificationsService pushNotificationsService;
        public NoticeService(IAppServiceProvider appServiceProvider)
        {
            connectionsService = appServiceProvider.ConnectionsService;
            pushNotificationsService = appServiceProvider.PushNotificationsService;
        }
        public async void SendNewSessionNoticeAsync(ClientConnection connection)
        {
            try
            {
                pushNotificationsService.SendNewSessionNoticeAsync(connection);
                var clientConnections = connectionsService
                    .GetUserClientConnections(connection.UserId.GetValueOrDefault())?
                    .Where(opt => opt != connection)?.ToList();
                if (!clientConnections.IsNullOrEmpty())
                {
                    NewSessionNotice notice = new NewSessionNotice(new SessionVm
                    {
                        AppName = connection.CurrentToken.AppName,
                        DeviceName = connection.CurrentToken.DeviceName,
                        IP = connection.ClientIP.ToString(),
                        OSName = connection.CurrentToken.OSName,
                        TokenId = connection.CurrentToken.Id.GetValueOrDefault()
                    });
                    await SendNoticeToClientsAsync(clientConnections, notice).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async Task SendNeedLoginNoticeAsync(ClientConnection client)
        {
            NeedLoginNotice notice = new NeedLoginNotice();
            await SendNoticeToClientAsync(client, notice).ConfigureAwait(false);
        }

        public async void SendUserNodeChangedNoticeAsync(long userId, long newNodeId)
        {
            try
            {
                var clientConnections = connectionsService.GetUserClientConnections(userId);
                if (clientConnections != null)
                {
                    UserNodeChangedNotice notice = new UserNodeChangedNotice(userId, newNodeId);
                    await SendNoticeToClientsAsync(clientConnections, notice).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private async Task SendNoticeToClientAsync(ClientConnection client, Notice notice)
        {
            await SendNoticeToClientsAsync(new List<ClientConnection> { client }, notice).ConfigureAwait(false);
        }

        private static async Task SendNoticeToClientsAsync(IEnumerable<ClientConnection> clients, Notice notice)
        {
            try
            {
                foreach (var client in clients)
                {
                    try
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
                    catch (WebSocketException)
                    {
                        continue;
                    }
                    catch (CryptographicException ex)
                    {
                        Logger.WriteLog(ex);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex, notice.ToString());
            }
        }

        public async void SendEncryptedKeysNoticeAsync(byte[] encryptedData, byte[] encryptedSymmetricKey, ClientConnection responseConnection, ClientConnection requestorConnection, byte[] publicKey)
        {
            try
            {
                EncryptedKeysNotice encryptedKeysNotice = new EncryptedKeysNotice(
                    responseConnection.CurrentToken.OSName,
                    responseConnection.CurrentToken.AppName,
                    responseConnection.CurrentToken.DeviceName,
                    encryptedSymmetricKey,
                    encryptedData,
                    publicKey);
                await SendNoticeToClientAsync(requestorConnection, encryptedKeysNotice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async void SendPendingMessagesAsync(List<PendingMessageDto> pendingMessages, long receiverId)
        {
            try
            {
                if (pendingMessages == null || !pendingMessages.Any())
                {
                    return;
                }

                List<Notice> notices = pendingMessages
                    .Select(message => ObjectSerializer.JsonToObject<Notice>(
                        message.Content, new NoticeJsonConverter(), new BitArrayJsonConverter()))
                    .ToList();
                var clientConnections = connectionsService.GetUserClientConnections(receiverId);
                foreach (var notice in notices)
                {
                    await SendNoticeToClientsAsync(clientConnections, notice).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
    }
}