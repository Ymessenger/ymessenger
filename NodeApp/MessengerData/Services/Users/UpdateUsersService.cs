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
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using NodeApp.Objects;
using Npgsql;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Users
{
    public class UpdateUsersService : IUpdateUsersService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        private readonly IVerificationCodesService verificationCodesService;
        public UpdateUsersService(IDbContextFactory<MessengerDbContext> contextFactory, IVerificationCodesService verificationCodesService)
        {
            this.contextFactory = contextFactory;
            this.verificationCodesService = verificationCodesService;
        }
        public async Task<UserDto> CreateOrUpdateUserAsync(UserDto user)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                User userInfo = await context.Users
                    .Include(opt => opt.UserPublicKeys)
                    .Include(opt => opt.Phones)
                    .Include(opt => opt.Emails)
                    .Include(opt => opt.BlackList)
                    .Include(opt => opt.Tokens)
                    .FirstOrDefaultAsync(opt => opt.Id == user.Id)
                    .ConfigureAwait(false);
                if (userInfo != null)
                {
                    userInfo = UserConverter.GetUser(userInfo, user);
                    context.Update(userInfo);
                }
                else
                {
                    userInfo = UserConverter.GetUser(user);
                    await context.AddAsync(userInfo).ConfigureAwait(false);
                }
                await context.SaveChangesAsync().ConfigureAwait(false);
                return UserConverter.GetUserDto(userInfo, null, null);
            }
        }
        public async Task SetUsersConfirmedAsync(List<long> usersIds)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var usersCondition = PredicateBuilder.New<User>();
                usersCondition = usersIds.Aggregate(usersCondition,
                    (current, value) => current.Or(opt => opt.Id == value && !opt.Confirmed.Value).Expand());
                var users = await context.Users.Where(usersCondition).ToListAsync().ConfigureAwait(false);
                if (users.Count < usersIds.Count)
                {
                    throw new ObjectDoesNotExistsException();
                }

                users.ForEach(opt => opt.Confirmed = true);
                context.UpdateRange(users);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        public async Task EditUserNodeAsync(long userId, long? newNodeId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                User user = await context.Users.FindAsync(userId).ConfigureAwait(false);
                user.NodeId = newNodeId;
                context.Users.Update(user);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        public async Task<short> CreateVCodeAsync(string targetEmail, long? userId = null)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    var userEmail = await context.Emails.Include(email => email.User)
                        .FirstOrDefaultAsync(email => email.EmailAddress == targetEmail).ConfigureAwait(false);
                    short verificationCode =
                        await verificationCodesService.CreateVerificationCodeAsync(DateTime.UtcNow.ToUnixTime(), targetEmail, userEmail?.UserId ?? userId).ConfigureAwait(false);
                    return verificationCode;
                }
            }
            catch (Exception ex)
            {
                throw new CreateVerificationCodeException("Could not create verification code.", ex);
            }
        }
        public async Task<short> CreateVCodeAsync(UserPhoneVm userPhone, RequestType requestType, long? userId = null)
        {
            try
            {
                if (requestType == RequestType.VerificationUser)
                {
                    using (MessengerDbContext context = contextFactory.Create())
                    {
                        var targetUserPhone = await context.Phones
                            .Include(phone => phone.User)
                            .FirstOrDefaultAsync(phone => phone.PhoneNumber == userPhone.FullNumber)
                            .ConfigureAwait(false);
                        short verificationCode =
                            await verificationCodesService.CreateVerificationCodeAsync(DateTime.UtcNow.ToUnixTime(), userPhone.FullNumber, targetUserPhone?.UserId ?? userId).ConfigureAwait(false);
                        return verificationCode;
                    }
                }
                throw new UnknownVerificationTypeException();
            }
            catch (Exception ex)
            {
                throw new CreateVerificationCodeException("Could not create verification code.", ex);
            }
        }
        public async Task<UserVm> EditUserAsync(EditUserVm editableUser, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                User editedUser = await context.Users
                    .Include(user => user.Phones)
                    .Include(user => user.Emails)
                    .FirstOrDefaultAsync(user => user.Id == userId && user.Deleted == false)
                    .ConfigureAwait(false);
                editedUser = UserConverter.GetUser(editedUser, editableUser);
                context.Update(editedUser);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return UserConverter.GetUserVm(editedUser);
            }
        }
        public async Task UpdateUserActivityTimeAsync(ClientConnection clientConnection)
        {
            try
            {
                if (clientConnection.UserId != null)
                {
                    using (MessengerDbContext context = contextFactory.Create())
                    {
                        NpgsqlParameter userIdParam = new NpgsqlParameter("userId", clientConnection.UserId.Value);
                        NpgsqlParameter timeParam = new NpgsqlParameter("time", DateTime.UtcNow.ToUnixTime());
                        string updateCommand = @"UPDATE ""Users"" SET ""Online"" = @time WHERE ""Id"" = @userId";
                        await context.Database.ExecuteSqlCommandAsync(updateCommand, userIdParam, timeParam).ConfigureAwait(false);
                        if (clientConnection.CurrentToken != null)
                        {
                            NpgsqlParameter tokenParam = new NpgsqlParameter("token", clientConnection.CurrentToken.AccessToken);
                            string updateTokenCommand = @"UPDATE ""Tokens"" SET ""LastActivityTime"" = @time WHERE ""UserId"" = @userId AND ""AccessToken"" = @token";
                            await context.Database.ExecuteSqlCommandAsync(updateTokenCommand, userIdParam, timeParam, tokenParam).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public async Task<List<long>> AddUsersToBlackListAsync(IEnumerable<long> usersId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var usersCondition = PredicateBuilder.New<User>();
                usersCondition = usersId.Aggregate(usersCondition,
                    (current, value) => current.Or(opt => opt.Id == value).Expand());
                List<long> existingUsersId =
                    await context.Users.Where(usersCondition).Select(opt => opt.Id).ToListAsync().ConfigureAwait(false);
                if (existingUsersId.Count < usersId.Count())
                {
                    throw new ObjectDoesNotExistsException();
                }

                List<BadUser> badUsers = new List<BadUser>();
                foreach (long Id in existingUsersId)
                {
                    badUsers.Add(new BadUser
                    {
                        Uid = userId,
                        BadUid = Id
                    });
                }
                await context.BadUsers.AddRangeAsync(badUsers).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return existingUsersId;
            }
        }
        public async Task<List<long>> DeleteUsersFromBlackListAsync(IEnumerable<long> usersId,
            long userId)
        {
            var usersCondition = PredicateBuilder.New<BadUser>();
            usersCondition = usersId.Aggregate(usersCondition,
                (current, value) => current.Or(badUser => badUser.BadUid == value && badUser.Uid == userId).Expand());
            using (MessengerDbContext context = contextFactory.Create())
            {
                List<BadUser> badUsers = await context.BadUsers.Where(usersCondition).ToListAsync().ConfigureAwait(false);
                if (badUsers.Count < usersId.Count()
                    || !badUsers.Any()
                    || !usersId.Any())
                {
                    throw new ObjectDoesNotExistsException();
                }
                context.BadUsers.RemoveRange(badUsers);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return badUsers.Select(opt => opt.BadUid).ToList();
            }
        }

        public async Task<UserVm> UpdateUserPhoneAsync(long userId, string phone)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var user = await context.Users
                    .Include(opt => opt.Phones)
                    .Include(opt => opt.Emails)
                    .FirstOrDefaultAsync(opt => opt.Id == userId)
                    .ConfigureAwait(false);
                user.Phones = new List<Phones>
                {
                    new Phones
                    {
                        Main = true,
                        PhoneNumber = phone,
                        UserId = userId
                    }
                };
                context.Users.Update(user);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return UserConverter.GetUserVm(user);
            }
        }

        public async Task<UserVm> UpdateUserEmailAsync(long userId, string email)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var user = await context.Users
                    .Include(opt => opt.Emails)
                    .Include(opt => opt.Phones)
                    .FirstOrDefaultAsync(opt => opt.Id == userId)
                    .ConfigureAwait(false);
                user.Emails = new List<Emails>
                {
                    new Emails
                    {
                        EmailAddress = email,
                        UserId = userId
                    }
                };
                context.Users.Update(user);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return UserConverter.GetUserVm(user);
            }
        } 
        public async Task<UserVm> SetUserBannedAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var user = await context.Users
                    .FirstOrDefaultAsync(opt => opt.Id == userId)
                    .ConfigureAwait(false);
                user.Banned = !user.Banned;
                context.Users.Update(user);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return UserConverter.GetUserVm(user);
            }
        }
    }
}