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
using NodeApp.CacheStorageClasses;
using ObjectsLibrary.Blockchain.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.Blockchain
{
    public class BlockSegmentsCacheService
    {
        private const string LIST_BLOCK_SEGMENTS_KEY = "LSEGMENTS";
        private readonly ICacheRepository<BlockSegmentVm> cacheRepository;

        private static readonly Lazy<BlockSegmentsCacheService> lazy = new Lazy<BlockSegmentsCacheService>(() => new BlockSegmentsCacheService());

        private BlockSegmentsCacheService()
        {
            cacheRepository = new RedisBlockSegmentRepository(NodeSettings.Configs.CacheServerConnection);
        }
        public static BlockSegmentsCacheService Instance => lazy.Value;

        public async void AddBlockSegment(BlockSegmentVm blockSegment)
        {
            try
            {
                await cacheRepository.AddObject(LIST_BLOCK_SEGMENTS_KEY, blockSegment).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async void AddBlockSegments(IEnumerable<BlockSegmentVm> blockSegments)
        {
            try
            {
                await cacheRepository.AddObjects(LIST_BLOCK_SEGMENTS_KEY, blockSegments).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async Task<IEnumerable<BlockSegmentVm>> GetBlockSegments(long length)
        {
            return await cacheRepository.GetObjects(LIST_BLOCK_SEGMENTS_KEY, length).ConfigureAwait(false);
        }

        public async Task<long> GetBlockSegmentsCount()
        {
            return await cacheRepository.GetCount(LIST_BLOCK_SEGMENTS_KEY).ConfigureAwait(false);
        }
    }
}
