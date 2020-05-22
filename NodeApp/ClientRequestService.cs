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
using NodeApp.Interfaces;
using NodeApp.Objects;
using ObjectsLibrary.ClientRequests;
using ObjectsLibrary.ClientResponses;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp
{
    public class ClientRequestService
    {
        private static readonly Dictionary<KeyValuePair<long, ClientConnection>, TaskCompletionSource<KeyValuePair<ClientConnection, ClientResponse>>> responseTasks
            = new Dictionary<KeyValuePair<long, ClientConnection>, TaskCompletionSource<KeyValuePair<ClientConnection, ClientResponse>>>();
        private delegate void ResponseReceivedDelegate(ClientConnection connection, ClientResponse response);
        private readonly INoticeService noticeService;
        public ClientRequestService(INoticeService noticeService)
        {
            this.noticeService = noticeService;
        }
        public static void AddResponse(ClientConnection clientConnection, ClientResponse response)
        {
            if (responseTasks.TryGetValue(KeyValuePair.Create(response.RequestId, clientConnection), out var taskCompletionSource))
            {
                taskCompletionSource.SetResult(new KeyValuePair<ClientConnection, ClientResponse>(clientConnection, response));
            }
        }
        public async void ExchangeEncryptedPrivateKeysAsync(byte[] publicKey, List<ClientConnection> clientConnections, ClientConnection requestorConnection)
        {
            try
            {
                List<Task> tasks = new List<Task>();
                foreach (var connection in clientConnections)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        GetKeysClientRequest request = new GetKeysClientRequest(publicKey);
                        await SendRequestAsync(request, connection).ConfigureAwait(false);
                        var response = await GetResponseAsync(request, connection).ConfigureAwait(false);
                        if (response.Value != null && response.Value is EncryptedDataClientResponse clientResponse)
                        {
                            noticeService.SendEncryptedKeysNoticeAsync(
                                clientResponse.EncryptedData,
                                clientResponse.EncryptedSymmetricKey,
                                connection,
                                requestorConnection,
                                clientResponse.PublicKey);
                        }
                    }));
                }
                await Task.WhenAll().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        private async Task SendRequestAsync(ClientRequest request, ClientConnection clientConnection)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (clientConnection == null)
            {
                throw new ArgumentNullException(nameof(clientConnection));
            }

            try
            {
                byte[] requestData = Encoding.UTF8.GetBytes(ObjectSerializer.ObjectToJson(request));
                if (clientConnection.IsEncryptedConnection)
                {
                    requestData = Encryptor.SymmetricDataEncrypt(
                        requestData,
                        NodeData.Instance.NodeKeys.SignPrivateKey,
                        clientConnection.SymmetricKey,
                        ObjectsLibrary.Enums.MessageDataType.Binary,
                        NodeData.Instance.NodeKeys.Password);
                }
                if (clientConnection.IsProxiedClientConnection)
                {
                    await clientConnection.ProxyNodeWebSocket.SendAsync(
                        requestData,
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    await clientConnection.ClientSocket.SendAsync(
                        requestData,
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        private async Task<KeyValuePair<ClientConnection, ClientResponse>> GetResponseAsync(ClientRequest clientRequest, ClientConnection clientConnection, int timeoutMilliseconds = 60 * 1000)
        {
            try
            {
                TaskCompletionSource<KeyValuePair<ClientConnection, ClientResponse>> taskCompletionSource
                    = new TaskCompletionSource<KeyValuePair<ClientConnection, ClientResponse>>(TaskCreationOptions.RunContinuationsAsynchronously);
                responseTasks.Add(KeyValuePair.Create(clientRequest.RequestId, clientConnection), taskCompletionSource);
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    cancellationTokenSource.CancelAfter(timeoutMilliseconds);
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    var responseTask = taskCompletionSource.Task;
                    var responseWaitTask = Task.Run(async () => await responseTask.ConfigureAwait(false), cancellationTokenSource.Token);
                    while (responseWaitTask.Status == TaskStatus.Running)
                    {
                        cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                    return await responseWaitTask.ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                return new KeyValuePair<ClientConnection, ClientResponse>(clientConnection, null);
            }
        }
    }
}
