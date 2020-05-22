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
using NodeApp.Helpers;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Channels
{
    public class LoadChannelsService : ILoadChannelsService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public LoadChannelsService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task<List<ChannelVm>> FindChannelsByStringQueryAsync(string searchQuery, long? navigationId, bool? direction = true)
        {
            ExpressionsHelper helper = new ExpressionsHelper();
            var searchExpression = helper.GetChannelExpression(searchQuery);
            using (MessengerDbContext context = contextFactory.Create())
            {
                var query = context.Channels
                .AsNoTracking()
                .Where(opt => !opt.Deleted)
                .Where(searchExpression);
                if (direction.GetValueOrDefault())
                {
                    query = query.OrderBy(opt => opt.ChannelId)
                        .Where(opt => opt.ChannelId > navigationId.GetValueOrDefault());
                }
                else
                {
                    query = query.OrderByDescending(opt => opt.ChannelId)
                        .Where(opt => opt.ChannelId < navigationId.GetValueOrDefault());
                }
                var channels = await query.ToListAsync().ConfigureAwait(false);
                return ChannelConverter.GetChannels(channels);
            }
        }
        public async Task<List<ChannelDto>> GetUserChannelsAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var query = from channel in context.Channels
                            join channelUser in context.ChannelUsers on channel.ChannelId equals channelUser.ChannelId
                            where channelUser.UserId == userId
                            && !channelUser.Banned
                            && !channelUser.Deleted
                            && !channel.Deleted
                            select channel;
                List<Channel> channels = await query.AsNoTracking()
                    .Include(opt => opt.ChannelUsers)
                    .Include(opt => opt.Messages)
                        .ThenInclude(opt => opt.Attachments)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return ChannelConverter.GetChannelsDto(channels);
            }
        }
        public async Task<List<long>> GetUserChannelsIdAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return await context.ChannelUsers
                .Where(opt => opt.UserId == userId
                    && opt.Deleted == false
                    && opt.Banned == false
                    && opt.Channel.Deleted == false)
                .Select(opt => opt.ChannelId)
                .ToListAsync()
                .ConfigureAwait(false);
            }
        }
        public async Task<List<long>> GetChannelNodesIdAsync(long channelId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return (await context.Channels
                .Where(opt => opt.ChannelId == channelId)
                .Select(opt => opt.NodesId)
                .FirstOrDefaultAsync().ConfigureAwait(false)).ToList();
            }
        }
        public async Task<List<ChannelVm>> GetChannelsAsync(IEnumerable<long> channelsId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var channelsCondition = PredicateBuilder.New<Channel>();
                channelsCondition = channelsId.Aggregate(channelsCondition,
                    (current, value) => current.Or(opt => opt.ChannelId == value).Expand());

                List<Channel> channels = await context.Channels
                    .AsNoTracking()
                    .Where(channelsCondition)
                    .Where(opt => opt.Deleted == false)
                    .ToListAsync()
                    .ConfigureAwait(false);
                if (channels == null || !channels.Any())
                {
                    throw new GetConversationsException();
                }
                List<ChannelVm> resultChannels = ChannelConverter.GetChannels(channels).ToList();
                var channelUsersCondition = PredicateBuilder.New<ChannelUser>();
                channelUsersCondition = channels.Aggregate(channelUsersCondition,
                    (current, value) => current.Or(opt => opt.ChannelId == value.ChannelId && opt.UserId == userId).Expand());
                List<ChannelUser> channelUsers = await context
                    .ChannelUsers
                    .AsNoTracking()
                    .Where(channelUsersCondition)
                    .ToListAsync()
                    .ConfigureAwait(false);
                foreach (var channel in resultChannels)
                {
                    channel.SubscribersCount = await context.ChannelUsers.CountAsync(opt =>
                        opt.Deleted == false && opt.ChannelId == channel.ChannelId).ConfigureAwait(false);
                    var currentChannelUser = channelUsers.FirstOrDefault(opt => opt.ChannelId == channel.ChannelId);
                    if (currentChannelUser != null && !currentChannelUser.Deleted)
                    {
                        channel.UserRole = currentChannelUser.ChannelUserRole;
                    }
                }
                var bannedUsers = channelUsers.Where(opt => opt.Banned)?.ToList();
                if (bannedUsers != null)
                {
                    foreach (var channelUser in bannedUsers)
                    {
                        var channel = resultChannels.FirstOrDefault(opt => opt.ChannelId == channelUser.ChannelId);
                        if (channelUser.Banned && channel != null)
                        {
                            resultChannels.Remove(channel);
                        }
                    }
                }
                return resultChannels;
            }
        }
        public async Task<List<ChannelUserVm>> GetChannelUsersAsync(long channelId, long? navigationUserId, long? requestorId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                if (navigationUserId == null)
                {
                    navigationUserId = 0;
                }

                if (requestorId != null)
                {
                    ChannelUser requestorChannelUser = await context.ChannelUsers
                        .FirstOrDefaultAsync(opt =>
                               opt.ChannelUserRole >= ChannelUserRole.Administrator
                            && opt.ChannelId == channelId
                            && opt.UserId == requestorId)
                        .ConfigureAwait(false);
                    if (requestorChannelUser == null)
                    {
                        throw new PermissionDeniedException();
                    }
                }
                var channelUsers = await context.ChannelUsers
                    .OrderByDescending(opt => opt.ChannelUserRole)
                    .ThenBy(opt => opt.UserId)
                    .Where(opt => opt.ChannelId == channelId && opt.UserId > navigationUserId && opt.Deleted == false)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return ChannelConverter.GetChannelUsers(channelUsers);
            }
        }
        public async Task<List<long>> GetChannelUsersIdAsync(long channelId, bool banned = false, bool deleted = false)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return await context.ChannelUsers
                .Where(opt => opt.ChannelId == channelId && opt.Banned == banned && opt.Deleted == deleted)
                .Select(opt => opt.UserId)
                .ToListAsync()
                .ConfigureAwait(false);
            }
        }
        public async Task<ChannelVm> GetChannelByIdAsync(long channelId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                Channel channel = await context.Channels
                    .FirstOrDefaultAsync(opt => opt.ChannelId == channelId).ConfigureAwait(false);
                return ChannelConverter.GetChannel(channel);
            }
        }
        public async Task<List<ConversationPreviewVm>> GetUserChannelsPreviewAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var query = from channel in context.Channels
                            join channelUser in context.ChannelUsers on channel.ChannelId equals channelUser.ChannelId
                            join lastReadedMessage in context.Messages on
                                new { MessageId = channelUser.LastReadedGlobalMessageId, ChannelId = (long?)channelUser.ChannelId }
                                    equals
                                new { MessageId = (Guid?)lastReadedMessage.GlobalId, ChannelId = lastReadedMessage.ChannelId }
                                    into lastReadedMessageTable
                            from lastReadedMessage in lastReadedMessageTable.DefaultIfEmpty()
                            join message in context.Messages on
                                new { MessageId = channel.LastMessageGlobalId, ChannelId = (long?)channel.ChannelId }
                                    equals
                                new { MessageId = (Guid?)message.GlobalId, ChannelId = message.ChannelId }
                                    into messagesTable
                            from message in messagesTable.DefaultIfEmpty()
                            join attachment in context.Attachments on message.Id equals attachment.MessageId into attachTable
                            from attachment in attachTable.DefaultIfEmpty()
                            where channelUser.UserId == userId
                               && channel.Deleted == false
                               && channelUser.Deleted == false
                               && message.Deleted == false
                            select new ConversationPreviewVm
                            {
                                ConversationId = channel.ChannelId,
                                ConversationType = ConversationType.Channel,
                                LastMessageSenderId = message.SenderId,
                                LastMessageTime = message.SendingTime,
                                Photo = channel.Photo,
                                PreviewText = message.Text,
                                Title = channel.ChannelName,
                                Read = lastReadedMessage.SendingTime >= message.SendingTime ? true : false,
                                AttachmentType = (AttachmentType)attachment.Type,
                                LastMessageId = message.GlobalId,
                                IsMuted = channelUser.IsMuted
                            };
                return await query.ToListAsync().ConfigureAwait(false);
            }
        }
        public async Task<List<ChannelUserVm>> GetAdministrationChannelUsersAsync(long channelId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var channelUsers = await context.ChannelUsers.Where(opt =>
                opt.ChannelId == channelId
                && opt.Banned == false
                && opt.Deleted == false
                && opt.ChannelUserRole >= ChannelUserRole.Administrator)
                .ToListAsync()
                .ConfigureAwait(false);
                return ChannelConverter.GetChannelUsers(channelUsers);
            }
        }
        public async Task<List<ChannelDto>> GetChannelsWithSubscribersAsync(IEnumerable<long> channelsId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var channelsCondition = PredicateBuilder.New<Channel>();
                channelsCondition = channelsId.Aggregate(channelsCondition,
                    (current, value) => current.Or(opt => opt.ChannelId == value).Expand());
                List<Channel> channels = await context.Channels
                    .Include(opt => opt.ChannelUsers)
                    .Where(channelsCondition)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return ChannelConverter.GetChannelsDto(channels);
            }
        }
    }
}