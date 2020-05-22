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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodeApp.HttpServer.Models;
using NodeApp.Interfaces;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Converters;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers
{
    public class BlockchainController : Controller
    {
        private const int BLOCKS_LIMIT = 25;
        private readonly IBlockchainDataRestorer _blockchainDataRestorer;
        public BlockchainController(IBlockchainDataRestorer blockchainDataRestorer)
        {
            _blockchainDataRestorer = blockchainDataRestorer;
        }
        public async Task<ActionResult> Download([FromQuery]long startId, [FromQuery]long endId)
        {
            if (startId > endId || startId < 0 || endId < 0 || (endId - startId) > 100)
            {
                return BadRequest();
            }
            List<BlockVm> blocks = new List<BlockVm>(await BlockchainReadService.GetBlocksAsync(startId, endId).ConfigureAwait(false));
            var bytes = ObjectSerializer.ObjectToByteArray(blocks);
            return File(bytes, "application/octet-stream");
        }
        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Restore()
        {
            var firstBlock = (await BlockchainReadService.GetBlocksAsync(1, 1).ConfigureAwait(false)).FirstOrDefault();
            var lastBlock = await BlockchainReadService.GetLastBlockAsync().ConfigureAwait(false);
            return PartialView(new BlockchainRestoreModel
            {
                Start = firstBlock.Header.CreationTime.ToDateTime(),
                End = lastBlock.Header.CreationTime.ToDateTime()
            });
        }
        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Restore([FromForm]BlockchainRestoreModel model)
        {
            BlockchainInfo blockchainInfo = await BlockchainReadService.GetBlockchainInformationAsync().ConfigureAwait(false);
            if (model.Start == null)
            {
                model.Start = blockchainInfo.FirstBlockTime;
            }
            if (model.End == null)
            {
                model.End = blockchainInfo.LastBlockTime;
            }
            if (model.End < model.Start)
            {
                return BadRequest();
            }

            List<BlockVm> blocks = await BlockchainReadService.GetBlocksAsync(
                model.Start.GetValueOrDefault().ToUnixTime(),
                model.End.GetValueOrDefault().ToUnixTime(),
                BLOCKS_LIMIT).ConfigureAwait(false);
            while (blocks.Count == BLOCKS_LIMIT
                && blocks.LastOrDefault().Header.CreationTime < model.End.GetValueOrDefault().ToUnixTime())
            {
                foreach (var block in blocks)
                {
                    await _blockchainDataRestorer.SaveBlockDataAsync(block).ConfigureAwait(false);
                }
                blocks = await BlockchainReadService.GetBlocksAsync(
                    blocks.LastOrDefault().Header.CreationTime,
                    null,
                    BLOCKS_LIMIT).ConfigureAwait(false);
            }
            return RedirectToAction("Index", "Configuration");
        }
    }
}