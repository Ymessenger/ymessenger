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
using NodeApp.Interfaces;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeRequestHandlers
{   
    public class ProxyUsersCommunicationsNodeRequestHandler : ICommunicationHandler
    {
        private readonly ProxyUsersCommunicationsNodeRequest request;
        private readonly NodeConnection current;
        private readonly ClientRequestService clientRequestService;        
        private readonly IAppServiceProvider serviceProvider;

        public ProxyUsersCommunicationsNodeRequestHandler(NodeRequest request, NodeConnection current, IAppServiceProvider serviceProvider)
        {
            this.request = (ProxyUsersCommunicationsNodeRequest)request;
            this.current = current;
            clientRequestService = new ClientRequestService(serviceProvider.NoticeService);
            this.serviceProvider = serviceProvider;           
        }

        public async Task HandleAsync()
        {
            switch (request.ObjectType)
            {
                case ObjectType.Request:
                    {
                        ClientConnection clientConnection = null;
                        CommunicationObject userObject;
                        var clientConnections = serviceProvider.ConnectionsService.GetUserClientConnections(request.UserId);
                        if (clientConnections != null)
                        {
                            clientConnection = clientConnections.FirstOrDefault(opt => opt.IsProxiedClientConnection && opt.ProxyNodeWebSocket != null);
                        }
                        if(clientConnection == null)
                        {
                            clientConnection = new ClientConnection(request.UserId, current.NodeWebSocket);
                            serviceProvider.ConnectionsService.AddOrUpdateUserConnection(request.UserId, clientConnection);
                        }
                        if (clientConnection.IsEncryptedConnection)
                        {
                            byte[] decryptedData = Encryptor.SymmetricDataDecrypt(
                                request.CommunicationData,
                                clientConnection.SignPublicKey,
                                clientConnection.SymmetricKey,
                                NodeData.Instance.NodeKeys.Password).DecryptedData;
                            userObject = ObjectSerializer.BytesToCommunicationObject(decryptedData);
                        }
                        else
                        {
                            userObject = ObjectSerializer.BytesToCommunicationObject(request.CommunicationData);
                        }
                        IRequestHandler requestHandler = ClientWebSocketRequestManager.InitRequestHandler(
                            (Request)userObject, 
                            clientConnection, 
                            clientRequestService,
                            serviceProvider);
                        Response response;
                        try
                        {
                            response = await requestHandler.CreateResponseAsync().ConfigureAwait(false);                            
                        }
                        catch
                        {
                            response = new ResultResponse(request.RequestId, null, ErrorCode.UnknownError);
                        }
                        SendResponse(response, clientConnection, current);
                    }
                    break;                            
            }
        }
        
        private void SendResponse(Response response, ClientConnection clientConnection, NodeConnection nodeConnection)
        {
            byte[] responseData = ObjectSerializer.CommunicationObjectToBytes(response);
            if (clientConnection.IsEncryptedConnection)
            {
                responseData = Encryptor.SymmetricDataEncrypt(
                    responseData,
                    NodeData.Instance.NodeKeys.SignPrivateKey,
                    clientConnection.SymmetricKey,
                    MessageDataType.Response,
                    NodeData.Instance.NodeKeys.Password);
            }
            ProxyUsersCommunicationsNodeResponse nodeResponse = new ProxyUsersCommunicationsNodeResponse(request.RequestId,responseData, request.UserId, request.UserPublicKey);
            NodeWebSocketCommunicationManager.SendResponse(nodeResponse, nodeConnection);
            if (response.ResponseType == ResponseType.EncryptedKey)
            {
                clientConnection.SentKey = true;
            }
        }   

        public bool IsObjectValid()
        {
            return current.Node != null && request.CommunicationData != null && request.CommunicationData.Any();
        }
    }
}