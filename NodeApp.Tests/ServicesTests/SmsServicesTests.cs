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
using NodeApp.Objects.SettingsObjects.SmsServicesConfiguration;
using NodeApp.SmsServiceClasses;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests.ServicesTests
{
    public class SmsServicesTests
    {
        [Fact]
        public async Task SMSRUSendSmsTest()
        {
            SmsServiceConfiguration configuration = new SMSRUServiceConfiguration
            {
                ApiId = "051182F3-7898-AC6D-8233-803B74BF10ED",
                ServiceName = SmsServiceTypes.SMSRU,
                PartnerId = "295663",
                Test = true
            };
            ISmsService service = new SMSRUSmsService(configuration);
            Assert.True(await service.SendAsync("79131743886", "TEST-ТЕСТ"));
        }
        [Fact]
        public async Task SMSIntelSendSmsTest()
        {
            SmsServiceConfiguration configuration = new SMSIntelServiceConfiguration
            {
                ServiceName = SmsServiceTypes.SMSIntel,
                AuthToken = "vie77292oqujo21gj1yr0gmdnlc10ym4z6kds6nhg34g1rezwc3qqgobxav9xz1a",
                ServiceUrl = "https://lcab.smsint.ru/json/v1.0/sms/send/text"
            };
            ISmsService service = new SMSIntelSmsService(configuration);
            Assert.True(await service.SendAsync("79131743886", "TEST-ТЕСТ"));
        }
        [Fact]
        public async Task BSGSendSmsTest()
        {
            SmsServiceConfiguration configuration = new BSGServiceConfiguration
            {
                ApiKey = "live_dB92BUU13zBbQ77MVSHH",
                SenderName = "SERVIX.io",
                ServiceUrl = "https://app.bsg.hk/rest/sms",
                ServiceName = SmsServiceTypes.BSG
            };
            ISmsService service = new BSGSmsService(configuration);
            Assert.True(await service.SendAsync("79131743886", "Verification code: 6563"));
        }
    }
}
