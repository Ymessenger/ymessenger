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
using System.Collections.Generic;
using NodeApp.Objects;

namespace NodeApp.Interfaces.Services
{
    public interface IConnectionsService
    {
        void AddOrUpdateNodeConnection(long nodeId, NodeConnection nodeConnection);
        void AddOrUpdateUserConnection(long userId, ClientConnection clientConnection);
        void CloseAllClientConnections();
        List<ClientConnection> GetClientConnections();
        List<ClientConnection> GetClientConnections(IEnumerable<long> usersIds);
        NodeConnection GetNodeConnection(long nodeId);
        List<NodeConnection> GetNodeConnections();
        List<NodeConnection> GetNodeConnections(IEnumerable<long> nodesIds);
        List<ClientConnection> GetUserClientConnections(long userId);
        void RemoveAllUserClientConnections(long userId);
        void RemoveClientConnection(ClientConnection clientConnection);
        void RemoveNodeConnection(NodeConnection nodeConnection);
        void RestartNodeConnections(List<long> nodesIds = null);
    }
}