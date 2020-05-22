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
using NodeApp.CrossNodeClasses;
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace NodeApp
{
    public class NodeDataReceiver : DataReceiver
    {
        private readonly NodeConnection currentNode;        
        internal static ConcurrentDictionary<long, TaskCompletionSource<NodeResponse>> ResponseTasks = new ConcurrentDictionary<long, TaskCompletionSource<NodeResponse>>();
        private readonly IConnectionsService connectionsService;

        public NodeDataReceiver(NodeConnection nodeConnection, IAppServiceProvider serviceProvider) : base(serviceProvider)
        {
            socket = nodeConnection.NodeWebSocket;
            currentNode = nodeConnection;
            connectionsService = serviceProvider.ConnectionsService;
        }

        public override async Task BeginReceiveAsync()
        {
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        byte[] receivedData = await ReceiveBytesAsync(16 * 1024, int.MaxValue).ConfigureAwait(true);
                        NodeWebSocketCommunicationManager manager = new NodeWebSocketCommunicationManager(_appServiceProvider);
                        manager.Handle(receivedData, currentNode);
                    }
                    catch (WebSocketException)
                    {
                        currentNode.PublicKey = null;
                        currentNode.SymmetricKey = null;
                        if (currentNode.Node != null)
                        {
                            connectionsService.RemoveNodeConnection(currentNode);
                        }

                        return;
                    }
                    catch (TooLargeReceivedDataException ex)
                    {
                        Logger.WriteLog(ex);
                    }
                    catch (Exception ex)
                    {                        
                        Logger.WriteLog(ex);
                    }
                }
            }
            catch (WebSocketException)
            {
                return;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
    }
}
