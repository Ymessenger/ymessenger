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
using NodeApp.Blockchain;
using NodeApp.CrossNodeClasses;
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.ExceptionClasses;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Enums;
using ObjectsLibrary.LicensorNoticeClasses;
using ObjectsLibrary.LicensorRequestClasses;
using ObjectsLibrary.LicensorResponseClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp.LicensorClasses
{
    public class LicensorClient
    {
        private static readonly Lazy<LicensorClient> singleton = new Lazy<LicensorClient>(() => new LicensorClient());
        private ClientWebSocket webSocket = new ClientWebSocket();        
        private readonly Dictionary<long, TaskCompletionSource<LicensorResponse>> responseTasks = new Dictionary<long, TaskCompletionSource<LicensorResponse>>();
        private delegate LicensorResponse ResponseReceivedDelegate(LicensorResponse response);
        private event ResponseReceivedDelegate ResponseReceivedEvent;
        private byte[] symmetricKey;
        private byte[] licensorPublicKey;
        private byte[] licensorSignPublicKey;
        private long? expiredAt;

        public byte[] GetSignPublicKey()
        {
            return (byte[])licensorSignPublicKey.Clone();
        }
        private bool IsEncrypted
        {
            get
            {
                return symmetricKey != null 
                    && symmetricKey.Any() 
                    && licensorPublicKey != null 
                    && licensorPublicKey.Any() 
                    && licensorSignPublicKey != null 
                    && licensorSignPublicKey.Any();
            }
        }
        public bool IsAuthentificated
        {
            get
            {
                return webSocket.State == WebSocketState.Open && IsEncrypted;
            }
        }
        private LicensorClient()
        {
            try
            {
                ResponseReceivedEvent += ResponseReceivedHandler;
                ConnectAndListenAsync().Wait();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return;
            }
        }     
        private async Task<bool> TryConnectAsync(bool authRequired = true)
        {
            if (authRequired == false)
                return true;
            try
            {
                if(webSocket.State != WebSocketState.Open)
                    await ConnectAndListenAsync().ConfigureAwait(false);
                if (!IsAuthentificated && authRequired)
                {
                    bool authResult = await AuthAsync().ConfigureAwait(false);
                    return authResult;
                }
                return true;
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
                return false;
            }
        }
        public static LicensorClient Instance => singleton.Value;
        private async Task ConnectAndListenAsync()
        {
            if (webSocket.State == WebSocketState.None || webSocket.State == WebSocketState.Aborted)
            {
                webSocket.Dispose();
                webSocket = new ClientWebSocket();
                symmetricKey = Array.Empty<byte>();
                await webSocket.ConnectAsync(new Uri($"wss://{NodeSettings.Configs.LicensorUrl}/"), CancellationToken.None).ConfigureAwait(false);
                BeginReceiveAsync();
            }
        }        
        public async Task<BlockchainInfo> GetBlockchainInfoAsync()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    var data = await webClient.DownloadDataTaskAsync($"https://{NodeSettings.Configs.LicensorUrl}/api/Blockchain/Information").ConfigureAwait(false);
                    return ObjectSerializer.ByteArrayToObject<BlockchainInfo>(data);
                }
            }
            catch(WebException ex)
            {
                Logger.WriteLog(ex);
                return null;
            }

        }
        public async Task<bool> AuthAsync(bool recursive = false)
        {
            if (string.IsNullOrWhiteSpace(NodeSettings.Configs.LicensorUrl))
                return false;
            if (webSocket.State != WebSocketState.Open)
            {
                try
                {
                    await ConnectAndListenAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new LicensorException("No connection.", ex);
                }
            }
            try
            {
                AuthRequest request = new AuthRequest(NodeData.Instance.NodeKeys.PublicKey, NodeSettings.Configs.Node.Id);
                await SendRequestAsync(request).ConfigureAwait(false);
                var response = await GetResponseAsync(request).ConfigureAwait(false);
                if (response is AuthResponse authResponse)
                {
                    symmetricKey = Encryptor.AsymmetricDecryptKey(
                        authResponse.EncryptedKey,
                        NodeData.Instance.NodeKeys.PrivateKey,
                        authResponse.LicensorSignPublicKey,
                        NodeData.Instance.NodeKeys.Password).Data;                    
                    var nodeDecryptedData = Encryptor.SymmetricDataDecrypt(
                        authResponse.EncryptedNode,
                        authResponse.LicensorSignPublicKey,
                        symmetricKey,
                        NodeData.Instance.NodeKeys.Password).DecryptedData;
                    var tokenDecryptedData = Encryptor.SymmetricDataDecrypt(
                        authResponse.EncryptedAccessToken,
                        authResponse.LicensorSignPublicKey,
                        symmetricKey,
                        NodeData.Instance.NodeKeys.Password).DecryptedData;                    
                    NodeVm node = ObjectSerializer.ByteArrayToObject<NodeVm>(nodeDecryptedData);
                    var licensorSign = authResponse.LicensorSign;
                    node.StartDay = node.StartDay.ToUniversalTime();
                    var nodeJson = ObjectSerializer.ObjectToJson(node);                                                                         
                    licensorPublicKey = authResponse.LicensorPublicKey;
                    licensorSignPublicKey = authResponse.LicensorSignPublicKey;
                    expiredAt = DateTime.UtcNow.AddDays(7).ToUnixTime();
                    bool isValid = Encryptor.CheckSign(
                                licensorSign.Sign,
                                licensorSignPublicKey,
                                Encoding.UTF8.GetBytes(nodeJson),
                                NodeData.Instance.NodeKeys.Password);
                    if (isValid)
                    {
                        NodeSettings.Configs.LicensorSign = licensorSign;
                    }                    
                    return true;
                }
                else if (response is ResultResponse resultResponse)
                {
                    if(resultResponse.ErrorCode == ObjectsLibrary.LicensorResponseClasses.ErrorCode.KeyNotFound || resultResponse.ErrorCode == ObjectsLibrary.LicensorResponseClasses.ErrorCode.IdNotFound)
                    {
                        if (!recursive)
                        {
                            var nodeKeys = await AppServiceProvider.Instance.KeysService.GetActualNodeKeysAsync(NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                            await AddNewKeyAsync(nodeKeys.PublicKey, nodeKeys.SignPublicKey, nodeKeys.KeyId, nodeKeys.ExpirationTime, nodeKeys.GenerationTime, false).ConfigureAwait(false);
                            await AuthAsync(recursive: true).ConfigureAwait(false);
                            return true;
                        }
                    }
                    throw new ResponseException($"Authorization failed. Response error message: {resultResponse.ErrorMessage}");
                }
                return false;
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
                return false;
            }
        }
        public async Task<NodeVm> GetNodeAsync(byte[] nodePublicKey, long nodeId)
        {
            if (!await TryConnectAsync().ConfigureAwait(false))
                return new NodeVm();
            GetNodesRequest request = new GetNodesRequest(nodePublicKey, nodeId);            
            await SendRequestAsync(request).ConfigureAwait(false);
            var response = await GetResponseAsync(request).ConfigureAwait(false);
            if(response is NodesResponse nodesResponse)
            {
                return nodesResponse.Nodes.FirstOrDefault();
            }
            else
            {
                var resultResponse = response as ResultResponse;
                throw new ResponseException($"Failed to get node. Response error message: {resultResponse.ErrorMessage}");
            }
        }
        public async Task<List<NodeVm>> GetNodesAsync(long? nodeId, string searchQuery, List<long> nodesIds)
        {
            if (!await TryConnectAsync().ConfigureAwait(false))
                return new List<NodeVm>();
            GetNodesRequest request = nodesIds != null 
                ? new GetNodesRequest(nodesIds) 
                : new GetNodesRequest(nodeId, searchQuery);
            await SendRequestAsync(request).ConfigureAwait(false);
            var response = await GetResponseAsync(request).ConfigureAwait(false);
            if (response is NodesResponse nodesResponse)
            {
                return nodesResponse.Nodes;
            }
            else if (response is ResultResponse resultResponse)
            {
                throw new ResponseException($"Failed to get nodes list. Response error message: {resultResponse.ErrorMessage}");
            }
            else
            {
                throw new ResponseException($"Invalid response. {response.ResponseType}");
            }
        }
        public async Task<NodePools> GetNodePoolsAsync(params PoolType[] pools)
        {
            if (!await TryConnectAsync().ConfigureAwait(false))
                throw new LicensorException("Unable to establish a connection.");
            GetPoolsRequest request = new GetPoolsRequest(pools);
            await SendRequestAsync(request).ConfigureAwait(false);
            var response = await GetResponseAsync(request).ConfigureAwait(false);
            if(response is PoolsResponse poolsResponse)
            {
                return poolsResponse.NodePools;
            }
            else if(response is ResultResponse resultResponse)
            {
                throw new ResponseException($"Failed to get node pools. Response error message: {resultResponse.ErrorMessage}");
            }
            else
            {
                throw new ResponseException($"Invalid response. {response.ResponseType}");
            }
        }
        public async Task<byte[]> VerifyNodeAsync(byte[] sequence, byte[] encrytedKey, byte[] userPublicKey)
        {
            if(!await TryConnectAsync().ConfigureAwait(false))
                throw new LicensorException("Unable to establish connection with licensor.");
            VerifyNodeRequest request = new VerifyNodeRequest(sequence, userPublicKey, encrytedKey);
            await SendRequestAsync(request).ConfigureAwait(false);
            var response = await GetResponseAsync(request).ConfigureAwait(false);
            if(response is VerificationResponse verificationResponse)
            {
                return verificationResponse.EncryptedSequence;
            }
            else if(response is ResultResponse resultResponse)
            {
                throw new ResponseException($"Failed to verify node. Response error message: {resultResponse.ErrorMessage}");
            }
            else
            {
                throw new ResponseException($"Invalid response. {response.ResponseType}");
            }
        }
        public async Task AddNewBlockAsync(BlockVm block)
        {
            if (!await TryConnectAsync().ConfigureAwait(false))
                return;
            AddNewBlockRequest request = new AddNewBlockRequest(block);
            await SendRequestAsync(request).ConfigureAwait(false);
            var response = await GetResponseAsync(request).ConfigureAwait(false);
            if(response is ResultResponse resultResponse)
            {
                if (!string.IsNullOrWhiteSpace(resultResponse.ErrorMessage))
                {
                    BlockchainSynchronizationService synchronizationService = new BlockchainSynchronizationService();
                    await synchronizationService.CheckAndSyncBlockchainAsync().ConfigureAwait(false);                    
                }
            }
        }
        public async Task AddNewKeyAsync(byte[] publicKey, byte[] signPublicKey, long keyId, long expirationTime, long generationTime, bool withAuth)
        {
            if (withAuth)
            {
                if (!await TryConnectAsync(withAuth).ConfigureAwait(false))
                    throw new LicensorException("Unable to establish connection with licensor.");
            }
            if (webSocket.State != WebSocketState.Open)
            {
                await ConnectAndListenAsync().ConfigureAwait(false);
            }
            AddNewKeyRequest request = new AddNewKeyRequest(publicKey, signPublicKey, keyId, expirationTime, generationTime, NodeSettings.Configs.Node.Id);
            await SendRequestAsync(request).ConfigureAwait(false);
            var response = await GetResponseAsync(request).ConfigureAwait(false);
            if (response is ResultResponse resultResponse)
            {
                if (!string.IsNullOrWhiteSpace(resultResponse.ErrorMessage))
                    throw new ResponseException($"Failed to add new key. Response error message: {resultResponse.ErrorMessage}");
            }
        }
        public async Task<NodeVm> EditNodeAsync(NodeVm node)
        {
            if (!await TryConnectAsync().ConfigureAwait(false))
                throw new LicensorException("Unable to establish connection with licensor.");
            EditNodeRequest request = new EditNodeRequest(node);
            await SendRequestAsync(request).ConfigureAwait(false);
            var response = await GetResponseAsync(request).ConfigureAwait(false);
            if (response is EditNodeResponse editResponse)
            {
                NodeSettings.Configs.LicensorSign = editResponse.LicensorSign;
                return editResponse.Node;
            }
            else if (response is ResultResponse resultResponse)
                throw new ResponseException($"Failed to edit node. Response error message: {resultResponse.ErrorMessage}");
            throw new ResponseException($"Unknown response type: {response.ResponseType}");
        }
        private async Task<byte[]> ReceiveBytesAsync(uint bufferLength = 16 * 1024)
        {
            byte[] buffer = new byte[bufferLength];
            List<byte> fullMessage = new List<byte>();
            WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
            while (!receiveResult.EndOfMessage)
            {
                fullMessage.AddRange(buffer.Take(receiveResult.Count));
                buffer = new byte[bufferLength];
                receiveResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
            }
            fullMessage.AddRange(buffer.Take(receiveResult.Count));
            return fullMessage.ToArray();
        }
        private async void BeginReceiveAsync()
        {
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        byte[] receivedData = await ReceiveBytesAsync().ConfigureAwait(false);
                        if (IsEncrypted)
                        {
                            receivedData = Encryptor.SymmetricDataDecrypt(
                                receivedData, 
                                licensorSignPublicKey,
                                symmetricKey, 
                                NodeData.Instance.NodeKeys.Password).DecryptedData;
                        }
                        CommunicationObject communicationObject = ObjectSerializer.ByteArrayToObject<CommunicationObject>(receivedData);
                        if (communicationObject is LicensorResponse response)
                        {
                            Console.WriteLine($"Response received: {response.ResponseType}");
                            ResponseReceivedEvent.Invoke(response);
                        }
                        else if(communicationObject is LicensorNotice licensorNotice)
                        {
                            LicensorNoticeManager licensorNoticeManager = new LicensorNoticeManager();
                            await licensorNoticeManager.HandleNoticeAsync(licensorNotice).ConfigureAwait(false);
                        }
                    }                  
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex);
                        return;
                    }                    
                }
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        private async Task SendRequestAsync(LicensorRequest request)
        {
            Console.WriteLine($"Sending request: {request.RequestType}");
            byte[] requestBytes = ObjectSerializer.ObjectToByteArray(request);           
            if (IsEncrypted)
            {
                requestBytes = Encryptor.SymmetricDataEncrypt(
                    requestBytes, 
                    NodeData.Instance.NodeKeys.SignPrivateKey, 
                    symmetricKey, 
                    MessageDataType.Binary, 
                    NodeData.Instance.NodeKeys.Password);
            }
            await webSocket.SendAsync(requestBytes, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
        }
        private LicensorResponse ResponseReceivedHandler(LicensorResponse response)
        {
            if(responseTasks.TryGetValue(response.RequestId, out var taskCompletionSource))
            {
                taskCompletionSource.SetResult(response);
            }
            return response;
        }
        private async Task<LicensorResponse> GetResponseAsync(LicensorRequest request, int timeoutMilliseconds = 30 * 1000)
        {
            try
            {
                TaskCompletionSource<LicensorResponse> taskCompletionSource = new TaskCompletionSource<LicensorResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                responseTasks.Add(request.RequestId, taskCompletionSource);                
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeoutMilliseconds);                
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                var responseTask = taskCompletionSource.Task;
                var responseWaitTask = Task.Run(async () => await responseTask.ConfigureAwait(false), cancellationTokenSource.Token);
                while (responseWaitTask.Status == TaskStatus.Running)
                {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    await Task.Delay(100).ConfigureAwait(false);
                }
                var response = await responseWaitTask.ConfigureAwait(false);
                cancellationTokenSource.Dispose();
                return response;
            }
            catch (TaskCanceledException)
            {
                throw new ResponseException("Response timed out.");
            }            
        }        
        public async Task<LicenseVm> GetLicenseAsync(long? nodeId = null)
        {
            try
            {
                if (!await TryConnectAsync().ConfigureAwait(false))
                    throw new LicensorException("Unable to establish connection with licensor.");
                GetLicenseRequest request = new GetLicenseRequest(nodeId);
                await SendRequestAsync(request).ConfigureAwait(false);
                var response = await GetResponseAsync(request).ConfigureAwait(false);
                if (response is LicensesResponse licensesResponse)
                {
                    var license = licensesResponse.Licenses.FirstOrDefault();
                    return license;
                }
                else if (response is ResultResponse resultResponse)
                    throw new ResponseException($"Failed to edit node. Response error message: {resultResponse.ErrorMessage}");
                throw new ResponseException($"Unknown response type: {response.ResponseType}");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return null;
            }
        }
        public async Task<List<BlockVm>> GetBlockchainBlocksAsync(long startId, long endId)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] responseData = await webClient.DownloadDataTaskAsync($"https://{NodeSettings.Configs.LicensorUrl}/api/Blockchain/Download?startId={startId}&endId={endId}")
                    .ConfigureAwait(false);
                return ObjectSerializer.ByteArrayToObject<List<BlockVm>>(responseData);
            }
        }
        public async Task UpdateSessionKeyAsync()
        {
            byte[] oldKey = new byte[symmetricKey.Length];
            long oldExpired = expiredAt.GetValueOrDefault();
            Array.Copy(symmetricKey, oldKey, symmetricKey.Length);
            try
            {
                long keyId = RandomExtensions.NextInt64();
                uint ttl = (uint) TimeSpan.FromDays(7).TotalSeconds;
                long expired = DateTime.UtcNow.ToUnixTime() + ttl;
                var symmetricKey = Encryptor.GetSymmetricKey(256, keyId, ttl, NodeData.Instance.NodeKeys.Password);
                var encryptedKey = Encryptor.AsymmetricDataEncrypt(
                    symmetricKey, 
                    licensorPublicKey, 
                    NodeData.Instance.NodeKeys.SignPrivateKey, 
                    NodeData.Instance.NodeKeys.Password);
                var request = new UpdateSessionKeyRequest(encryptedKey, expired);
                await SendRequestAsync(request).ConfigureAwait(false);
                this.symmetricKey = symmetricKey;                
                expiredAt = expired; 
                await GetResponseAsync(request);                
            }          
            catch(Exception ex)
            {
                symmetricKey = oldKey;
                expiredAt = oldExpired;
                Console.WriteLine($"Update session key error: {ex.Message}");
                Logger.WriteLog(ex);
            }            
        }
    }
}