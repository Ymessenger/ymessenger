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
using NodeApp.CrossNodeClasses.Requests;
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.LicensorClasses;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeRequestHandlers
{
    public class ConnectRequestHandler : ICommunicationHandler
    {
        private readonly ConnectNodeRequest request;
        private readonly NodeConnection nodeConnection;
        private readonly IConnectionsService connectionsService;
        private readonly INodesService nodesService;        
        private readonly INodeNoticeService nodeNoticeService;
        public ConnectRequestHandler(
            CommunicationObject request,
            NodeConnection node,
            IConnectionsService connectionsService,
            INodesService nodesService,           
            INodeNoticeService nodeNoticeService)
        {
            this.request = (ConnectNodeRequest)request;
            this.connectionsService = connectionsService;
            this.nodesService = nodesService;            
            this.nodeNoticeService = nodeNoticeService;
            nodeConnection = node;
        }
        public async Task HandleAsync()
        {
            try
            {
                var nodeKey = request.Keys;     
                ConnectData connectData = request.GetConnectData(nodeKey.SignPublicKey, NodeData.Instance.NodeKeys.Password, NodeData.Instance.NodeKeys.PrivateKey);       
                nodeConnection.Node = connectData.Node;
                nodeConnection.Node.StartDay = nodeConnection.Node.StartDay.ToUniversalTime();
                var nodeJson = ObjectSerializer.ObjectToJson(nodeConnection.Node);
                bool isValid = Encryptor.CheckSign(
                    connectData.LicensorSign, 
                    LicensorClient.Instance.GetSignPublicKey(), 
                    Encoding.UTF8.GetBytes(nodeJson), 
                    NodeData.Instance.NodeKeys.Password);
                if (!isValid)  
                    NodeWebSocketCommunicationManager.SendResponse(
                        new ResultNodeResponse(request.RequestId, ErrorCode.AuthorizationProblem, "Wrong sign for node data."), nodeConnection);
                LicenseVm license = connectData.License;
                long currentTime = DateTime.UtcNow.ToUnixTime();
                if (!license.IsLicenseValid(currentTime, LicensorClient.Instance.GetSignPublicKey(), NodeData.Instance.NodeKeys.Password, out _))
                {
                    var licenseFromLicensor = await LicensorClient.Instance.GetLicenseAsync(nodeConnection.Node.Id).ConfigureAwait(false);
                    if (!licenseFromLicensor.IsLicenseValid(currentTime, LicensorClient.Instance.GetSignPublicKey(), NodeData.Instance.NodeKeys.Password, out var errorMessage))
                    {
                        var isBlockchainLicenseValid = await BlockchainReadService.IsNodeLicenseValidAsync(nodeConnection.Node.Id, currentTime).ConfigureAwait(false);
                        if(!isBlockchainLicenseValid)
                            NodeWebSocketCommunicationManager.SendResponse(
                                new ResultNodeResponse(request.RequestId, ErrorCode.AuthorizationProblem, errorMessage), nodeConnection);
                    }
                }
                nodeConnection.Uri = new Uri($"wss://{nodeConnection.Node.Domains.FirstOrDefault()}:{nodeConnection.Node.NodesPort}");
                ConnectData responseConnectData = new ConnectData 
                { 
                    Node = NodeSettings.Configs.Node,
                    LicensorSign = NodeSettings.Configs.LicensorSign.Sign,
                    License = NodeSettings.Configs.License                    
                };
                byte[] encryptedData = Encryptor.SymmetricDataEncrypt(
                    ObjectSerializer.ObjectToByteArray(responseConnectData),
                    NodeData.Instance.NodeKeys.SignPrivateKey,
                    connectData.SymmetricKey,
                    MessageDataType.Binary,
                    NodeData.Instance.NodeKeys.Password);                
                await NodeWebSocketCommunicationManager.SendResponseAsync(new NodesInformationResponse(request.RequestId, encryptedData), nodeConnection).ConfigureAwait(false);
                nodeConnection.PublicKey = nodeKey.PublicKey;
                nodeConnection.SymmetricKey = connectData.SymmetricKey;
                nodeConnection.Node.NodeKey.EncPublicKey = nodeKey.PublicKey;
                nodeConnection.PublicKeyExpirationTime = nodeKey.ExpirationTime;
                nodeConnection.PublicKeyId = nodeKey.KeyId;
                nodeConnection.SignPublicKey = nodeKey.SignPublicKey;
                nodesService.CreateOrUpdateNodeInformationAsync(nodeConnection.Node);
                connectionsService.AddOrUpdateNodeConnection(nodeConnection.Node.Id, nodeConnection);
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                await nodeNoticeService.SendPendingMessagesAsync(nodeConnection.Node.Id).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                NodeWebSocketCommunicationManager.SendResponse(new ResultNodeResponse(request.RequestId, ErrorCode.UnknownError, ex.Message), nodeConnection);
            }
        }

        public bool IsObjectValid()
        {
            return !request.Data.IsNullOrEmpty()
                && request.EncryptedKey != null && request.EncryptedKey.Any();
        }       
    }
}