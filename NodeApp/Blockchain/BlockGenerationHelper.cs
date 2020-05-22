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
using ObjectsLibrary.Blockchain.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.Blockchain
{
    public class BlockGenerationHelper
    {
        private readonly IBlockGenerationHelper _blockGenerationHelper;
        public BlockGenerationHelper(IBlockGenerationHelper blockGenerationHelper)
        {
            _blockGenerationHelper = blockGenerationHelper;
        }

        public static BlockGenerationHelper Instance { get; set; }

        public void AddSegment(BlockSegmentVm segment)
        {
            _blockGenerationHelper.AddSegment(segment);
        }

        public void AddSegments(IEnumerable<BlockSegmentVm> segments)
        {
            _blockGenerationHelper.AddSegments(segments);
        }

        public async Task StopBlockGenerationAsync()
        {
            try
            {
                await _blockGenerationHelper.StopBlockGenerationAsync().ConfigureAwait(false);
            }
            catch
            {
                return;
            }
        }

        public void HandleNewBlock(BlockVm block)
        {
            _blockGenerationHelper.HandleNewBlock(block);
        }
    }
}
