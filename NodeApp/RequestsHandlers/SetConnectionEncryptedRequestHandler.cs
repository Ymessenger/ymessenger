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
using NodeApp.CacheStorageClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class SetConnectionEncryptedRequestHandler : IRequestHandler
    {
        private readonly SetConnectionEncryptedRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IConnectionsService connectionsService;
        private readonly IKeysService keysService;
        private readonly INodeRequestSender nodeRequestSender;

        public SetConnectionEncryptedRequestHandler(
            Request request, ClientConnection clientConnection, IConnectionsService connectionsService,
            IKeysService keysService, INodeRequestSender nodeRequestSender)
        {
            this.request = (SetConnectionEncryptedRequest)request;
            this.clientConnection = clientConnection;
            this.connectionsService = connectionsService;
            this.keysService = keysService;
            this.nodeRequestSender = nodeRequestSender;
        }

        public async Task<Response> CreateResponseAsync()
        {
            if (request.NodeId == null || request.NodeId == NodeSettings.Configs.Node.Id)
            {
                byte[] symmetricKey = Encryptor.GetSymmetricKey(
                    256,
                    RandomExtensions.NextInt64(),
                    100000,
                    NodeData.Instance.NodeKeys.Password);
                byte[] publicKey;
                byte[] signPublicKey;
                if (request.UserId != null && request.PublicKeyId != null && request.SignPublicKeyId != null)
                {
                    var userKey = await keysService.GetUserKeyAsync(request.PublicKeyId.Value, request.UserId.Value, false).ConfigureAwait(false);
                    var signKey = await keysService.GetUserKeyAsync(request.SignPublicKeyId.Value, request.UserId.Value, true).ConfigureAwait(false);
                    if (userKey != null && signKey != null)
                    {
                        publicKey = userKey.Data;
                        signPublicKey = signKey.Data;
                        clientConnection.UserId = userKey.UserId;
                        connectionsService.AddOrUpdateUserConnection(userKey.UserId.Value, clientConnection);
                    }
                    else
                    {
                        return new ResultResponse(request.RequestId, "Key not found", ObjectsLibrary.Enums.ErrorCode.ObjectDoesNotExists);
                    }
                }
                else
                {
                    publicKey = request.PublicKey;
                    signPublicKey = request.SignPublicKey;
                }
                byte[] encryptedKey = Encryptor.AsymmetricDataEncrypt(
                    symmetricKey,
                    publicKey,
                    NodeData.Instance.NodeKeys.SignPrivateKey,
                    NodeData.Instance.NodeKeys.Password);
                clientConnection.SymmetricKey = symmetricKey;
                clientConnection.PublicKey = publicKey;
                clientConnection.SignPublicKey = signPublicKey;
                return new EncryptedKeyResponse(request.RequestId, encryptedKey);
            }
            else
            {
                await MetricsHelper.Instance.SetCrossNodeApiInvolvedAsync(request.RequestId).ConfigureAwait(false);
                var nodeConnection = connectionsService.GetNodeConnection(request.NodeId.Value);
                if (nodeConnection != null)
                {
                    clientConnection.UserId = request.UserId;
                    clientConnection.ProxyNodeWebSocket = nodeConnection.NodeWebSocket;
                    clientConnection.PublicKey = request.PublicKey;
                    clientConnection.SignPublicKey = request.SignPublicKey;
                    if(request.UserId != null)
                    {
                        connectionsService.AddOrUpdateUserConnection(request.UserId.GetValueOrDefault(), clientConnection);
                    }                    
                    var response = await nodeRequestSender.SendProxyUsersCommunicationsNodeRequestAsync(
                        ObjectSerializer.CommunicationObjectToBytes(request),
                        clientConnection.UserId.GetValueOrDefault(),
                        nodeConnection,
                        ObjectType.Request,
                        clientConnection.PublicKey,
                        clientConnection.SignPublicKey).ConfigureAwait(false);
                    return ObjectSerializer.BytesToResponse(response.CommunicationData);
                }
                return new ResultResponse(request.RequestId, "Internal server error.", ObjectsLibrary.Enums.ErrorCode.UnknownError);
            }
        }

        public bool IsRequestValid()
        {
            return (request.PublicKey != null && request.PublicKey.Any())
                || (request.PublicKeyId != null && request.UserId != null);
        }
    }
}