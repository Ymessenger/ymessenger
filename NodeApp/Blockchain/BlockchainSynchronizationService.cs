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
using LinqKit;
using Microsoft.EntityFrameworkCore;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.LicensorClasses;
using Npgsql;
using ObjectsLibrary.Blockchain.Entities;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Builders;
using ObjectsLibrary.Converters;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NodeApp.Blockchain
{
    public class BlockchainSynchronizationService : IBlochainSynchronizationService
    {
        private readonly IKeysService keysService;
        public BlockchainSynchronizationService()
        {
            keysService = AppServiceProvider.Instance.KeysService;
        }
        
        public async Task CheckAndSyncBlockchainAsync()
        {
            var startBlockId = await CommonBlockchainPartSearchAsync().ConfigureAwait(false);
            BlockchainInfo blockchainInfo = await BlockchainReadService.GetBlockchainInformationAsync().ConfigureAwait(false);
            var webInfo = await LicensorClient.Instance.GetBlockchainInfoAsync().ConfigureAwait(false);
            if (startBlockId < blockchainInfo.Count)
            {
                await SaveAndRemoveOldBlocksAsync(startBlockId).ConfigureAwait(false);
                await DownloadBlockchainAsync(startBlockId).ConfigureAwait(false);
                await RestoreOldBlockchainDataAsync().ConfigureAwait(false);
            }
            else if (startBlockId < webInfo.Count)
            {
                await DownloadBlockchainAsync(startBlockId).ConfigureAwait(false);
            }
        }

        private async Task RestoreOldBlockchainDataAsync()
        {
            using (BlockchainDbContext context = new BlockchainDbContext())
            {
                long maxId = await context.ObsoleteBlocks.MaxAsync(opt => opt.Id).ConfigureAwait(false);
                long minId = await context.ObsoleteBlocks.MinAsync(opt => opt.Id).ConfigureAwait(false);
                byte limit = 100;
                for (long i = minId; i < maxId; i += limit)
                {
                    var obsoleteBlocks = await context.ObsoleteBlocks
                        .Where(block => block.Id >= i)
                        .Take(limit)
                        .ToListAsync().ConfigureAwait(false);
                    List<BlockVm> blocks = obsoleteBlocks.Select(block => ObjectSerializer.ByteArrayToObject<BlockVm>(block.Data)).ToList();
                    List<BlockSegmentVm> allSegments = new List<BlockSegmentVm>();
                    List<BlockSegmentVm> noexistentSegments = new List<BlockSegmentVm>();
                    foreach (var block in blocks)
                    {
                        allSegments.AddRange(block.BlockSegments);
                    }
                    var segmentsCondition = PredicateBuilder.New<BlockSegment>();
                    segmentsCondition = allSegments.Aggregate(segmentsCondition,
                        (current, value) => current.Or(opt => opt.SegmentHash.SequenceEqual(value.SegmentHash)).Expand());
                    var existentSegments = await context.BlockSegments.Where(segmentsCondition).ToListAsync().ConfigureAwait(false);
                    noexistentSegments = allSegments
                        .Where(segment => !existentSegments.Any(exSegment => segment.SegmentHash.SequenceEqual(exSegment.SegmentHash)))
                        ?.ToList();
                    if (!noexistentSegments.IsNullOrEmpty())
                    {
                        BlockGenerationHelper.Instance.AddSegments(noexistentSegments);
                    }
                }
                NpgsqlParameter blockIdParam = new NpgsqlParameter("blockId", minId);
                RawSqlString sqlString = new RawSqlString(@"DELETE FROM ""ObsoleteBlocks"" WHERE ""Id"" >= @blockId");
                await context.Database.ExecuteSqlCommandAsync(sqlString, blockIdParam).ConfigureAwait(false);
            }
        }

        private async Task DownloadBlockchainAsync(long startId)
        {
            var webInfo = await LicensorClient.Instance.GetBlockchainInfoAsync().ConfigureAwait(false);
            BlockchainDataRestorer dataRestorer = new BlockchainDataRestorer();
            try
            {
                BlockchainInfo ownInfo = await BlockchainReadService.GetBlockchainInformationAsync().ConfigureAwait(false);
                List<BlockVm> newBlocks = default;
                for (long i = startId; i < webInfo.Count; i += 100)
                {
                    if (webInfo.Count - ownInfo.Count > 100)
                    {
                        newBlocks = await LicensorClient.Instance.GetBlockchainBlocksAsync(i, i + 100).ConfigureAwait(false);
                    }
                    else
                    {
                        newBlocks = await LicensorClient.Instance.GetBlockchainBlocksAsync(ownInfo.Count, webInfo.Count).ConfigureAwait(false);
                    }
                    if (newBlocks != null)
                    {

                        foreach (BlockVm block in newBlocks)
                        {
                            using (BlockchainDbContext context = new BlockchainDbContext())
                            {
                                try
                                {
                                    await context.Blocks.AddAsync(BlockBuilder.GetBlock(block)).ConfigureAwait(false);
                                    await context.SaveChangesAsync().ConfigureAwait(false);
                                    await dataRestorer.SaveBlockDataAsync(block).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteLog(ex);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private async Task<long> CommonBlockchainPartSearchAsync()
        {
            using (WebClient webClient = new WebClient())
            {
                BlockchainInfo blockchainInfo = await BlockchainReadService.GetBlockchainInformationAsync().ConfigureAwait(false);
                long blockId = -1;
                long tempBlockId = blockchainInfo.BlockHashes.Min(opt => opt.FirstValue);
                byte[] responseData = await webClient.UploadDataTaskAsync(
                       $"https://{NodeSettings.Configs.LicensorUrl}/api/Blockchain/FindMatch", ObjectSerializer.ObjectToByteArray(blockchainInfo)).ConfigureAwait(false);
                blockId = BitConverter.ToInt64(responseData);
                while (blockId == -1)
                {
                    blockchainInfo = await BlockchainReadService.GetBlockchainInformationAsync(tempBlockId).ConfigureAwait(false);
                    if (!blockchainInfo.BlockHashes.Any())
                    {
                        return 1;
                    }
                    if (blockchainInfo.BlockHashes.Min(opt => opt.FirstValue) == 1)
                        return 1;
                    responseData = await webClient.UploadDataTaskAsync(
                       $"https://{NodeSettings.Configs.LicensorUrl}/api/Blockchain/FindMatch", ObjectSerializer.ObjectToByteArray(blockchainInfo)).ConfigureAwait(false);
                    blockId = BitConverter.ToInt64(responseData);
                    tempBlockId = blockchainInfo.BlockHashes.Min(opt => opt.FirstValue);
                }
                if (blockId != -1)
                {
                    return blockId;
                }
                return 1;
            }
        }

        private async Task SaveAndRemoveOldBlocksAsync(long blockId)
        {
            using (BlockchainDbContext context = new BlockchainDbContext())
            {
                long maxId = await context.Blocks.MaxAsync(opt => opt.Id).ConfigureAwait(false);
                byte limit = 100;
                if (maxId < blockId)
                    return;
                for (long i = blockId; i < maxId; i += limit)
                {
                    var blocks = await context.Blocks
                        .Include(opt => opt.BlockSegments)
                        .ThenInclude(opt => opt.SegmentHeader)
                        .Include(opt => opt.Header)
                        .Where(opt => opt.Id > i)
                        .Take(limit)
                        .ToListAsync().ConfigureAwait(false);
                    var obsoleteBlocks = blocks.Select(opt => new ObsoleteBlock
                    {
                        Id = opt.Id,
                        Data = ObjectSerializer.ObjectToByteArray(BlockBuilder.GetBlockVm(opt))
                    });
                    await context.ObsoleteBlocks.AddRangeAsync(obsoleteBlocks).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
                NpgsqlParameter blockIdParam = new NpgsqlParameter("blockId", blockId);
                RawSqlString sqlString = new RawSqlString(@"DELETE FROM ""Blocks"" WHERE ""Id"" > @blockId");
                await context.Database.ExecuteSqlCommandAsync(sqlString, blockIdParam).ConfigureAwait(false);
            }
        }
    }
    public interface IBlochainSynchronizationService
    {
        Task CheckAndSyncBlockchainAsync();
    }
}
