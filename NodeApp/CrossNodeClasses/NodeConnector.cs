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
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses
{
    public class NodeConnector
    {
        private readonly List<KeyValuePair<Uri, NodeVm>> destPoints;
        public static IConnectionsService ConnectionsService { get; set; }
        public NodeConnector(List<KeyValuePair<string,NodeVm>> urls)
        {
            destPoints = new List<KeyValuePair<Uri, NodeVm>>();
            var tempUri = new List<KeyValuePair<Uri, NodeVm>>();
            foreach (var url in urls)
            {
                var parsedUrl = url.Key.Contains("https://") ? url.Key : $"https://{url.Key}/";
                tempUri.Add(new KeyValuePair<Uri, NodeVm>(new Uri(parsedUrl), url.Value));
            }
            foreach (var uri in tempUri)
            {
                destPoints.Add(new KeyValuePair<Uri, NodeVm>(new Uri($"wss://{uri.Key.Host}:{uri.Key.Port}/"), uri.Value));
            }
        }

        public async Task<IEnumerable<NodeConnection>> ConnectToNodesAsync()
        {
            List<NodeConnection> connectedNodeList = new List<NodeConnection>();            
            List<Task> connectTasks = new List<Task>();
            foreach (var point in destPoints)
            {
                try
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    CancellationToken token = cancellationTokenSource.Token;                    
                    ClientWebSocket webSocket = new ClientWebSocket();
                    connectTasks.Add(webSocket.ConnectAsync(point.Key, token));
                    NodeConnection node = new NodeConnection
                    {
                        NodeWebSocket = webSocket,
                        Uri = point.Key,
                        PublicKey = point.Value.NodeKey.EncPublicKey,
                        SignPublicKey = point.Value.NodeKey.SignPublicKey
                    };
                    connectedNodeList.Add(node);                    
                }
                catch(Exception ex)
                {
                    Logger.WriteLog(ex);
                    continue;
                }
            }
            try
            {
                await Task.WhenAll(connectTasks).ConfigureAwait(false);                
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
            return connectedNodeList;
        }

        public void Listen(IEnumerable<NodeConnection> nodeConnections)
        {
            foreach (var node in nodeConnections)
            {
                NodeDataReceiver receiver = new NodeDataReceiver(node, AppServiceProvider.Instance);
                Task.Run(async () => await receiver.BeginReceiveAsync().ConfigureAwait(false));
            }
            Task.Delay(500).Wait();
        }
    }
}
