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
using Microsoft.EntityFrameworkCore;
using NodeApp.LicensorClasses;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static NodeApp.HttpServer.Models.CreateNewKeysModel;

namespace NodeApp.Helpers
{
    public static class TasksHelper
    {
        public static void StartCheckKeysTask(NodeKeysDto paramNodeKeys, KeyLength keyLength, uint lifeTime)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        NodeKeysDto nodeKeys = await AppServiceProvider.Instance.KeysService.GetActualNodeKeysAsync(NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                        if (nodeKeys == null ||
                            (nodeKeys.ExpirationTime - DateTime.UtcNow.ToUnixTime()) <= TimeSpan.FromDays(5).TotalSeconds)
                        {
                            nodeKeys = await AppServiceProvider.Instance.KeysService.CreateNewNodeKeysAsync(NodeSettings.Configs.Node.Id, keyLength, lifeTime).ConfigureAwait(false);
                            if (paramNodeKeys == null)
                            {
                                paramNodeKeys = nodeKeys;
                            }

                            Console.WriteLine($"New keys was generated. KeyId: {nodeKeys.KeyId}");
                            await NodeData.NodeNoticeService.SendNewNodeKeysNodeNoticeAsync(nodeKeys.PublicKey, nodeKeys.SignPublicKey, nodeKeys.KeyId, nodeKeys.ExpirationTime).ConfigureAwait(false);
                            await LicensorClient.Instance.AddNewKeyAsync(nodeKeys.PublicKey, nodeKeys.SignPublicKey, nodeKeys.KeyId, nodeKeys.ExpirationTime, nodeKeys.GenerationTime, true).ConfigureAwait(false);
                        }                        
                        paramNodeKeys = nodeKeys;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex);
                    }
                    finally
                    {
                        await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(true);
                    }
                }
            }).Wait(TimeSpan.FromSeconds(5));
        }

        public static void StartNodeConnectionsCheckTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        List<NodeVm> nodes = new List<NodeVm>(await AppServiceProvider.Instance.NodesService.GetAllNodesInfoAsync().ConfigureAwait(false));
                        nodes = nodes.Where(opt => opt.Id != NodeSettings.Configs.Node.Id).ToList();
                        foreach (var node in nodes)
                        {                            
                            try
                            {
                                var nodeConnection = NodeData.ConnectionsService.GetNodeConnection(node.Id);
                                if (nodeConnection == null)
                                {
                                    if (string.IsNullOrWhiteSpace(node.Domains?.FirstOrDefault()))
                                    {
                                        throw new ArgumentNullException("Node domains not found.");
                                    }
                                    ClientWebSocket webSocket = new ClientWebSocket();                                    
                                    Uri newNodeUri = new Uri($"wss://{node.Domains.FirstOrDefault()}:{node.NodesPort}/");                                    
                                    await webSocket.ConnectAsync(newNodeUri, CancellationToken.None).ConfigureAwait(false);                                    
                                    NodeConnection newNodeConnection = new NodeConnection
                                    {
                                        Node = node,
                                        NodeWebSocket = webSocket,
                                        Uri = newNodeUri
                                    };
                                    NodeDataReceiver nodeDataReceiver = new NodeDataReceiver(newNodeConnection, AppServiceProvider.Instance);
                                    Task receiveDataTask = new Task(async () => await nodeDataReceiver.BeginReceiveAsync().ConfigureAwait(false));
                                    receiveDataTask.Start();
                                    AppServiceProvider.Instance.NodeRequestSender.SendConnectRequestAsync(newNodeConnection);                                    
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(ex);
                                continue;
                            }                            
                        }
                        nodes.Clear();
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex);
                    }
                    finally
                    {
                        await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(true);
                    }
                }
            });
        }
        public static void StartRemoveExpiredMessagesTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    using (MessengerDbContext context = new MessengerDbContext())
                    {
                        var expiredMessages = await context.Messages
                            .Where(message => message.ExpiredAt <= DateTime.UtcNow.ToUnixTime() && message.ExpiredAt != null)
                            .ToListAsync().ConfigureAwait(false);
                        context.RemoveRange(expiredMessages);
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        UsersConversationsCacheService.Instance.MessagesRemovedUpdateConversationsAsync(expiredMessages);
                    }
                    await AppServiceProvider.Instance.PendingMessagesService.RemoveExpiredAsync().ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(true);
                }
            });
        }
        public static void StartUpdateLicenseTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        NodeSettings.Configs.License = await LicensorClient.Instance.GetLicenseAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex);
                    }
                    finally
                    {
                        await Task.Delay(TimeSpan.FromHours(1)).ConfigureAwait(true);
                    }
                }
            });
        }

        public static void StartUpdateSessionKeyTask()
        {
            Task.Run(async () => 
            {
                await Task.Delay(TimeSpan.FromHours(5)).ConfigureAwait(true);
                while (true)
                {
                    try
                    {                        
                        await LicensorClient.Instance.UpdateSessionKeyAsync();
                    }
                    finally
                    {
                        await Task.Delay(TimeSpan.FromHours(5)).ConfigureAwait(true);
                    }
                }
            });
        }
    }
}
