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
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Channels
{
    public class UpdateChannelsService : IUpdateChannelsService
    {        
        private readonly ILoadUsersService loadUsersService;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public UpdateChannelsService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {            
            loadUsersService = appServiceProvider.LoadUsersService;
            this.contextFactory = contextFactory;
        }

        public async Task<ChannelVm> EditChannelAsync(ChannelVm channel, long editorUserId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                ChannelUser channelUser = await context.ChannelUsers
                .Include(opt => opt.Channel)
                .FirstOrDefaultAsync(opt =>
                       opt.ChannelId == channel.ChannelId
                    && opt.UserId == editorUserId
                    && opt.ChannelUserRole >= ChannelUserRole.Administrator)
                .ConfigureAwait(false);
                if (channelUser != null)
                {
                    if (channelUser.ChannelUserRole == ChannelUserRole.Administrator && (channelUser.Banned || channelUser.Deleted))
                    {
                        throw new PermissionDeniedException();
                    }

                    channelUser.Channel = ChannelConverter.GetChannel(channelUser.Channel, channel);
                    context.Update(channelUser.Channel);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    var editedChannel = ChannelConverter.GetChannel(channelUser.Channel);
                    editedChannel.UserRole = channelUser.ChannelUserRole;
                    editedChannel.SubscribersCount = await context.ChannelUsers.CountAsync(opt => opt.Deleted == false && opt.ChannelId == channel.ChannelId).ConfigureAwait(false);
                    return editedChannel;
                }
                throw new PermissionDeniedException();
            }
        }
        public async Task<List<ChannelUserVm>> EditChannelUsersAsync(List<ChannelUserVm> channelUsers, long editorUserId, long channelId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                ExpressionStarter<ChannelUser> channelUsersContidion = PredicateBuilder.New<ChannelUser>();
                channelUsersContidion = channelUsers.Aggregate(channelUsersContidion,
                    (current, value) => current.Or(opt => opt.ChannelId == value.ChannelId && opt.UserId == value.UserId));

                Channel currentChannel = await context.Channels.FirstOrDefaultAsync(opt => opt.ChannelId == channelId && !opt.Deleted).ConfigureAwait(false);
                if (currentChannel == null)
                {
                    throw new ObjectDoesNotExistsException("Channel not found.");
                }

                ChannelUser editorChannelUser = await context.ChannelUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(opt =>
                           opt.ChannelId == channelId
                        && opt.UserId == editorUserId)
                    .ConfigureAwait(false);
                List<ChannelUser> editableChannelUsers = await context.ChannelUsers
                    .Where(channelUsersContidion)
                    .ToListAsync()
                    .ConfigureAwait(false);
                if (editorChannelUser == null)
                {
                    throw new PermissionDeniedException();
                }
                if (editableChannelUsers == null || editableChannelUsers.Count < channelUsers.Count())
                {
                    throw new ObjectDoesNotExistsException("Editable users are not in channel.");
                }
                if (editorChannelUser.ChannelUserRole == ChannelUserRole.Subscriber
                    && channelUsers.Count() != 1
                    && channelUsers.ElementAt(0).UserId != editorChannelUser.UserId
                    || editorChannelUser.Banned)
                {
                    throw new PermissionDeniedException();
                }
                editableChannelUsers.ForEach(channelUser =>
                {
                    ChannelUserVm edited = channelUsers.FirstOrDefault(opt => opt.UserId == channelUser.UserId);
                    if (editorChannelUser.ChannelUserRole > channelUser.ChannelUserRole)
                    {
                        channelUser.ChannelUserRole = edited.ChannelUserRole < editorChannelUser.ChannelUserRole && editorChannelUser.UserId != channelUser.UserId
                            ? edited.ChannelUserRole.GetValueOrDefault()
                            : channelUser.ChannelUserRole;
                        channelUser.Deleted = channelUser.ChannelUserRole != ChannelUserRole.Creator && edited.Deleted.GetValueOrDefault(channelUser.Deleted);
                        channelUser.Banned = channelUser.ChannelUserRole != ChannelUserRole.Creator && edited.Banned.GetValueOrDefault(channelUser.Banned);
                    }
                    else if (editorChannelUser.UserId == channelUser.UserId)
                    {
                        channelUser.ChannelUserRole = edited.ChannelUserRole < channelUser.ChannelUserRole
                            ? edited.ChannelUserRole.GetValueOrDefault()
                            : channelUser.ChannelUserRole;
                        channelUser.Deleted = edited.Deleted.GetValueOrDefault(false);
                    }
                    else
                    {
                        throw new PermissionDeniedException();
                    }
                });
                context.ChannelUsers.UpdateRange(editableChannelUsers);
                if (channelUsers.Any(opt => opt.Deleted.GetValueOrDefault() || opt.Banned.GetValueOrDefault()))
                {
                    var query = from channel in context.Channels
                                join channelUser in context.ChannelUsers on channel.ChannelId equals channelUser.ChannelId
                                join user in context.Users on channelUser.UserId equals user.Id
                                where channel.ChannelId == channelId && !channelUser.Banned && !channelUser.Deleted
                                select user.NodeId;
                    var channelNodesId = (await query.ToListAsync().ConfigureAwait(false)).Distinct();
                    currentChannel.NodesId = channelNodesId.Select(id => id.GetValueOrDefault()).ToArray();
                    context.Channels.Update(currentChannel);
                }
                await context.SaveChangesAsync().ConfigureAwait(false);
                return ChannelConverter.GetChannelUsers(editableChannelUsers);
            }
        }
        public async Task<List<ChannelUserVm>> AddUsersToChannelAsync(List<long> usersId, long channelId, long requestorId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var usersCondition = PredicateBuilder.New<ChannelUser>();
                usersCondition = usersId.Aggregate(usersCondition,
                    (current, value) => current.Or(opt => opt.UserId == value).Expand());
                var requestorUser = await context.ChannelUsers
                    .FirstOrDefaultAsync(opt =>
                           opt.ChannelUserRole >= ChannelUserRole.Administrator
                        && opt.ChannelId == channelId
                        && opt.UserId == requestorId
                        && opt.Channel.Deleted == false)
                    .ConfigureAwait(false);
                if (requestorUser == null && (usersId.Count() > 1 || (usersId.Count() == 1 && usersId.ElementAt(0) != requestorId)))
                {
                    throw new AddUserToChannelException();
                }

                List<ChannelUser> existingUsers = await context.ChannelUsers
                    .Where(usersCondition)
                    .Where(opt => opt.ChannelId == channelId)
                    .ToListAsync()
                    .ConfigureAwait(false);
                List<long> nonExistingUsersId;
                if (existingUsers?.Any() ?? false)
                {
                    nonExistingUsersId = usersId.Where(id => !existingUsers.Any(opt => opt.UserId == id)).ToList();
                }
                else
                {
                    nonExistingUsersId = usersId;
                }

                List<ChannelUser> newChannelUsers = nonExistingUsersId.Select(id => new ChannelUser
                {
                    ChannelId = channelId,
                    ChannelUserRole = ChannelUserRole.Subscriber,
                    SubscribedTime = DateTime.UtcNow.ToUnixTime(),
                    UserId = id
                }).ToList();
                var users = await loadUsersService.GetUsersByIdAsync(nonExistingUsersId).ConfigureAwait(false);
                Channel channel = await context.Channels.FirstOrDefaultAsync(opt => opt.ChannelId == channelId).ConfigureAwait(false);
                channel.NodesId = channel.NodesId.Concat(users.Select(opt => opt.NodeId.GetValueOrDefault())).Distinct().ToArray();
                context.Update(channel);
                var updatedUsers = new List<ChannelUser>();
                if (existingUsers != null)
                {
                    existingUsers.ForEach(channelUser =>
                    {
                        if (!channelUser.Banned && channelUser.Deleted)
                        {
                            channelUser.Deleted = false;
                            updatedUsers.Add(channelUser);
                        }
                    });
                    context.UpdateRange(existingUsers);
                }
                await context.AddRangeAsync(newChannelUsers).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return ChannelConverter.GetChannelUsers(newChannelUsers.Concat(updatedUsers));
            }
        }
        public async Task UpdateChannelLastReadedMessageAsync(MessageDto message, long userId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    ChannelUser channelUser = await context.ChannelUsers.FirstOrDefaultAsync(opt =>
                    opt.ChannelId == message.ConversationId && opt.UserId == userId).ConfigureAwait(false);
                    var oldMessage = await context.Messages
                        .AsNoTracking()
                        .FirstOrDefaultAsync(opt => opt.GlobalId == channelUser.LastReadedGlobalMessageId && opt.ChannelId == channelUser.ChannelId)
                        .ConfigureAwait(false);
                    if (oldMessage != null && oldMessage.SendingTime > message.SendingTime)
                    {
                        return;
                    }

                    channelUser.LastReadedGlobalMessageId = message.GlobalId;
                    context.ChannelUsers.Update(channelUser);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
    }
}