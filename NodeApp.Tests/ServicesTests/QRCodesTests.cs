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
using NodeApp.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class QRCodesTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IQRCodesService codesService;
        public QRCodesTests()
        {
            TestsData testsData = TestsData.Create(nameof(QRCodesTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            codesService = testsData.AppServiceProvider.QRCodesService;
        }
        [Fact]
        public async Task CreateQrCode()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var qrCode = await codesService.CreateQRCodeAsync(user.Id, 1);
            Assert.True(!string.IsNullOrEmpty(qrCode.Sequence) && qrCode.UserId == user.Id);
        }
        [Fact]
        public async Task CreateTokens()
        {            
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var qrCode = await codesService.CreateQRCodeAsync(user.Id, 1);
            Assert.NotNull(await codesService.CreateTokenByQRCodeAsync(qrCode));
        }
    }
}
