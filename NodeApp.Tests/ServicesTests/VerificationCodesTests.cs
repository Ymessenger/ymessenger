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
using NodeApp.Interfaces.Services;
using ObjectsLibrary;
using System;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class VerificationCodesTests
    {
        
        private readonly IVerificationCodesService verificationCodesService;
        
        public VerificationCodesTests()
        {
            var testsData = TestsData.Create(nameof(VerificationCodesTests));
            verificationCodesService = testsData.AppServiceProvider.VerificationCodesService;            
        }
        [Fact]
        public async Task CreateAndGetVCode()
        {
            string key = RandomExtensions.NextString(10);
            var code = await verificationCodesService.CreateVerificationCodeAsync(DateTime.UtcNow.ToUnixTime(), key);
            var expectedCode = await verificationCodesService.GetUserVerificationCodeAsync(key);            
            Assert.Equal(code, expectedCode.VCode);
        }
        [Fact] 
        public async Task CheckVCode()
        {
            string key = RandomExtensions.NextString(10);
            var code = await verificationCodesService.CreateVerificationCodeAsync(DateTime.UtcNow.ToUnixTime(), key);
            Assert.True(await verificationCodesService.IsVerificationCodeValidAsync(key, null, code));
            Assert.False(await verificationCodesService.IsVerificationCodeValidAsync(key, null, RandomExtensions.NextInt16(1000, 9999)));
        }
        [Fact]
        public async Task CanRequestSmsCode()
        {
            string key = RandomExtensions.NextString(10);
            await verificationCodesService.CreateVerificationCodeAsync(DateTime.UtcNow.ToUnixTime(), key);
            Assert.False(await verificationCodesService.CanRequestSmsCodeAsync(DateTime.UtcNow.ToUnixTime(), key));            
            Assert.True(await verificationCodesService.CanRequestSmsCodeAsync(DateTime.UtcNow.AddSeconds(31).ToUnixTime(), key));
            await verificationCodesService.CreateVerificationCodeAsync(DateTime.UtcNow.AddSeconds(31).ToUnixTime(), key);
            Assert.False(await verificationCodesService.CanRequestSmsCodeAsync(DateTime.UtcNow.AddSeconds(70).ToUnixTime(), key));
            Assert.True(await verificationCodesService.CanRequestSmsCodeAsync(DateTime.UtcNow.AddSeconds(95).ToUnixTime(), key));
        }
    }
}
