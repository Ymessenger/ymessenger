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
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeRequestHandlers
{
    public class GetInfoBlocksRequestHandler : ICommunicationHandler
    {
        private readonly GetInfoBlocksNodeRequest request;
        private readonly NodeConnection current;
        public GetInfoBlocksRequestHandler(CommunicationObject request, NodeConnection current)
        {
            this.request = (GetInfoBlocksNodeRequest)request;
            this.current = current;
        }
        public async Task HandleAsync()
        {
            try
            {
                BlockchainInfo info = await BlockchainReadService.GetBlockchainInformationAsync().ConfigureAwait(false);
                BlockchainInfoNodeResponse response = new BlockchainInfoNodeResponse(request.RequestId, info);
                NodeWebSocketCommunicationManager.SendResponse(response, current);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                NodeWebSocketCommunicationManager.SendResponse(new ResultNodeResponse(request.RequestId, ErrorCode.UnknownError), current);
            }
        }

        public bool IsObjectValid()
        {
            return true;
        }
    }
}