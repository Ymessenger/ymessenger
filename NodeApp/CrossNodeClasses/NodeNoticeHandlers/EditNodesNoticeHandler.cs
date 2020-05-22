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
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class EditNodesNoticeHandler : ICommunicationHandler
    {
        private readonly NewOrEditNodesNodeNotice notice;
        private readonly NodeConnection nodeConnection;
        private readonly ICrossNodeService crossNodeService;
        public EditNodesNoticeHandler(CommunicationObject @object, NodeConnection nodeConnection, ICrossNodeService crossNodeService)
        {
            notice = (NewOrEditNodesNodeNotice)@object;
            this.nodeConnection = nodeConnection;
            this.crossNodeService = crossNodeService;
        }
        public async Task HandleAsync()
        {
            foreach (var node in notice.Nodes)
            {
                await crossNodeService.NewOrEditNodeAsync(node).ConfigureAwait(false);
            }
        }

        public bool IsObjectValid()
        {
            if (notice.Nodes == null || !notice.Nodes.Any() || nodeConnection.Node == null)
            {
                return false;
            }

            foreach (var node in notice.Nodes)
            {
                if (node.Id == 0 || string.IsNullOrWhiteSpace(node.Name))
                {
                    return false;
                }
            }
            return true;
        }
    }
}