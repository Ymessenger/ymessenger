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
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetInformationNodeRequestHandler : IRequestHandler
    {
        private readonly GetInformationNodeRequest request;
        private readonly INodesService nodesService;
        public GetInformationNodeRequestHandler(Request request, INodesService nodesService)
        {
            this.request = (GetInformationNodeRequest)request;
            this.nodesService = nodesService;
        }
        public async Task<Response> CreateResponseAsync()
        {
            if (request.NodesId == null || !request.NodesId.Any())
            {
                NodeVm node = await nodesService.GetAllNodeInfoAsync(NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                return new NodesResponse(request.RequestId, node);
            }
            else
            {
                List<NodeVm> nodes = await nodesService.GetNodesAsync(request.NodesId).ConfigureAwait(false);
                return new NodesResponse(request.RequestId, nodes);
            }
        }

        public bool IsRequestValid()
        {
            if (request.NodesId != null && !request.NodesId.Any())
            {
                return false;
            }

            return true;
        }
    }
}