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
using System.Threading.Tasks;
using NodeApp.CrossNodeClasses.Requests;
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.Interfaces;
using NodeApp.Objects;

namespace NodeApp.CrossNodeClasses.NodeRequestHandlers
{
    public class GetPublicKeyNodeRequestHandler : ICommunicationHandler
    {
        private readonly GetPublicKeyNodeRequest request;
        private readonly NodeConnection current;
        private readonly IKeysService keysService;

        public GetPublicKeyNodeRequestHandler(NodeRequest request, NodeConnection current, IKeysService keysService)
        {
            this.request = (GetPublicKeyNodeRequest) request;
            this.current = current;
            this.keysService = keysService;
        }

        public async Task HandleAsync()
        {
            NodeResponse response;
            if(request.KeyId == null)
            {
                response = new PublicKeyNodeResponse(
                    request.RequestId,
                    NodeData.Instance.NodeKeys.PublicKey,
                    NodeData.Instance.NodeKeys.SignPublicKey,
                    NodeData.Instance.NodeKeys.KeyId,
                    NodeData.Instance.NodeKeys.ExpirationTime);
            }
            else
            {
                var key = await keysService.GetNodeKeysAsync(NodeSettings.Configs.Node.Id, request.KeyId.Value);
                if (key == null)
                {
                    response = new ResultNodeResponse(request.RequestId, ObjectsLibrary.Enums.ErrorCode.ObjectDoesNotExists, "The key was not found.");
                }
                else
                {
                    response = new PublicKeyNodeResponse(request.RequestId, key.PublicKey, key.SignPublicKey, key.KeyId, key.ExpirationTime);
                }
            }
            await NodeWebSocketCommunicationManager.SendResponseAsync(response, current).ConfigureAwait(false);
        }

        public bool IsObjectValid()
        {
            return true;
        }
    }
}