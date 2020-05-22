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
using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using Npgsql;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Channels
{
    public class CreateChannelsService : ICreateChannelsService
    {
        private readonly IUpdateChannelsService updateChannelsService;
        private readonly ILoadUsersService loadUsersService;
        private readonly IPoolsService poolsService;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public CreateChannelsService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.updateChannelsService = appServiceProvider.UpdateChannelsService;
            this.loadUsersService = appServiceProvider.LoadUsersService;
            this.poolsService = appServiceProvider.PoolsService;
            this.contextFactory = contextFactory;
        }
        public async Task<ChannelVm> CreateOrEditChannelAsync(ChannelVm channel, long requestorId, IEnumerable<ChannelUserVm> subscribers)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                bool isExistsChannel = await context.Channels.AnyAsync(opt => opt.ChannelId == channel.ChannelId).ConfigureAwait(false);
                if (isExistsChannel)
                {
                    return await updateChannelsService.EditChannelAsync(channel, requestorId).ConfigureAwait(false);
                }
                else
                {
                    return await CreateChannelAsync(channel, requestorId, subscribers).ConfigureAwait(false);
                }
            }
        }
        public async Task<List<ChannelDto>> CreateOrUpdateUserChannelsAsync(List<ChannelDto> channels)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var channelsCondition = PredicateBuilder.New<Channel>();
                channelsCondition = channels.Aggregate(channelsCondition,
                    (current, value) => current.Or(opt => opt.ChannelId == value.ChannelId).Expand());
                List<Channel> existingChannels = await context.Channels
                    .Include(opt => opt.ChannelUsers)
                    .Where(channelsCondition)
                    .ToListAsync()
                    .ConfigureAwait(false);
                for (int i = 0; i < existingChannels.Count; i++)
                {
                    ChannelDto editedChannel = channels.FirstOrDefault(opt => opt.ChannelId == existingChannels[i].ChannelId);
                    existingChannels[i] = ChannelConverter.GetChannel(existingChannels[i], editedChannel);
                    if (!editedChannel.ChannelUsers.IsNullOrEmpty())
                    {
                        existingChannels[i].ChannelUsers = ChannelConverter.GetChannelUsers(editedChannel.ChannelUsers).ToList();
                    }
                }
                context.UpdateRange(existingChannels);
                List<ChannelDto> nonExistingChannels = channels.Where(channelDto => !existingChannels.Any(channel => channelDto.ChannelId == channel.ChannelId))?.ToList();
                List<Channel> newChannels = ChannelConverter.GetChannels(nonExistingChannels);
                await context.AddRangeAsync(newChannels).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return ChannelConverter.GetChannelsDto(newChannels.Concat(existingChannels));
            }
        }

        public async Task<List<ChannelUserVm>> CreateOrEditChannelUsersAsync(List<ChannelUserVm> channelUsers, long requestorId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    var channelUsersCondition = PredicateBuilder.New<ChannelUser>();
                    channelUsersCondition = channelUsers.Aggregate(channelUsersCondition,
                            (current, value) => current.Or(opt => opt.ChannelId == value.ChannelId && opt.UserId == value.UserId).Expand());
                    List<ChannelUserVm> result = new List<ChannelUserVm>();
                    var existingChannelsUsers = ChannelConverter.GetChannelUsers(await context.ChannelUsers.Where(channelUsersCondition).ToListAsync().ConfigureAwait(false));
                    List<ChannelUserVm> nonExistingChannelUsers = new List<ChannelUserVm>();
                    if (existingChannelsUsers?.Any() ?? false)
                    {
                        var groups = existingChannelsUsers.GroupBy(opt => opt.ChannelId);
                        foreach (var group in groups)
                        {
                            var edited = channelUsers.Where(opt => group.Any(p => p.ChannelId == opt.ChannelId && p.UserId == opt.UserId)).ToList();
                            result.AddRange(await updateChannelsService.EditChannelUsersAsync(edited, requestorId, group.Key.GetValueOrDefault()).ConfigureAwait(false));
                        }
                        nonExistingChannelUsers = channelUsers.Where(opt => !existingChannelsUsers.Any(p => p.ChannelId == opt.ChannelId && p.UserId == opt.UserId)).ToList();
                    }
                    else
                    {
                        nonExistingChannelUsers = channelUsers;
                    }
                    if (nonExistingChannelUsers?.Any() ?? false)
                    {
                        var newChannelUsers = ChannelConverter.GetChannelUsers(nonExistingChannelUsers).ToList();
                        await context.AddRangeAsync(newChannelUsers).ConfigureAwait(false);
                        result.AddRange(ChannelConverter.GetChannelUsers(newChannelUsers));
                    }
                    var channelGroups = channelUsers.GroupBy(opt => opt.ChannelId);
                    foreach (var group in channelGroups)
                    {
                        var channelsNodesIds = await context.ChannelUsers
                            .Where(opt => opt.ChannelId == group.Key)
                            .Include(opt => opt.User)
                            .Select(opt => opt.User.NodeId)
                            .ToArrayAsync()
                            .ConfigureAwait(false);
                        var channel = await context.Channels.FindAsync(group.Key).ConfigureAwait(false);
                        channel.NodesId = channelsNodesIds.Select(id => id.GetValueOrDefault()).Distinct().ToArray();
                        context.Channels.Update(channel);
                    }
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return result;
                }
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is PostgresException postgresException)
                {
                    if (postgresException.ConstraintName == "FK_ChannelUsers_Channels_ChannelId")
                    {
                        throw new ConversationNotFoundException();
                    }

                    if (postgresException.ConstraintName == "FK_ChannelUsers_Users_UserId")
                    {
                        throw new UserNotFoundException();
                    }
                }
                throw new AddOrRemoveChannelUsersException();
            }
        }
        public async Task<ChannelVm> CreateChannelAsync(ChannelVm channel, long creatorId, IEnumerable<ChannelUserVm> subscribers)
        {
            ChannelUser channelUser = new ChannelUser
            {
                ChannelUserRole = ChannelUserRole.Creator,
                SubscribedTime = DateTime.UtcNow.ToUnixTime(),
                UserId = creatorId,
                Deleted = false
            };
            if (subscribers != null && await loadUsersService.IsUserBlacklisted(creatorId, subscribers.Select(opt => opt.UserId)).ConfigureAwait(false))
            {
                throw new UserBlockedException();
            }

            List<ChannelUser> channelUsers = new List<ChannelUser>()
            {
                channelUser
            };
            if (subscribers != null)
            {
                channelUsers.AddRange(ChannelConverter.GetChannelUsers(subscribers.Where(opt => opt.UserId != creatorId)));
            }
            List<UserVm> users = await loadUsersService.GetUsersByIdAsync(channelUsers.Select(opt => opt.UserId)).ConfigureAwait(false);
            if (users.Count() < channelUsers.Count())
            {
                throw new UserNotFoundException();
            }
            Channel newChannel = new Channel
            {
                ChannelId = channel.ChannelId.GetValueOrDefault(await poolsService.GetChannelIdAsync().ConfigureAwait(false)),
                About = channel.About,
                Deleted = false,
                ChannelName = channel.ChannelName,
                CreationTime = DateTime.UtcNow.ToUnixTime(),
                Photo = channel.Photo,
                Tag = string.IsNullOrWhiteSpace(channel.Tag) ? RandomExtensions.NextString(10, "QWERTYUIOPASDFGHJKLZXCVBNM1234567890") : channel.Tag,
                NodesId = users.Select(opt => opt.NodeId.GetValueOrDefault()).Distinct().ToArray()
            };
            using (MessengerDbContext context = contextFactory.Create())
            {
                newChannel.ChannelUsers = channelUsers;
                var entityEntry = await context.Channels.AddAsync(newChannel).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                var createdChannel = ChannelConverter.GetChannel(entityEntry.Entity);
                createdChannel.SubscribersCount = newChannel.ChannelUsers.Count();
                return createdChannel;
            }
        }

        public async Task<List<ChannelDto>> CreateChannelsAsync(List<ChannelDto> channels)
        {
            if (channels == null || !channels.Any())
            {
                return channels;
            }

            var channelsCondition = PredicateBuilder.New<Channel>();
            var resultChannels = new List<Channel>();
            channelsCondition = channels.Aggregate(channelsCondition,
                (current, value) => current.Or(opt => opt.ChannelId == value.ChannelId).Expand());
            using (MessengerDbContext context = contextFactory.Create())
            {
                var existingChannels = await context.Channels.Where(channelsCondition).ToListAsync().ConfigureAwait(false);
                var nonExistingChannels = channels.Where(channel => !existingChannels.Any(exChannel => exChannel.ChannelId == channel.ChannelId));
                resultChannels.AddRange(existingChannels);
                if (nonExistingChannels != null)
                {
                    var newChannels = ChannelConverter.GetChannels(nonExistingChannels.ToList());
                    await context.Channels.AddRangeAsync(newChannels).ConfigureAwait(false);
                    resultChannels.AddRange(newChannels);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
                return ChannelConverter.GetChannelsDto(resultChannels);
            }
        }
    }
}
