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
using NodeApp.MessengerData.Entities;
using NodeApp.MessengerData.Services.Users;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class TokensService : ITokensService
    {
        public const int ACCESS_LIFETIME_HOUR = 24 * 3;
        public const int REFRESH_LIFETIME_HOUR = 24 * 30;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        private readonly IVerificationCodesService verificationCodesService;
        public TokensService(IDbContextFactory<MessengerDbContext> contextFactory, IVerificationCodesService verificationCodesService)
        {
            this.contextFactory = contextFactory;
            this.verificationCodesService = verificationCodesService;
        }
        public async Task<TokenVm> UserIdVCodeCreateTokenPairAsync(long userId, short vCode,
            string deviceTokenId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                User targetUser = await context.Users
                    .Include(opt => opt.Phones)
                    .FirstOrDefaultAsync(user => user.Id == userId && !user.Deleted)
                    .ConfigureAwait(false);
                if (targetUser != null)
                {
                    if (!await verificationCodesService.IsVerificationCodeValidAsync(
                        targetUser.Phones.FirstOrDefault().PhoneNumber, userId, vCode).ConfigureAwait(false))
                    {
                        throw new WrongVerificationCodeException();
                    }
                    string accessToken = RandomExtensions.NextString(64);
                    string refreshToken = RandomExtensions.NextString(64);
                    Token tokenPair = new Token()
                    {
                        UserId = userId,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        AccessTokenExpirationTime = DateTime.UtcNow.AddHours(ACCESS_LIFETIME_HOUR).ToUnixTime(),
                        RefreshTokenExpirationTime = DateTime.UtcNow.AddHours(REFRESH_LIFETIME_HOUR).ToUnixTime(),
                        DeviceTokenId = deviceTokenId
                    };
                    context.Users.Update(targetUser);
                    await context.Tokens.AddAsync(tokenPair).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return TokenConverter.GetTokenVm(tokenPair);
                }
                throw new UserNotFoundException();
            }
        }
        public async Task<ValuePair<TokenVm, string>> UserIdPasswordCreateTokenPairAsync(long userId, string password,
            string deviceTokenId)
        {
            byte[] passwordHash = CreateUsersService.GetSha512Hash(password);
            using (MessengerDbContext context = contextFactory.Create())
            {
                User targetUser =
                    await context.Users.FirstOrDefaultAsync(user => user.Id == userId && user.Sha512Password.SequenceEqual(passwordHash) && !user.Deleted)
                    .ConfigureAwait(false);
                if (targetUser != null)
                {
                    string accessToken = RandomExtensions.NextString(64);
                    string refreshToken = RandomExtensions.NextString(64);
                    Token tokenPair = new Token()
                    {
                        UserId = userId,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        AccessTokenExpirationTime = DateTime.UtcNow.AddHours(ACCESS_LIFETIME_HOUR).ToUnixTime(),
                        RefreshTokenExpirationTime = DateTime.UtcNow.AddHours(REFRESH_LIFETIME_HOUR).ToUnixTime(),
                        DeviceTokenId = deviceTokenId
                    };
                    string newPassword = RandomExtensions.NextString(CreateUsersService.PASSWORD_LENGTH);
                    targetUser.Sha512Password = CreateUsersService.GetSha512Hash(newPassword);
                    context.Users.Update(targetUser);
                    await context.Tokens.AddAsync(tokenPair).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return new ValuePair<TokenVm, string>(TokenConverter.GetTokenVm(tokenPair), newPassword);
                }
            }
            throw new UserNotFoundException();
        }
        public async Task<TokenVm> CreateTokenPairByUserIdAsync(long userId, bool generateRefresh = true,
            int? tokenLifetimeSeconds = ACCESS_LIFETIME_HOUR * 60 * 60)
        {
            string accessToken = RandomExtensions.NextString(64);
            var tokenPair = new Token()
            {
                UserId = userId,
                AccessToken = accessToken,
                RefreshToken = generateRefresh ? RandomExtensions.NextString(64) : null,
                AccessTokenExpirationTime = DateTime.UtcNow.AddSeconds(tokenLifetimeSeconds.GetValueOrDefault())
                    .ToUnixTime(),
                RefreshTokenExpirationTime =
                    DateTime.UtcNow.AddSeconds(REFRESH_LIFETIME_HOUR * 60 * 60).ToUnixTime(),

            };
            using (MessengerDbContext context = contextFactory.Create())
            {
                await context.Tokens.AddAsync(tokenPair).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
            return TokenConverter.GetTokenVm(tokenPair);
        }
        public async Task<TokenVm> PhoneVCodeCreateTokenPairAsync(string phone, short vCode,
           string deviceTokenId)
        {
            string accessToken = RandomExtensions.NextString(64);
            string refreshToken = RandomExtensions.NextString(64);
            using (MessengerDbContext context = contextFactory.Create())
            {
                User targetUser = await context.Users
                    .FirstOrDefaultAsync(user => user.Phones.Any(p => p.PhoneNumber == phone) && !user.Deleted)
                    .ConfigureAwait(false);
                if (targetUser == null)
                {
                    throw new UserNotFoundException();
                }

                if (await verificationCodesService.IsVerificationCodeValidAsync(phone, targetUser.Id, vCode).ConfigureAwait(false))
                {
                    Token tokenPair = new Token()
                    {
                        UserId = targetUser.Id,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        AccessTokenExpirationTime = DateTime.UtcNow.AddHours(ACCESS_LIFETIME_HOUR).ToUnixTime(),
                        RefreshTokenExpirationTime = DateTime.UtcNow.AddHours(REFRESH_LIFETIME_HOUR).ToUnixTime(),
                        DeviceTokenId = deviceTokenId
                    };
                    await context.Tokens.AddAsync(tokenPair).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return TokenConverter.GetTokenVm(tokenPair);
                }
                throw new WrongVerificationCodeException();
            }
        }
        public async Task<TokenVm> EmailVCodeCreateTokenPairAsync(string email, short vCode,
           string deviceTokenId)
        {
            string accessToken = RandomExtensions.NextString(64);
            string refreshToken = RandomExtensions.NextString(64);
            using (MessengerDbContext context = contextFactory.Create())
            {
                var targetUser =
                    await context.Users.FirstOrDefaultAsync(user => user.Emails.Any(p => p.EmailAddress == email) && !user.Deleted)
                    .ConfigureAwait(false);
                if (targetUser == null)
                {
                    throw new UserNotFoundException();
                }
                if (!await verificationCodesService.IsVerificationCodeValidAsync(email, targetUser.Id, vCode).ConfigureAwait(false))
                {
                    throw new WrongVerificationCodeException();
                }
                var tokenPair = new Token
                {
                    UserId = targetUser.Id,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    AccessTokenExpirationTime = DateTime.UtcNow.AddHours(ACCESS_LIFETIME_HOUR).ToUnixTime(),
                    RefreshTokenExpirationTime = DateTime.UtcNow.AddHours(REFRESH_LIFETIME_HOUR).ToUnixTime(),
                    DeviceTokenId = deviceTokenId
                };
                await context.Tokens.AddAsync(tokenPair).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return TokenConverter.GetTokenVm(tokenPair);
            }
        }
        public async Task<ValuePair<TokenVm, string>> EmailPasswordCreateTokenPairAsync(string targetEmail, string password,
            string deviceTokenId)
        {
            byte[] passwordHash = CreateUsersService.GetSha512Hash(password);
            using (MessengerDbContext context = contextFactory.Create())
            {
                User targetUser = await context.Users
                    .Include(user => user.Emails)
                    .FirstOrDefaultAsync(user => user.Emails.Any(p => p.EmailAddress == targetEmail)
                                                 && user.Sha512Password.SequenceEqual(passwordHash) && !user.Deleted)
                    .ConfigureAwait(false);
                if (targetUser != null)
                {
                    string accessToken = RandomExtensions.NextString(64);
                    string refreshToken = RandomExtensions.NextString(64);
                    Token tokenPair = new Token()
                    {
                        UserId = targetUser.Id,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        AccessTokenExpirationTime = DateTime.UtcNow.AddHours(ACCESS_LIFETIME_HOUR).ToUnixTime(),
                        RefreshTokenExpirationTime = DateTime.UtcNow.AddHours(REFRESH_LIFETIME_HOUR).ToUnixTime(),
                        DeviceTokenId = deviceTokenId
                    };
                    string newPassword = RandomExtensions.NextString(CreateUsersService.PASSWORD_LENGTH);
                    targetUser.Sha512Password = CreateUsersService.GetSha512Hash(newPassword);
                    context.Users.Update(targetUser);
                    EmailHandler.SendEmailAsync(targetEmail, newPassword);
                    await context.Tokens.AddAsync(tokenPair).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return new ValuePair<TokenVm, string>(TokenConverter.GetTokenVm(tokenPair), newPassword);
                }
                else
                {
                    throw new UserNotFoundException();
                }
            }
        }
        public async Task<ValuePair<TokenVm, string>> PhonePasswordCreateTokenPairAsync(string phone, string password,
           string deviceTokenId)
        {
            byte[] passwordHash = CreateUsersService.GetSha512Hash(password);
            using (MessengerDbContext context = contextFactory.Create())
            {
                User targetUser = await context.Users
                    .Include(user => user.Emails)
                    .FirstOrDefaultAsync(user =>
                        user.Phones.Any(p => p.PhoneNumber == phone) && user.Sha512Password.SequenceEqual(passwordHash) && !user.Deleted)
                    .ConfigureAwait(false);
                if (targetUser == null)
                {
                    throw new UserNotFoundException();
                }

                string accessToken = RandomExtensions.NextString(64);
                string refreshToken = RandomExtensions.NextString(64);
                Token tokenPair = new Token()
                {
                    UserId = targetUser.Id,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    AccessTokenExpirationTime = DateTime.UtcNow.AddHours(ACCESS_LIFETIME_HOUR).ToUnixTime(),
                    RefreshTokenExpirationTime = DateTime.UtcNow.AddHours(REFRESH_LIFETIME_HOUR).ToUnixTime(),
                    DeviceTokenId = deviceTokenId
                };
                string newPassword = RandomExtensions.NextString(CreateUsersService.PASSWORD_LENGTH);
                targetUser.Sha512Password = CreateUsersService.GetSha512Hash(newPassword);
                context.Users.Update(targetUser);
                await context.Tokens.AddAsync(tokenPair).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return new ValuePair<TokenVm, string>(TokenConverter.GetTokenVm(tokenPair), newPassword);
            }
        }
        public async Task<TokenVm> CheckTokenAsync(TokenVm targetToken, long nodeId)
        {
            long userId = targetToken.UserId;
            using (MessengerDbContext context = contextFactory.Create())
            {
                Token tokenPair = await context.Tokens
                    .Include(token => token.User)
                    .FirstOrDefaultAsync(
                        token => token.UserId == userId && token.AccessToken == targetToken.AccessToken && !token.User.Deleted)
                    .ConfigureAwait(false);
                if (tokenPair == null)
                {
                    User userInfo = await context.Users.FindAsync(userId).ConfigureAwait(false);
                    if (userInfo != null && userInfo.NodeId != nodeId)
                    {
                        throw new UserFromAnotherNodeException(userInfo.NodeId);
                    }
                    throw new InvalidTokenException();
                }

                if (DateTime.UtcNow <= tokenPair.AccessTokenExpirationTime.ToDateTime())
                {
                    tokenPair.DeviceTokenId = targetToken.DeviceTokenId;
                    context.Update(tokenPair);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return TokenConverter.GetTokenVm(tokenPair);
                }
                if (DateTime.UtcNow >= tokenPair.AccessTokenExpirationTime.ToDateTime()
                    && DateTime.UtcNow <= tokenPair.RefreshTokenExpirationTime.GetValueOrDefault().ToDateTime())
                {
                    string accessToken = RandomExtensions.NextString(64);
                    string refreshToken = RandomExtensions.NextString(64);
                    Token newToken = new Token
                    {
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        UserId = userId,
                        DeviceTokenId = targetToken.DeviceTokenId,
                        AccessTokenExpirationTime = DateTime.UtcNow.AddHours(ACCESS_LIFETIME_HOUR).ToUnixTime(),
                        RefreshTokenExpirationTime = DateTime.UtcNow.AddHours(REFRESH_LIFETIME_HOUR).ToUnixTime()
                    };
                    await context.Tokens.AddAsync(newToken).ConfigureAwait(false);
                    context.Remove(tokenPair);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    context.Database.CloseConnection();
                    return TokenConverter.GetTokenVm(newToken);
                }
                throw new TokensTimeoutException();
            }
        }
        public async Task<List<TokenVm>> RemoveTokensAsync(long userId, string accessToken, List<long> tokensIds)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var query = context.Tokens.Where(token => token.UserId == userId);
                if (tokensIds != null && tokensIds.Any())
                {
                    var tokensCondition = PredicateBuilder.New<Token>();
                    tokensCondition = tokensIds.Aggregate(tokensCondition,
                        (current, value) => current.Or(opt => opt.Id == value).Expand());
                    query = query.Where(tokensCondition);
                }
                else
                {
                    query = query.Where(token => token.AccessToken == accessToken);
                }
                var tokens = await query.ToListAsync().ConfigureAwait(false);
                context.Tokens.RemoveRange(tokens);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return TokenConverter.GetTokensVm(tokens);
            }
        }
        public async Task<TokenVm> RefreshTokenPairAsync(long userId, string refreshToken)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                Token target = await context.Tokens
                    .FirstOrDefaultAsync(token => token.RefreshToken == refreshToken && token.UserId == userId && !token.User.Deleted)
                    .ConfigureAwait(false);
                if (target != null)
                {
                    target.AccessToken = RandomExtensions.NextString(64);
                    target.RefreshToken = RandomExtensions.NextString(64);
                    target.AccessTokenExpirationTime = DateTime.UtcNow.AddHours(ACCESS_LIFETIME_HOUR).ToUnixTime();
                    target.RefreshTokenExpirationTime = DateTime.UtcNow.AddHours(REFRESH_LIFETIME_HOUR).ToUnixTime();
                    context.Tokens.Update(target);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return TokenConverter.GetTokenVm(target);
                }
                else
                {
                    throw new InvalidTokenException();
                }
            }
        }
        public async Task<List<TokenVm>> GetAllUsersTokensAsync(IEnumerable<long> usersId,
           bool requireDeviceToken = true)
        {
            var tokensCondition = PredicateBuilder.New<Token>();
            if (requireDeviceToken)
            {
                tokensCondition = usersId.Aggregate(tokensCondition,
                    (current, value) => current.Or(opt =>
                        opt.UserId == value
                        && opt.DeviceTokenId != null
                        && opt.AccessTokenExpirationTime > DateTime.UtcNow.ToUnixTime()).Expand());
            }
            else
            {
                tokensCondition = usersId.Aggregate(tokensCondition,
                    (current, value) => current.Or(opt =>
                        opt.UserId == value
                        && opt.AccessTokenExpirationTime > DateTime.UtcNow.ToUnixTime()).Expand());
            }
            using (MessengerDbContext context = contextFactory.Create())
            {
                var tokens = await context.Tokens
                .AsNoTracking()
                .Where(tokensCondition)
                .ToListAsync()
                .ConfigureAwait(false);
                return TokenConverter.GetTokensVm(tokens);
            }
        }
        public async Task SetDeviceTokenIdNullAsync(string deviceTokenId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var existingTokens =
                await context.Tokens.Where(opt => opt.DeviceTokenId == deviceTokenId).ToListAsync().ConfigureAwait(false);
                existingTokens.ForEach(opt => opt.DeviceTokenId = null);
                context.UpdateRange(existingTokens);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        public async Task<TokenVm> UpdateTokenDataAsync(string osName, string deviceName, string appName, string ipAddress, TokenVm tokenVm)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var token = await context.Tokens.FirstOrDefaultAsync(opt =>
                    opt.AccessToken == tokenVm.AccessToken
                    && opt.UserId == tokenVm.UserId).ConfigureAwait(false);
                token.AppName = appName;
                token.DeviceName = deviceName;
                token.OSName = osName;
                token.IPAddress = ipAddress;
                context.Tokens.Update(token);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return TokenConverter.GetTokenVm(token);
            }
        }
    }
}