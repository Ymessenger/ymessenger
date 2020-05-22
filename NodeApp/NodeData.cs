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
using NodeApp.Helpers;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.LicensorClasses;
using NodeApp.MessengerData.DataTransferObjects;
using ObjectsLibrary;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp
{
    public class NodeData
    {                     
        public static IConnectionsService ConnectionsService { get; set; }
        public static INodeNoticeService NodeNoticeService { get; set; }
        public NodeKeysDto NodeKeys { get; private set; }
        public List<Guid> RoutedMessagesId { get; private set; }
        private NodeData()
        {
            RoutedMessagesId = new List<Guid>();
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
                            nodeKeys = await AppServiceProvider.Instance.KeysService.CreateNewNodeKeysAsync(NodeSettings.Configs.Node.Id, HttpServer.Models.CreateNewKeysModel.KeyLength.Long, 10 * 24 * 3600).ConfigureAwait(false);
                            if (NodeKeys == null)
                            {
                                NodeKeys = nodeKeys;
                            }

                            await NodeNoticeService.SendNewNodeKeysNodeNoticeAsync(nodeKeys.PublicKey, nodeKeys.SignPublicKey, nodeKeys.KeyId, nodeKeys.ExpirationTime).ConfigureAwait(false);
                            await LicensorClient.Instance.AddNewKeyAsync(nodeKeys.PublicKey, nodeKeys.SignPublicKey, nodeKeys.KeyId, nodeKeys.ExpirationTime, nodeKeys.GenerationTime, true).ConfigureAwait(false);
                        }
                        NodeKeys = nodeKeys;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex);
                    }
                    finally
                    {
                        await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
                    }
                }
            }).Wait(TimeSpan.FromSeconds(5));
            TasksHelper.StartRemoveExpiredMessagesTask();
        }
        public NodeKeysDto PublicKeys => new NodeKeysDto
        {
            KeyId = NodeKeys.KeyId,
            ExpirationTime = NodeKeys.ExpirationTime,
            GenerationTime = NodeKeys.GenerationTime,
            NodeId = NodeKeys.NodeId,
            PublicKey = NodeKeys.PublicKey,
            SignPublicKey = NodeKeys.SignPublicKey
        };

        private static readonly Lazy<NodeData> singleton = new Lazy<NodeData>(() => new NodeData());
        public void SetNodeKeys(NodeKeysDto nodeKeys)
        {
            NodeKeys = nodeKeys ?? throw new ArgumentNullException(nameof(nodeKeys));
        }
        public static NodeData Instance => singleton.Value;
    }
}
