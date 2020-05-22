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
using NodeApp.Interfaces.Services;
using NodeApp.MessengerData.Services;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class TokensTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly ITokensService tokensService;
        private readonly IVerificationCodesService verificationCodesService;
        public TokensTests()
        {
            TestsData testsData = TestsData.Create(nameof(TokensTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            tokensService = testsData.AppServiceProvider.TokensService;
            verificationCodesService = testsData.AppServiceProvider.VerificationCodesService;
        }
        [Fact]
        public async Task PhoneVCodeCreateToken()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var vcode = await verificationCodesService.CreateVerificationCodeAsync(DateTime.UtcNow.ToUnixTime(), user.Id.ToString(), user.Id);
            await Assert.ThrowsAsync<WrongVerificationCodeException>(async() => await tokensService.PhoneVCodeCreateTokenPairAsync(user.Phones.FirstOrDefault().PhoneNumber, -1000, null));
            Assert.NotNull(await tokensService.PhoneVCodeCreateTokenPairAsync(user.Phones.FirstOrDefault().PhoneNumber, vcode, null));            
            await Assert.ThrowsAsync<UserNotFoundException>(async () => await tokensService.PhoneVCodeCreateTokenPairAsync("+19996660000", 0, null));
        }
        [Fact]
        public async Task EmailVCodeCreateToken()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var vcode = await verificationCodesService.CreateVerificationCodeAsync(DateTime.UtcNow.ToUnixTime(), user.Id.ToString(), user.Id);
            await Assert.ThrowsAsync<CreateTokenPairException>(async () => await tokensService.EmailVCodeCreateTokenPairAsync(user.Emails.FirstOrDefault().EmailAddress, -1000, null));
            Assert.NotNull(await tokensService.EmailVCodeCreateTokenPairAsync(user.Emails.FirstOrDefault().EmailAddress, vcode, null));
            await Assert.ThrowsAsync<CreateTokenPairException>(async () => await tokensService.EmailVCodeCreateTokenPairAsync("email@list.com", 0, null));
        }
        [Fact]
        public async Task UserIdVCodeCreateToken()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var vcode = await verificationCodesService.CreateVerificationCodeAsync(DateTime.UtcNow.ToUnixTime(), user.Id.ToString(), user.Id);
            await Assert.ThrowsAsync<UserNotFoundException>(async () => await tokensService.UserIdVCodeCreateTokenPairAsync(0, vcode, null));
            Assert.NotNull(await tokensService.UserIdVCodeCreateTokenPairAsync(user.Id, vcode, null));
            await Assert.ThrowsAsync<WrongVerificationCodeException>(async () => await tokensService.UserIdVCodeCreateTokenPairAsync(user.Id, -1000, null));
        }
        [Fact]
        public async Task EmailPasswordCreateToken()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            await Assert.ThrowsAsync<UserNotFoundException>(async() => await tokensService.EmailPasswordCreateTokenPairAsync("fake_mail", "password", null));
            await Assert.ThrowsAsync<UserNotFoundException>(async() => await tokensService.EmailPasswordCreateTokenPairAsync(user.Emails.FirstOrDefault().EmailAddress, "fake_password", null));
            Assert.NotNull(await tokensService.EmailPasswordCreateTokenPairAsync(user.Emails.FirstOrDefault().EmailAddress, "password", null));
        }
        [Fact]
        public async Task PhonePasswordCreateToken()
        {
            var user = fillTestDbHelper.Users.Skip(1).FirstOrDefault();
            await Assert.ThrowsAsync<UserNotFoundException>(async() => await tokensService.PhonePasswordCreateTokenPairAsync(user.Phones.FirstOrDefault().PhoneNumber, "fake_password", null));
            await Assert.ThrowsAsync<UserNotFoundException>(async() => await tokensService.PhonePasswordCreateTokenPairAsync("fake_phone", "password", null));
            Assert.NotNull(await tokensService.PhonePasswordCreateTokenPairAsync(user.Phones.FirstOrDefault().PhoneNumber, "password", null));
        }
        [Fact]
        public async Task CheckToken()
        {
            var user = fillTestDbHelper.Users.Skip(2).FirstOrDefault();
            var token = await tokensService.PhonePasswordCreateTokenPairAsync(user.Phones.FirstOrDefault().PhoneNumber, "password", null);
            Assert.NotNull(await tokensService.CheckTokenAsync(token.FirstValue, user.NodeId.Value));
        }
        [Fact]
        public async Task RemoveTokens()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var token = await tokensService.CreateTokenPairByUserIdAsync(user.Id);
            var removedToken = await tokensService.RemoveTokensAsync(user.Id, token.AccessToken, new List<long> { token.Id.Value });            
            await Assert.ThrowsAsync<InvalidTokenException>(async () => await tokensService.CheckTokenAsync(token, user.NodeId.Value));
        }
        [Fact]
        public async Task UserIdPasswordCreateToken()
        {
            var user = fillTestDbHelper.Users.Skip(3).FirstOrDefault();
            await Assert.ThrowsAsync<UserNotFoundException>(async() => await tokensService.UserIdPasswordCreateTokenPairAsync(user.Id, "fake_password", null));
            await Assert.ThrowsAsync<UserNotFoundException>(async() => await tokensService.UserIdPasswordCreateTokenPairAsync(long.MinValue, "password", null));
            Assert.NotNull(await tokensService.UserIdPasswordCreateTokenPairAsync(user.Id, "password", null));
        }       
        [Fact]
        public async Task UpdateTokenData()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var expectedToken = await tokensService.CreateTokenPairByUserIdAsync(user.Id);
            expectedToken = await tokensService.UpdateTokenDataAsync("WINDOWS", "PC", "TESTS", null, expectedToken);
            var actualToken = fillTestDbHelper.Users.FirstOrDefault(opt => opt.Id == user.Id).Tokens.FirstOrDefault(opt => opt.Id == expectedToken.Id);
            Assert.True(expectedToken.OSName == actualToken.OSName 
                && expectedToken.DeviceName == actualToken.DeviceName
                && expectedToken.AppName == actualToken.AppName);
        }
        [Fact]
        public async Task GetAllUserTokens()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var tokens = new List<TokenVm>
            {
                await tokensService.CreateTokenPairByUserIdAsync(user.Id),
                await tokensService.CreateTokenPairByUserIdAsync(user.Id)
            };
            var expectedTokens = fillTestDbHelper.Users
                .FirstOrDefault(opt => opt.Id == user.Id)
                .Tokens;
            var actualTokens = await tokensService.GetAllUsersTokensAsync(new List<long> { user.Id }, false);
            Assert.Contains(actualTokens, opt => expectedTokens.Any(p => p.Id == opt.Id));
        }
        [Fact]
        public async Task SetDeviceIdNull()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            string deviceTokenId = RandomExtensions.NextString(32);
            var vcode = await verificationCodesService.CreateVerificationCodeAsync(DateTime.UtcNow.ToUnixTime(), user.Id.ToString(), user.Id);
            var token = await tokensService.PhoneVCodeCreateTokenPairAsync(user.Phones.FirstOrDefault().PhoneNumber, vcode, deviceTokenId);
            await tokensService.SetDeviceTokenIdNullAsync(deviceTokenId);
            var actualToken = fillTestDbHelper.Users.FirstOrDefault(opt => opt.Id == user.Id).Tokens.FirstOrDefault(opt => opt.Id == token.Id);
            Assert.Null(actualToken.DeviceTokenId);
        }
        [Fact]
        public async Task RefreshToken()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var vcode = await verificationCodesService.CreateVerificationCodeAsync(DateTime.UtcNow.ToUnixTime(), user.Id.ToString(), user.Id);
            var token = await tokensService.PhoneVCodeCreateTokenPairAsync(user.Phones.FirstOrDefault().PhoneNumber, vcode, null);
            await Assert.ThrowsAsync<InvalidTokenException>(async() => await tokensService.RefreshTokenPairAsync(user.Id, "bad token"));
            Assert.NotNull(await tokensService.RefreshTokenPairAsync(user.Id, token.RefreshToken));
        }
    }
}