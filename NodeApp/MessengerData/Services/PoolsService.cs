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
using NodeApp.Interfaces;
using NodeApp.LicensorClasses;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Exceptions;
using ObjectsLibrary.LicensorRequestClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class PoolsService : IPoolsService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public PoolsService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task CheckNodePoolsAsync()
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                List<PoolType> poolTypes = new List<PoolType>();
                if (!await context.UsersIdentificators.AnyAsync(opt => !opt.IsUsed).ConfigureAwait(false))
                    poolTypes.Add(PoolType.Users);
                if (!await context.ChatsIdentificators.AnyAsync(opt => !opt.IsUsed).ConfigureAwait(false))
                    poolTypes.Add(PoolType.Chats);
                if (!await context.ChannelsIdentificators.AnyAsync(opt => !opt.IsUsed).ConfigureAwait(false))
                    poolTypes.Add(PoolType.Channels);
                if (!await context.FilesIdentificators.AnyAsync(opt => !opt.IsUsed).ConfigureAwait(false))
                    poolTypes.Add(PoolType.Files);
                if (poolTypes.Any())
                {
                    var nodePools = await LicensorClient.Instance.GetNodePoolsAsync(poolTypes.ToArray()).ConfigureAwait(false);
                    await SaveNodePoolsAsync(nodePools).ConfigureAwait(false);
                }
            }
        }
        public async Task<long> GetUserIdAsync()
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                using (var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        UserIdentificator identificator;
                        if (!await context.UsersIdentificators.AnyAsync(opt => !opt.IsUsed).ConfigureAwait(false))
                        {
                            var nodePools = await LicensorClient.Instance.GetNodePoolsAsync(PoolType.Users).ConfigureAwait(false);
                            await SaveNodePoolsAsync(nodePools).ConfigureAwait(false);
                        }
                        identificator = await context.UsersIdentificators.FirstOrDefaultAsync(opt => !opt.IsUsed).ConfigureAwait(false);
                        identificator.IsUsed = true;
                        context.Update(identificator);
                        transaction.Commit();
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        return identificator.UserId;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new InternalErrorException("Error getting identifier.", ex);
                    }
                }
            }
        }
        public async Task<long> GetChatIdAsync()
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                using (var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        ChatIdentificator identificator;
                        if (!await context.UsersIdentificators.AnyAsync(opt => !opt.IsUsed).ConfigureAwait(false))
                        {
                            var nodePools = await LicensorClient.Instance.GetNodePoolsAsync(PoolType.Chats).ConfigureAwait(false);
                            await SaveNodePoolsAsync(nodePools).ConfigureAwait(false);
                        }
                        identificator = await context.ChatsIdentificators.FirstOrDefaultAsync(opt => !opt.IsUsed).ConfigureAwait(false);
                        identificator.IsUsed = true;
                        context.Update(identificator);
                        transaction.Commit();
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        return identificator.ChatId;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new InternalErrorException("Error getting identifier.", ex);
                    }
                }
            }
        }
        public async Task<long> GetChannelIdAsync()
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                using (var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        ChannelIdentificator identificator;
                        if (!await context.ChannelsIdentificators.AnyAsync(opt => !opt.IsUsed).ConfigureAwait(false))
                        {
                            var nodePools = await LicensorClient.Instance.GetNodePoolsAsync(PoolType.Channels).ConfigureAwait(false);
                            await SaveNodePoolsAsync(nodePools).ConfigureAwait(false);
                        }
                        identificator = await context.ChannelsIdentificators.FirstOrDefaultAsync(opt => !opt.IsUsed).ConfigureAwait(false);
                        identificator.IsUsed = true;
                        context.Update(identificator);
                        transaction.Commit();
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        return identificator.ChannelId;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new InternalErrorException("Error getting identifier.", ex);
                    }
                }
            }
        }
        public async Task<long> GetFileIdAsync()
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                using (var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        FileIdentificator identificator;
                        if (!await context.FilesIdentificators.AnyAsync(opt => !opt.IsUsed).ConfigureAwait(false))
                        {
                            var nodePools = await LicensorClient.Instance.GetNodePoolsAsync(PoolType.Files).ConfigureAwait(false);
                            await SaveNodePoolsAsync(nodePools).ConfigureAwait(false);
                        }
                        identificator = await context.FilesIdentificators.FirstOrDefaultAsync(opt => !opt.IsUsed).ConfigureAwait(false);
                        identificator.IsUsed = true;
                        context.Update(identificator);
                        transaction.Commit();
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        return identificator.FileId;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new InternalErrorException("Error getting identifier.", ex);
                    }
                }
            }
        }
        public async Task SaveNodePoolsAsync(NodePools nodePools)
        {
            var usersIdentificators = nodePools.UsersIds?.Select(userId => new UserIdentificator
            {
                UserId = userId
            });
            var chatsIdentificators = nodePools.ChatsIds?.Select(chatId => new ChatIdentificator
            {
                ChatId = chatId
            });
            var channelsIdentificators = nodePools.ChannelsIds?.Select(channelId => new ChannelIdentificator
            {
                ChannelId = channelId
            });
            var filesIdentificators = nodePools.FilesIds?.Select(fileId => new FileIdentificator
            {
                FileId = fileId
            });
            using (MessengerDbContext context = contextFactory.Create())
            {
                if (usersIdentificators != null)
                    await context.AddRangeAsync(usersIdentificators).ConfigureAwait(false);
                if (chatsIdentificators != null)
                    await context.AddRangeAsync(chatsIdentificators).ConfigureAwait(false);
                if (channelsIdentificators != null)
                    await context.AddRangeAsync(channelsIdentificators).ConfigureAwait(false);
                if (filesIdentificators != null)
                    await context.AddRangeAsync(filesIdentificators).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}