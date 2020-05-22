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
using NodeApp.Helpers;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Users
{
    public class LoadUsersService : ILoadUsersService
    {
        private readonly ILoadChatsService loadChatsService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public LoadUsersService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.loadChannelsService = appServiceProvider.LoadChannelsService;
            this.loadChatsService = appServiceProvider.LoadChatsService;
            this.contextFactory = contextFactory;
        }
        public async Task<List<SessionVm>> GetUserSessionsAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var tokens = await context.Tokens
                .AsNoTracking()
                .Where(token => token.UserId == userId &&
                    (token.AccessTokenExpirationTime > DateTime.UtcNow.ToUnixTime() || token.RefreshTokenExpirationTime > DateTime.UtcNow.ToUnixTime()))
                .OrderByDescending(token => token.LastActivityTime)
                .ToListAsync()
                .ConfigureAwait(false);
                return tokens.Select(token => new SessionVm
                {
                    AppName = token.AppName,
                    DeviceName = token.DeviceName,
                    LastActivityTime = token.LastActivityTime,
                    OSName = token.OSName,
                    TokenId = token.Id,
                    IP = token.IPAddress
                }).ToList();
            }
        }
        public async Task<UserDto> GetAllUserDataAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                User user = await context.Users.AsNoTracking()
                    .Include(opt => opt.DialogsFirstU)
                    .ThenInclude(opt => opt.Messages)
                    .ThenInclude(opt => opt.Attachments)
                    .Include(opt => opt.Emails)
                    .Include(opt => opt.Phones)
                    .Include(opt => opt.FilesInfo)
                    .Include(opt => opt.BlackList)
                    .Include(opt => opt.Tokens)
                    .Include(opt => opt.UserPublicKeys)
                    .Include(opt => opt.Contacts)
                    .Include(opt => opt.Favorites)
                    .Include(opt => opt.PollOptionsVotes)
                    .Include(opt => opt.Groups)
                    .ThenInclude(opt => opt.ContactGroups)
                    .ThenInclude(opt => opt.Contact)
                    .FirstOrDefaultAsync(opt => opt.Id == userId)
                    .ConfigureAwait(false);
                IEnumerable<ChatDto> userChats = await loadChatsService.GetUserChatsAsync(userId).ConfigureAwait(false);
                IEnumerable<ChannelDto> userChannels = await loadChannelsService.GetUserChannelsAsync(userId).ConfigureAwait(false);
                foreach (var dialog in user.DialogsFirstU)
                {
                    dialog.Messages = dialog.Messages.Where(opt => !opt.Deleted).ToList();
                }

                foreach (var chat in userChats)
                {
                    chat.Messages = chat.Messages.Where(opt => !opt.Deleted).ToList();
                }

                foreach (var channel in userChannels)
                {
                    channel.Messages = channel.Messages.Where(opt => !opt.Deleted).ToList();
                }
                return UserConverter.GetUserDto(user, userChats, userChannels);
            }
        }
        public async Task<bool> IsUserValidAsync(long userId)
        {
            if (userId == 0)
            {
                return false;
            }

            using (MessengerDbContext context = contextFactory.Create())
            {
                return await context.Users.AnyAsync(user =>
                user.Id == userId && user.Deleted == false && user.Confirmed == true).ConfigureAwait(false);
            }
        }
        public async Task<UserVm> GetUserAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var targetUser = await context.Users
                .AsNoTracking()
                .Include(user => user.Phones)
                .Include(user => user.Emails)
                .FirstOrDefaultAsync(user => user.Id == userId && user.Deleted == false).ConfigureAwait(false);
                return UserConverter.GetUserVm(targetUser);
            }
        }
        public async Task<List<UserVm>> GetUsersAsync(SearchUserVm templateUser = null, byte limit = 100,
           long navigationId = 0, bool confirmed = true)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                List<User> users;
                if (templateUser == null)
                {
                    users = await context.Users
                        .AsNoTracking()
                        .OrderBy(user => user.Id)
                        .Where(user => user.Id > navigationId && user.Confirmed == confirmed && user.Deleted == false && user.NodeId == NodeSettings.Configs.Node.Id)
                        .Include(opt => opt.BlackList)
                        .Include(opt => opt.Emails)
                        .Include(opt => opt.Phones)
                        .Take(limit)
                        .ToListAsync()
                        .ConfigureAwait(false);
                }
                else
                {
                    ExpressionsHelper expressionsHelper = new ExpressionsHelper();
                    users = await context.Users
                        .AsNoTracking()
                        .OrderBy(user => user.Id)
                        .Where(user => user.Id > navigationId && user.Confirmed == true && user.Deleted == false && user.NodeId == NodeSettings.Configs.Node.Id)
                        .Where(expressionsHelper.GetUserExpression(templateUser))
                        .Include(opt => opt.Phones)
                        .Include(opt => opt.BlackList)
                        .Include(opt => opt.Emails)
                        .Take(limit)
                        .ToListAsync()
                        .ConfigureAwait(false);
                }
                return UserConverter.GetUsersVm(users);
            }
        }
        public async Task<List<UserVm>> GetUsersByIdAsync(IEnumerable<long> usersId, long? requestorId = null)
        {
            var usersCondition = PredicateBuilder.New<User>();
            usersCondition = usersId.Aggregate(usersCondition,
                (current, value) => current.Or(user => user.Id == value && user.Confirmed == true).Expand());
            List<User> users;
            using (MessengerDbContext context = contextFactory.Create())
            {
                users = await context
                    .Users
                    .Include(opt => opt.Emails)
                    .Include(opt => opt.Phones)
                    .Include(opt => opt.BlackList)
                    .Where(usersCondition)
                    .ToListAsync()
                    .ConfigureAwait(false);
                if (requestorId != null)
                {
                    var query = from contact in context.Contacts
                                where contact.UserId == requestorId && usersId.Contains(contact.ContactUserId)
                                select new
                                {
                                    Contact = contact,
                                    Groups = (from contactGroup in context.ContactsGroups
                                              join userGroup in context.Groups on contactGroup.GroupId equals userGroup.GroupId
                                              where contactGroup.ContactId == contact.ContactId && userGroup.UserId == requestorId
                                              select userGroup).ToList()
                                };
                    var queryResult = await query.ToListAsync().ConfigureAwait(false);
                    foreach (var item in queryResult)
                    {
                        var contactUser = users.FirstOrDefault(opt => opt.Id == item.Contact.ContactUserId);
                        item.Contact.ContactGroups = item.Groups.Select(group => new ContactGroup
                        {
                            Group = group,
                            GroupId = group.GroupId,
                            ContactId = item.Contact.ContactId

                        }).ToHashSet();
                        contactUser.UserContacts = new List<Contact> { item.Contact };
                    }
                }
                return UserConverter.GetUsersVm(users, requestorId);
            }
        }
        public async Task<UserVm> GetUserInformationAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                User resultUser = await context.Users
                    .AsNoTracking()
                    .Include(user => user.BlackList)
                    .Include(user => user.Emails)
                    .Include(user => user.Phones)
                    .FirstOrDefaultAsync(user => user.Id == userId)
                    .ConfigureAwait(false);
                return UserConverter.GetUserVm(resultUser);
            }
        }
        public async Task<List<UserVm>> FindUsersByPhonesAsync(List<string> phones)
        {
            var phonesCondition = PredicateBuilder.New<Phones>();
            phonesCondition = phones.Aggregate(phonesCondition,
                (current, value) => current.Or(opt => opt.PhoneNumber == value).Expand());
            using (MessengerDbContext context = contextFactory.Create())
            {
                var users = await context.Phones
                    .Include(opt => opt.User)
                    .Where(phonesCondition)
                    .Select(opt => opt.User)
                    .Include(opt => opt.Phones)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return UserConverter.GetUsersVm(users);
            }
        }
        public async Task<List<UserVm>> FindUsersByStringQueryAsync(string stringQuery, long? navigationUserId = 0, bool? direction = true)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                ExpressionsHelper helper = new ExpressionsHelper();
                var query = context.Users
                    .Include(opt => opt.Emails)
                    .Include(opt => opt.Phones)
                    .Include(opt => opt.BlackList)
                    .Where(opt => opt.NodeId == NodeSettings.Configs.Node.Id && opt.Deleted == false && !opt.Deleted && opt.Confirmed == true);
                if (direction.GetValueOrDefault())
                {
                    query = query.OrderBy(opt => opt.Id)
                     .Where(opt => opt.Id > navigationUserId.GetValueOrDefault());
                }
                else
                {
                    query = query.OrderByDescending(opt => opt.Id)
                        .Where(opt => opt.Id < navigationUserId.GetValueOrDefault());
                }
                List<User> users = await query.Where(helper.GetUserExpression(stringQuery))
                    .ToListAsync().ConfigureAwait(false);
                return UserConverter.GetUsersVm(users);
            }
        }
        public async Task<bool> IsUserBlacklisted(long targetUserId, IEnumerable<long> usersId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                ExpressionStarter<User> usersCondition = PredicateBuilder.New<User>();
                usersCondition = usersId.Aggregate(usersCondition,
                    (current, value) => current.Or(user => user.Id == value).Expand());
                User targetUser = await context.Users
                    .AsNoTracking()
                    .Include(user => user.BlackList)
                    .Include(user => user.ReverseBlackList)
                    .FirstOrDefaultAsync(user => user.Id == targetUserId)
                    .ConfigureAwait(false);
                foreach (long userId in usersId)
                {
                    if (targetUser.ReverseBlackList.Any(opt => opt.Uid == userId) ||
                        targetUser.BlackList.Any(opt => opt.BadUid == userId))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public async Task<bool> IsEmailExistsAsync(string email)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return await context.Emails.AnyAsync(opt => opt.EmailAddress.ToLower() == email.ToLowerInvariant()).ConfigureAwait(false);
            }
        }
        public async Task<bool> IsPhoneExistsAsync(string phone)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return await context.Phones.AnyAsync(opt => opt.PhoneNumber == phone).ConfigureAwait(false);
            }
        }
    }
}