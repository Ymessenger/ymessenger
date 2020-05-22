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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.ClientResponses;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Enums;
using ObjectsLibrary.Exceptions;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp
{
    public class ClientDataReceiver : DataReceiver
    {
        private ResultResponse errorResponse;
        private readonly ClientConnection client;
        private readonly IConnectionsService connectionsService;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly INodeRequestSender nodeRequestSender;

        public ClientDataReceiver(ClientConnection client, IAppServiceProvider serviceProvider) : base(serviceProvider)
        {
            socket = client.ClientSocket;
            connectionsService = serviceProvider.ConnectionsService;
            nodeNoticeService = serviceProvider.NodeNoticeService;
            nodeRequestSender = serviceProvider.NodeRequestSender;
            this.client = client;

        }

        public override async Task BeginReceiveAsync()
        {
            ClientWebSocketRequestManager reqManager = new ClientWebSocketRequestManager(client, _appServiceProvider);
            Task requestWatch = Task.Run(WatchForRequestsAsync);
            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    byte[] receivedData = await ReceiveBytesAsync().ConfigureAwait(false);
                    requestCount++;
                    if (CheckRequestFrequency() && receivedData != null && receivedData.Any())
                    {
                        if (client.IsProxiedClientConnection)
                        {
                            await nodeRequestSender.SendProxyUsersCommunicationsNodeRequestAsync(
                                receivedData,
                                client.UserId.GetValueOrDefault(),
                                connectionsService.GetNodeConnections().FirstOrDefault(opt => opt.NodeWebSocket == client.ProxyNodeWebSocket),
                                ObjectType.Request,
                                client.PublicKey,
                                client.SignPublicKey).ConfigureAwait(false);
                            continue;
                        }
                        CommunicationObject communicationObject = ConvertReceivedDataToCommucicatonObject(receivedData);
                        switch (communicationObject.Type)
                        {
                            case ObjectType.Request:
                                reqManager.HandleRequestAsync((Request)communicationObject, receivedData.Length);
                                break;
                            case ObjectType.ClientResponse:
                                reqManager.HandleClientResponse((ClientResponse)communicationObject);
                                break;
                            default:
                                throw new ArgumentException($"{communicationObject.Type} not supported in current context.");
                        }
                    }
                    else
                    {
                        errorResponse = new ResultResponse(-1, "Sorry, too many requests.", ErrorCode.TooManyRequests);
                        await socket.SendAsync(
                            ObjectSerializer.ResponseToBytes(errorResponse),
                            WebSocketMessageType.Binary,
                            true,
                            CancellationToken.None).ConfigureAwait(false);
                    }
                }
                catch (WebSocketException)
                {
                    var clientConnections = connectionsService.GetUserClientConnections(client.UserId.GetValueOrDefault());
                    if (clientConnections != null)
                    {
                        connectionsService.RemoveClientConnection(client);
                        if (client.IsProxiedClientConnection)
                        {
                            nodeNoticeService.SendClientDisconnectedNodeNoticeAsync(client.UserId.GetValueOrDefault(), client.ProxyNodeWebSocket);
                        }
                    }
                }
                catch (DeserializationException ex)
                {
                    long requestId;
                    try
                    {
                        var data = ObjectSerializer.JsonToObject<dynamic>(ex.Message);
                        requestId = (long)data["RequestId"];
                    }
                    catch
                    {
                        Logger.WriteLog(ex);
                        requestId = -1;
                    }
                    errorResponse = new ResultResponse(requestId, "Unable to deserialize request data.", ErrorCode.InvalidRequestFormat);
                    await socket.SendAsync(
                        ObjectSerializer.ResponseToBytes(errorResponse),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None).ConfigureAwait(false);
                }
                catch (TooLargeReceivedDataException ex)
                {
                    Logger.WriteLog(ex);
                    errorResponse = new ResultResponse(-1, null, ErrorCode.TooLargeRequestData);
                    await socket.SendAsync(
                        ObjectSerializer.ResponseToBytes(errorResponse),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex);
                }
            }
        }
        private CommunicationObject ConvertReceivedDataToCommucicatonObject(byte[] data)
        {
            if (client.IsEncryptedConnection)
            {
                data = Encryptor.SymmetricDataDecrypt(
                    data,
                    client.SignPublicKey,
                    client.SymmetricKey,
                    NodeData.Instance.NodeKeys.Password)
                    .DecryptedData;
            }
            return ObjectSerializer.BytesToCommunicationObject(
                data,
                Newtonsoft.Json.NullValueHandling.Ignore,
                new RequestJsonConverter(),
                new ClientResponseJsonConverter(),
                new CommunicationObjectJsonConverter());
        }
    }
}
