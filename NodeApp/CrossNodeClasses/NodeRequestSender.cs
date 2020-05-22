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
using NodeApp.CrossNodeClasses.Enums;
using NodeApp.CrossNodeClasses.Requests;
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.LicensorClasses;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Enums;
using ObjectsLibrary.Exceptions;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses
{
    public class NodeRequestSender : INodeRequestSender
    {
        private readonly IConnectionsService connectionsService;
        private readonly IFilesService filesService;
        private readonly INodesService nodesService;
        private readonly INodeNoticeService nodeNoticeService;

        public NodeRequestSender(
            IConnectionsService connectionsService,
            IFilesService filesService,
            INodesService nodesService,
            INodeNoticeService nodeNoticeService)
        {
            this.connectionsService = connectionsService;
            this.nodesService = nodesService;
            this.filesService = filesService;
            this.nodeNoticeService = nodeNoticeService;
        }
        private void SendRequest(NodeConnection node, NodeRequest request)
        {
            try
            {
                SendRequestAsync(new List<NodeConnection> { node }, request);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async Task<SearchNodeResponse> GetSearchResponseAsync(string searchQuery, long? navigationUserId, bool? direction, List<SearchType> searchTypes, long? requestorId, NodeConnection node)
        {
            try
            {
                SearchNodeRequest request = new SearchNodeRequest(searchQuery, navigationUserId, direction, searchTypes, requestorId);
                SendRequest(node, request);
                SearchNodeResponse response = (SearchNodeResponse)await GetResponseAsync(request, 5 * 1000).ConfigureAwait(false);
                return response;
            }
            catch
            {
                return null;
            }
        }
        public async Task<ChannelDto> GetChannelInformationAsync(long channelId, NodeConnection nodeConnection)
        {
            try
            {
                GetObjectsInfoNodeRequest request = new GetObjectsInfoNodeRequest(new List<long> { channelId }, null, Enums.NodeRequestType.GetChannels);
                SendRequest(nodeConnection, request);
                NodeResponse response = await GetResponseAsync(request).ConfigureAwait(false);
                return ((ChannelsNodeResponse)response).Channels.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private async void SendRequestAsync(List<NodeConnection> nodes, NodeRequest request)
        {
            foreach (var node in nodes)
            {
                byte[] requestData = Array.Empty<byte>();
                try
                {
                    if (node.IsEncryptedConnection)
                    {
                        requestData = Encryptor.SymmetricDataEncrypt(
                            ObjectSerializer.ObjectToByteArray(request),
                            NodeData.Instance.NodeKeys.SignPrivateKey,
                            node.SymmetricKey,
                            MessageDataType.Request,
                            NodeData.Instance.NodeKeys.Password);
                    }
                    else
                    {
                        requestData = ObjectSerializer.ObjectToByteArray(request);
                    }
                    await node.NodeWebSocket.SendAsync(
                        requestData,
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex, request);
                }
            }
        }

        public async Task<ProxyUsersCommunicationsNodeResponse> SendProxyUsersCommunicationsNodeRequestAsync(
            byte[] communicationData, long userId, NodeConnection nodeConnection, ObjectType objectType,
            byte[] userPublicKey, byte[] signPublicKey)
        {
            ProxyUsersCommunicationsNodeRequest nodeRequest = new ProxyUsersCommunicationsNodeRequest(
                communicationData,
                userId,
                objectType,
                userPublicKey,
                signPublicKey);
            SendRequest(nodeConnection, nodeRequest);
            return (ProxyUsersCommunicationsNodeResponse)await GetResponseAsync(nodeRequest, (int)TimeSpan.FromSeconds(30).TotalMilliseconds).ConfigureAwait(false);
        }
        public async Task<NodeResponse> GetResponseAsync(NodeRequest request, int timeoutMilliseconds = 10 * 1000)
        {
            try
            {
                TaskCompletionSource<NodeResponse> taskCompletionSource = new TaskCompletionSource<NodeResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                NodeDataReceiver.ResponseTasks.AddOrUpdate(request.RequestId, taskCompletionSource, (value, newValue) =>
                {
                    return newValue;
                });
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeoutMilliseconds);
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                var responseWaitTask = Task.Factory.StartNew(
                    () => taskCompletionSource.Task,
                    cancellationTokenSource.Token,
                    TaskCreationOptions.RunContinuationsAsynchronously,
                    TaskScheduler.Current);
                var responseTask = await responseWaitTask.ConfigureAwait(false);
                if (await Task.WhenAny(responseTask, Task.Delay(timeoutMilliseconds)).ConfigureAwait(false) == responseTask)
                {
                    var response = await responseTask.ConfigureAwait(false);
                    return response;
                }
                else
                    throw new ResponseException("Response timed out.");
            }
            catch (TaskCanceledException)
            {
                throw new ResponseException("Response timed out.");
            }
        }
        public async Task<List<ChatUserVm>> GetChatUsersInformationAsync(List<long> usersId, long chatId, NodeConnection nodeConnection)
        {
            try
            {
                GetChatUsersInformationNodeRequest request = new GetChatUsersInformationNodeRequest(usersId, chatId);
                SendRequest(nodeConnection, request);
                ChatUsersNodeResponse response = (ChatUsersNodeResponse)await GetResponseAsync(request).ConfigureAwait(false);
                return response.ChatUsers;
            }
            catch
            {
                return null;
            }
        }
        private async Task<List<BlockVm>> GetBlocksAsync(NodeConnection node, long startId, long endId)
        {
            HttpWebRequest webRequest = WebRequest.CreateHttp($"https://{node.Uri.Authority}/Blockchain/Download?startId={startId}&endId={endId}");
            webRequest.Method = "GET";
            using (var response = await webRequest.GetResponseAsync().ConfigureAwait(false))
            {
                byte[] responseBuffer = new byte[128 * 1024];
                List<byte> responseData = new List<byte>();
                using (Stream stream = response.GetResponseStream())
                {
                    using (BinaryReader bReader = new BinaryReader(stream))
                    {
                        int count;
                        while ((count = bReader.Read(responseBuffer, 0, responseBuffer.Length)) > 0)
                        {
                            responseData.AddRange(responseBuffer.Take(count));
                            await stream.FlushAsync().ConfigureAwait(false);
                        }
                        return ObjectSerializer.ByteArrayToObject<List<BlockVm>>(responseData.ToArray());
                    }
                }
            }
        }
        public async Task<List<BlockVm>> GetBlocksAsync(long start, long end)
        {
            var nodeConnection = connectionsService.GetNodeConnections()
                .Where(connection => connection.NodeWebSocket.State == System.Net.WebSockets.WebSocketState.Open)
                .FirstOrDefault();
            if (nodeConnection != null)
            {
                return await GetBlocksAsync(nodeConnection, start, end).ConfigureAwait(false);
            }
            else
            {
                return null;
            }
        }
        public async void SendConnectRequestAsync(NodeConnection nodeConnection)
        {
            NodeKeysDto publicKey = null;
            try
            {                
                if (nodeConnection.Node == null
                    || nodeConnection.Node.NodeKey == null
                    || nodeConnection.Node.NodeKey.EncPublicKey.IsNullOrEmpty()
                    || nodeConnection.Node.NodeKey.SignPublicKey.IsNullOrEmpty()
                    || nodeConnection.Node.NodeKey.ExpiredAt < DateTime.UtcNow.ToUnixTime())
                {
                    publicKey = await GetNodePublicKeyAsync(nodeConnection).ConfigureAwait(false);
                }
                if (publicKey == null)
                {
                    publicKey = new NodeKeysDto
                    {
                        ExpirationTime = nodeConnection.Node.NodeKey.ExpiredAt,
                        KeyId = nodeConnection.Node.NodeKey.KeyId,
                        PublicKey = nodeConnection.Node.NodeKey.EncPublicKey,
                        SignPublicKey = nodeConnection.Node.NodeKey.SignPublicKey
                    };
                }
                byte[] symmetricKey;
                byte[] encryptedKey = null;
                if (nodeConnection.IsEncryptedConnection)
                {
                    symmetricKey = nodeConnection.SymmetricKey;
                }
                else
                {                   
                    symmetricKey = Encryptor.GetSymmetricKey(256, RandomExtensions.NextInt64(), uint.MaxValue, NodeData.Instance.NodeKeys.Password);                   
                }
                encryptedKey = Encryptor.AsymmetricDataEncrypt(
                       symmetricKey,
                       publicKey.PublicKey,
                       NodeData.Instance.NodeKeys.SignPrivateKey,
                       NodeData.Instance.NodeKeys.Password);
                ConnectData connectData = new ConnectData
                {
                    License = NodeSettings.Configs.License,
                    LicensorSign = NodeSettings.Configs.LicensorSign.Sign,
                    Node = NodeSettings.Configs.Node,
                    SymmetricKey = symmetricKey
                };
                byte[] encryptedRequestData = Encryptor.SymmetricDataEncrypt(
                    ObjectSerializer.ObjectToByteArray(connectData),
                    NodeData.Instance.NodeKeys.SignPrivateKey,
                    symmetricKey,
                    MessageDataType.Binary,
                    NodeData.Instance.NodeKeys.Password);
                NodeRequest request = new ConnectNodeRequest(encryptedKey, encryptedRequestData, NodeData.Instance.PublicKeys);
                SendRequest(nodeConnection, request);
                NodeResponse response = await GetResponseAsync(request, (int)TimeSpan.FromSeconds(30).TotalMilliseconds).ConfigureAwait(false);
                if (response is NodesInformationResponse informationResponse)
                {
                    ConnectData responseData = informationResponse.GetConnectData(symmetricKey, publicKey.SignPublicKey, NodeData.Instance.NodeKeys.Password);
                    responseData.Node.StartDay = responseData.Node.StartDay.ToUniversalTime();
                    var nodeJson = ObjectSerializer.ObjectToJson(responseData.Node);
                    var isValid = Encryptor.CheckSign(
                        responseData.LicensorSign,
                        LicensorClient.Instance.GetSignPublicKey(),
                        Encoding.UTF8.GetBytes(nodeJson),
                        NodeData.Instance.NodeKeys.Password);
                    if (!isValid)
                        return;
                    LicenseVm license = responseData.License;
                    long currentTime = DateTime.UtcNow.ToUnixTime();
                    if (license.ExpiredAt <= currentTime)
                        return;
                    foreach (LicenseSegmentVm segment in license.LicenseSegments)
                    {
                        if (segment.StartAt <= currentTime && segment.LicensorSign != null && segment.LicensorSign.Sign != null)
                        {
                            var jsonData = ObjectSerializer.ObjectToJson(segment.GetSignObject());
                            bool isSegmentValid = Encryptor.CheckSign(
                                segment.LicensorSign.Sign,
                                LicensorClient.Instance.GetSignPublicKey(),
                                Encoding.UTF8.GetBytes(jsonData),
                                NodeData.Instance.NodeKeys.Password);
                            if (!isSegmentValid)
                                return;
                        }
                    }
                    nodeConnection.Node = responseData.Node;
                    nodeConnection.Uri = new Uri($"wss://{responseData.Node.Domains.FirstOrDefault()}:{responseData.Node.NodesPort}");
                    nodeConnection.PublicKey = publicKey.PublicKey;
                    nodeConnection.SymmetricKey = symmetricKey;
                    nodeConnection.PublicKeyId = publicKey.KeyId;
                    nodeConnection.PublicKeyExpirationTime = publicKey.ExpirationTime;
                    nodeConnection.SignPublicKey = publicKey.SignPublicKey;
                    nodesService.CreateOrUpdateNodeInformationAsync(responseData.Node);
                    connectionsService.AddOrUpdateNodeConnection(nodeConnection.Node.Id, nodeConnection);
                    await nodeNoticeService.SendPendingMessagesAsync(nodeConnection.Node.Id).ConfigureAwait(false);
                }
                else if (response is ResultNodeResponse resultResponse)
                {
                    if (nodeConnection?.Uri != null)
                    {
                        Console.WriteLine($"Node URI: {nodeConnection.Uri.AbsoluteUri} Message: {resultResponse.Message}");
                    }
                    else
                    {
                        Console.WriteLine(resultResponse.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                if(publicKey != null) 
                {
                    Console.WriteLine($"PublicKey Id:{publicKey.KeyId}, Expired at: {publicKey.ExpirationTime.ToDateTime().ToString()}, Generated at: {publicKey.GenerationTime.ToDateTime().ToString()}, NodeId:{publicKey.NodeId}");
                }
                Logger.WriteLog(ex);
                nodeConnection.NodeWebSocket.Abort();
            }
        }
        public async Task<PollDto> GetPollInformationAsync(long conversationId, ConversationType conversationType, Guid pollId, NodeConnection nodeConnection)
        {
            GetPollInformationNodeRequest request = new GetPollInformationNodeRequest(pollId, conversationType, conversationId);
            SendRequest(nodeConnection, request);
            var response = await GetResponseAsync(request, 10000).ConfigureAwait(false);
            if (response is PollNodeResponse pollNodeResponse)
            {
                return pollNodeResponse.Poll;
            }
            else
            {
                return null;
            }
        }
        public void SendConnectRequestAsync(List<NodeConnection> nodes)
        {
            foreach (var nodeConnection in nodes)
            {
                SendConnectRequestAsync(nodeConnection);
            }
        }
        public async Task<List<UserVm>> GetUsersInfoAsync(List<long> usersId, long? requestorUserId, NodeConnection nodeConnection)
        {
            try
            {
                GetObjectsInfoNodeRequest request = new GetObjectsInfoNodeRequest(usersId, requestorUserId, Enums.NodeRequestType.GetUsers);
                SendRequest(nodeConnection, request);
                UsersNodeResponse nodeResponse = (UsersNodeResponse)await GetResponseAsync(request).ConfigureAwait(false);
                return nodeResponse.Users;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return new List<UserVm>();
            }
        }
        public async Task<GetObjectsInfoNodeRequest> GetChatsInfoAsync(List<long> chatsId, NodeConnection nodeConnection)
        {
            GetObjectsInfoNodeRequest request = new GetObjectsInfoNodeRequest(chatsId, null, Enums.NodeRequestType.GetChats);
            SendRequest(nodeConnection, request);
            return request;
        }
        public async Task DownloadFileNodeRequestAsync(string fileId, NodeConnection nodeConnection)
        {
            try
            {
                WebClient webClient = new WebClient();
                string fileUri = $"https://{nodeConnection.Uri.Host}:{nodeConnection.Node.NodesPort}/api/Files/{fileId}";
                await webClient.DownloadFileTaskAsync(fileUri, fileId).ConfigureAwait(false);
                ContentDisposition contentDisposition = new ContentDisposition(webClient.ResponseHeaders["Content-Disposition"]);
                webClient.Dispose();
                string filePath = Path.Combine(NodeSettings.LOCAL_FILE_STORAGE_PATH, $"[{RandomExtensions.NextString(10)}]{contentDisposition.FileName}");
                if (File.Exists(fileId))
                {
                    File.Move(fileId, filePath);
                }
                await filesService.UpdateFileInformationAsync(filePath, fileId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new DownloadFileException("Error while downloading file.", ex);
            }
        }
        public async Task<ValuePair<TokenVm, UserVm>> CheckTokenAsync(long userId, TokenVm token, long nodeId)
        {
            var nodeConnection = connectionsService.GetNodeConnection(nodeId);
            if (nodeConnection != null)
            {
                CheckTokenNodeRequest request = new CheckTokenNodeRequest(userId, token);
                SendRequest(nodeConnection, request);
                NodeResponse nodeResponse = await GetResponseAsync(request).ConfigureAwait(false);
                switch (nodeResponse.ResponseType)
                {
                    case NodeResponseType.UserTokens:
                        {
                            UserTokensNodeResponse tokensResponse = (UserTokensNodeResponse)nodeResponse;
                            return new ValuePair<TokenVm, UserVm>(tokensResponse.Token, tokensResponse.User);
                        }
                    default:
                        return null;
                }
            }
            else
                return null;
        }
        public async Task<ChatVm> GetFullChatInformationAsync(long chatId, NodeConnection nodeConnection)
        {
            try
            {
                GetFullChatInformationNodeRequest request = new GetFullChatInformationNodeRequest(chatId);
                SendRequest(nodeConnection, request);
                ChatsNodeResponse response = (ChatsNodeResponse)await GetResponseAsync(request).ConfigureAwait(false);
                return response.Chats.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return null;
            }
        }
        public async Task<List<MessageDto>> GetMessagesAsync(NodeConnection connection, long conversationId, ConversationType conversationType, Guid? messageId, List<AttachmentType> attachmentsTypes, bool direction = true, int length = 1000)
        {
            if (connection == null)
            {
                return new List<MessageDto>();
            }
            GetMessagesNodeRequest request = new GetMessagesNodeRequest(conversationType, conversationId, messageId, attachmentsTypes, direction, length);
            SendRequest(connection, request);
            NodeResponse response = await GetResponseAsync(request).ConfigureAwait(false);
            if (response.ResponseType == Enums.NodeResponseType.Messages)
            {
                MessagesNodeResponse messagesResponse = (MessagesNodeResponse)response;
                return messagesResponse.Messages;
            }
            else
            {
                throw new BadResponseException();
            }
        }
        public async Task<UserDto> DownloadUserDataAsync(string operationId, long nodeId)
        {
            var nodeConnection = connectionsService.GetNodeConnection(nodeId);
            if (nodeConnection != null)
            {
                string requestUriString = $"https://{nodeConnection.Uri.Authority}/UserMigration/Download";
                HttpWebRequest httpWebRequest = WebRequest.CreateHttp(requestUriString);
                byte[] encryptedData = Encryptor.SymmetricDataEncrypt(
                    Encoding.UTF8.GetBytes(operationId),
                    NodeData.Instance.NodeKeys.SignPrivateKey,
                    nodeConnection.SymmetricKey,
                    MessageDataType.Binary,
                    NodeData.Instance.NodeKeys.Password);
                httpWebRequest.Headers.Add("encryptedOperationId", Convert.ToBase64String(encryptedData));
                httpWebRequest.Headers.Add("nodeId", NodeSettings.Configs.Node.Id.ToString());
                using (WebResponse response = await httpWebRequest.GetResponseAsync().ConfigureAwait(false))
                {
                    var responseStream = response.GetResponseStream();
                    byte[] responseData = responseStream.ReadAllBytes();
                    byte[] decryptedData = Encryptor.SymmetricDataDecrypt(
                        responseData,
                        nodeConnection.SignPublicKey,
                        nodeConnection.SymmetricKey,
                        NodeData.Instance.NodeKeys.Password).DecryptedData;
                    return ObjectSerializer.ByteArrayToObject<UserDto>(decryptedData);
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(nodeId));
            }
        }
        public async Task<NodeKeysDto> GetNodePublicKeyAsync(NodeConnection nodeConnection, long? keyId = null)
        {            
            NodeRequest nodeRequest = keyId == null ? new GetPublicKeyNodeRequest() : new GetPublicKeyNodeRequest(keyId.Value);
            SendRequest(nodeConnection, nodeRequest);            
            try
            {
                NodeResponse response = await GetResponseAsync(nodeRequest).ConfigureAwait(false);                
                switch (response.ResponseType)
                {
                    case Enums.NodeResponseType.PublicKey:
                        {
                            PublicKeyNodeResponse nodeKeysResponse = (PublicKeyNodeResponse)response;
                            return new NodeKeysDto
                            {
                                NodeId = (nodeConnection.Node?.Id).GetValueOrDefault(),
                                KeyId = nodeKeysResponse.KeyId,
                                PublicKey = nodeKeysResponse.PublicKey,
                                ExpirationTime = nodeKeysResponse.ExpirationTime,
                                SignPublicKey = nodeKeysResponse.SignPublicKey
                            };
                        }
                    default:
                        return null;
                }
            }
            catch (ResponseException ex)
            {
                Logger.WriteLog(ex);
                return null;
            }
        }
        public async Task<List<UserVm>> BatchPhonesSearchAsync(NodeConnection nodeConnection, List<string> phones, long? requestorId)
        {
            BatchPhonesSearchNodeRequest request = new BatchPhonesSearchNodeRequest(phones, requestorId);
            SendRequest(nodeConnection, request);
            var response = await GetResponseAsync(request).ConfigureAwait(false);
            if (response is UsersNodeResponse usersResponse)
            {
                return usersResponse.Users;
            }
            return Enumerable.Empty<UserVm>().ToList();
        }
        public async Task<List<FileInfoVm>> GetFilesInformationAsync(List<string> filesIds, long? nodeId = null)
        {
            NodeConnection nodeConnection;
            if (nodeId != null)
                nodeConnection = connectionsService.GetNodeConnection(nodeId.Value);
            else
                nodeConnection = connectionsService.GetNodeConnections()
                    .FirstOrDefault(opt => opt.Node?.Id != NodeSettings.Configs.Node.Id && opt.NodeWebSocket?.State == WebSocketState.Open);
            if (nodeConnection != null)
            {
                GetObjectsInfoNodeRequest request = new GetObjectsInfoNodeRequest(filesIds);
                SendRequest(nodeConnection, request);
                var response = await GetResponseAsync(request).ConfigureAwait(false);
                if (response is FilesInformationResponse filesResponse)
                {
                    return filesResponse.FilesInfo;
                }
                if (response != null)
                    Console.WriteLine(response.ToJson());
            }
            return null;
        }
    }
}
