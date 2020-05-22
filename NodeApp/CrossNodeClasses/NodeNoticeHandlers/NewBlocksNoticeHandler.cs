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
using NodeApp.Blockchain;
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.Interfaces;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.Services;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class NewBlocksNoticeHandler : ICommunicationHandler
    {
        private readonly BlocksNodeNotice notice;
        private readonly NodeConnection nodeConnection;
        private readonly BlockchainDataRestorer dataRestorer;
        private readonly IKeysService keysService;
        public NewBlocksNoticeHandler(CommunicationObject @object, NodeConnection nodeConnection, IKeysService keysService)
        {
            notice = (BlocksNodeNotice)@object;
            this.nodeConnection = nodeConnection;
            dataRestorer = new BlockchainDataRestorer();
            this.keysService = keysService;
        }
        public async Task HandleAsync()
        {
            var nodeKey = await keysService.GetNodeKeysAsync(notice.Block.NodeId, notice.Block.Header.SignKeyId.GetValueOrDefault()).ConfigureAwait(false);
            if (await BlocksService.AcceptOrRejectNewBlockAsync(notice.Block, nodeKey?.SignPublicKey, NodeData.Instance.NodeKeys.Password).ConfigureAwait(false))
            {
                BlockGenerationHelper.Instance.HandleNewBlock(notice.Block);
                await dataRestorer.SaveBlockDataAsync(notice.Block).ConfigureAwait(false);
            }
        }

        public bool IsObjectValid()
        {
            return notice.Block?.BlockSegments != null && notice.Block.BlockSegments.Any() && nodeConnection.Node != null;
        }
    }
}