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
using NodeApp.Extensions;
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;

namespace NodeApp
{
    public class ConnectionsService : IConnectionsService
    {
        private readonly ConcurrentDictionary<long, NodeConnection> _nodes;
        private readonly ConcurrentDictionary<long, List<ClientConnection>> _clients;
        public ConnectionsService()
        {
            _nodes = new ConcurrentDictionary<long, NodeConnection>();
            _clients = new ConcurrentDictionary<long, List<ClientConnection>>();
        }

        public void AddOrUpdateUserConnection(long userId, ClientConnection clientConnection)
        {
            _clients.AddOrUpdate(userId,
                new List<ClientConnection>
                {
                    clientConnection
                },
                (id, oldConnections) =>
                {
                    oldConnections.Add(clientConnection);
                    return oldConnections;
                });
        }
        public List<ClientConnection> GetUserClientConnections(long userId)
        {
            if (_clients.TryGetValue(userId, out var clients))
            {
                return clients?.Where(connection => connection.ClientSocket?.State == WebSocketState.Open
                || connection.IsProxiedClientConnection)?.ToList();
            }

            return null;
        }
        public void RemoveClientConnection(ClientConnection clientConnection)
        {
            var clientConnections = _clients.FirstOrDefault(opt => opt.Value.Contains(clientConnection));
            clientConnections.Value.Remove(clientConnection);
        }
        public void RemoveAllUserClientConnections(long userId)
        {
            _clients.TryRemove(userId, out _);
        }
        public List<ClientConnection> GetClientConnections(IEnumerable<long> usersIds)
        {
            if (usersIds == null)
            {
                throw new ArgumentNullException(nameof(usersIds));
            }

            List<ClientConnection> clientConnections = new List<ClientConnection>();
            foreach (long userId in usersIds)
            {
                if (_clients.TryGetValue(userId, out var connections))
                {
                    clientConnections.AddRange(connections);
                }
            }
            return clientConnections;
        }
        public List<ClientConnection> GetClientConnections()
        {
            List<ClientConnection> result = new List<ClientConnection>();
            foreach (var client in _clients)
            {
                result.AddRange(client.Value);
            }
            return result;
        }
        public void AddOrUpdateNodeConnection(long nodeId, NodeConnection nodeConnection)
        {
            _nodes.AddOrUpdate(nodeId, nodeConnection,
                   (key, existingNodeConnection) =>
                   {
                       return nodeConnection;
                   });
        }
        public NodeConnection GetNodeConnection(long nodeId)
        {
            var nodeConnection = _nodes
                .FirstOrDefault(opt => opt.Value?.NodeWebSocket?.State == WebSocketState.Open && opt.Key == nodeId).Value;
            return nodeConnection;
        }
        public void RemoveNodeConnection(NodeConnection nodeConnection)
        {
            _nodes.TryRemove(nodeConnection.Node.Id, out _);
        }
        public List<NodeConnection> GetNodeConnections()
        {
            return _nodes.Values.ToList();
        }
        public List<NodeConnection> GetNodeConnections(IEnumerable<long> nodesIds)
        {
            if (nodesIds == null)
            {
                throw new ArgumentNullException(nameof(nodesIds));
            }

            List<NodeConnection> nodeConnections = new List<NodeConnection>();
            foreach (long nodeId in nodesIds)
            {
                if (_nodes.TryGetValue(nodeId, out var nodeConnection))
                {
                    nodeConnections.Add(nodeConnection);
                }
            }
            return nodeConnections;
        }
        public void RestartNodeConnections(List<long> nodesIds = null)
        {
            List<NodeConnection> nodeConnections = new List<NodeConnection>();
            if(!nodesIds.IsNullOrEmpty())
            {
                foreach (long nodeId in nodesIds)
                {
                    if (_nodes.TryGetValue(nodeId, out var nodeConnection))
                    {
                        nodeConnections.Add(nodeConnection);
                    }
                }
            }
            else
            {
                nodeConnections.AddRange(_nodes.Values.ToList());
            }
            foreach(var connection in nodeConnections)
            {
                connection.NodeWebSocket.Abort();
            }            
        }
        public void CloseAllClientConnections()
        {
            foreach (var connections in _clients)
            {
                foreach (var clientConnection in connections.Value)
                {
                    if (clientConnection.ClientSocket != null)
                    {
                        clientConnection.ClientSocket.Abort();
                    }
                    clientConnection.ProxyNodeWebSocket = null;
                }
            }
            _clients.Clear();
        }
    }
}