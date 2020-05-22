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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.LicensorClasses;
using ObjectsLibrary.Blockchain;
using ObjectsLibrary.Blockchain.Entities;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Builders;
using ObjectsLibrary.Encryption;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace NodeApp.Blockchain
{
    public class AppBlockGenerationHelper : IBlockGenerationHelper
    {
        private Task<BlockVm> blockGenerationTask;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken blockGenerationCancellToken;
        private readonly BlockSegmentsCacheService cacheService;
        private IEnumerable<BlockSegmentVm> currentSegments;
        private const int MAX_BLOCK_SEGMENTS_COUNT = 20;
        public INodeNoticeService NodeNoticeService
        {
            get
            {
                return AppServiceProvider.Instance.NodeNoticeService;
            }
        }

        public AppBlockGenerationHelper()
        {
            cacheService = BlockSegmentsCacheService.Instance;
            cancellationTokenSource = new CancellationTokenSource();
            blockGenerationCancellToken = cancellationTokenSource.Token;
            var periodicQueueCheckTask = new Task(async () =>
            {
                while (true)
                {
                    try
                    {
                        await CheckQueueAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex);
                    }
                    await Task.Delay(30 * 1000).ConfigureAwait(false);
                }
            });
            periodicQueueCheckTask.Start();
        }

        public void AddSegment(BlockSegmentVm segment)
        {
            if (segment.SegmentHeader.BlockSegmentType == BlockSegmentType.EditUser)
            {
                var segments = cacheService.GetBlockSegments(MAX_BLOCK_SEGMENTS_COUNT).Result;
                segments = segments.Where(opt => opt.SegmentHeader.ObjectId != segment.SegmentHeader.ObjectId
                    && opt.SegmentHeader.BlockSegmentType != segment.SegmentHeader.BlockSegmentType);
                segments = segments?.Append(segment) ?? new List<BlockSegmentVm> { segment };
                cacheService.AddBlockSegments(segments);
            }
            else
            {
                cacheService.AddBlockSegment(segment);
            }
        }

        public void AddSegments(IEnumerable<BlockSegmentVm> segments)
        {
            cacheService.AddBlockSegments(segments);
        }

        private async Task CheckQueueAsync()
        {
            long segmentsCount = await cacheService.GetBlockSegmentsCount().ConfigureAwait(false);
            if (segmentsCount > 0)
            {
                await BeginBlockGenerationAsync().ConfigureAwait(false);
            }
        }

        private async Task BeginBlockGenerationAsync()
        {
            if (blockGenerationTask == null || blockGenerationTask.IsCompleted)
            {
                IEnumerable<BlockSegmentVm> blockSegments;
                List<BlockSegmentVm> resultBlockSegments = new List<BlockSegmentVm>();
                blockSegments = await cacheService.GetBlockSegments(MAX_BLOCK_SEGMENTS_COUNT).ConfigureAwait(false);
                resultBlockSegments.AddRange(await BlockSegmentsService.Instance.GetNonExistingSemgentsAsync(blockSegments).ConfigureAwait(false));
                var block = await BlocksService.FormFullBlockAsync(resultBlockSegments, NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                currentSegments = resultBlockSegments;
                blockGenerationTask = Task<BlockVm>.Factory.StartNew(GenerateBlock, block, blockGenerationCancellToken);
                try
                {
                    BlockVm generatedBlock = await blockGenerationTask.ConfigureAwait(true);                   
                    NodeNoticeService.SendNewBlockNodeNoticeAsync(generatedBlock);
                    await LicensorClient.Instance.AddNewBlockAsync(generatedBlock).ConfigureAwait(false);
                }
                catch (BlockGenerationException)
                {
                    blockGenerationTask.Dispose();
                    cancellationTokenSource = new CancellationTokenSource();
                    blockGenerationCancellToken = cancellationTokenSource.Token;
                }
                catch (TaskCanceledException)
                {
                    blockGenerationTask.Dispose();
                    cancellationTokenSource = new CancellationTokenSource();
                    blockGenerationCancellToken = cancellationTokenSource.Token;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex);
                }
                finally
                {
                    blockGenerationTask = null;
                }
            }
        }

        public async Task StopBlockGenerationAsync()
        {
            if (blockGenerationTask.Status == TaskStatus.Running && currentSegments != null)
            {
                cacheService.AddBlockSegments(currentSegments);
                await Task.Delay(1000).ConfigureAwait(true);
            }
        }

        public void HandleNewBlock(BlockVm block)
        {
            if (blockGenerationTask != null && !blockGenerationTask.IsCompleted)
            {
                blockGenerationCancellToken.Register(BlockGenerationCancelledHandleDelegate, block);
                cancellationTokenSource.Cancel();
            }
        }

        private void BlockGenerationCancelledHandleDelegate(object blockObject)
        {
            BlockVm block = (BlockVm)blockObject;
            List<BlockSegmentVm> uniqueBlockSegments = currentSegments.Where(segment => !block.BlockSegments.Any(opt => opt.SegmentHash.SequenceEqual(segment.SegmentHash))).ToList();
            if (uniqueBlockSegments.Any())
            {
                AddSegments(uniqueBlockSegments);
            }
        }

        private BlockVm GenerateBlock(object @object)
        {
            try
            {
                BlockVm block = (BlockVm)@object;
                Task<byte[]> hashGenerationProcess = new Task<byte[]>((obj) => BlockHashing.ComputeBlockHashPoW((BlockVm)obj), block);
                hashGenerationProcess.Start();
                while (!hashGenerationProcess.IsCompleted)
                {
                    blockGenerationCancellToken.ThrowIfCancellationRequested();
                    Task.Delay(250).Wait();
                }
                block.Header.Hash = hashGenerationProcess.Result;
                block.Header.Sign = Encryptor.GetSign(BlockHashing.GetBlockBytes(block), NodeData.Instance.NodeKeys.SignPrivateKey, NodeData.Instance.NodeKeys.Password);
                block.Header.SignKeyId = NodeData.Instance.NodeKeys.KeyId;
                using (BlockchainDbContext context = new BlockchainDbContext())
                {
                    var resultBlock = context.Add(BlockBuilder.GetBlock(block));
                    context.SaveChanges();
                    return BlockBuilder.GetBlockVm(resultBlock.Entity);
                }
            }
            catch (Exception ex)
            {
                throw new BlockGenerationException("An error occurred while generating the block.", ex);
            }
        }
    }
}
