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
using NodeApp.SmsServiceClasses.Requests;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NodeApp.SmsServiceClasses
{
    public class GolosAlohaSmsService : ISmsService
    {
        private readonly VoiceServiceConfiguration _configuration;
        public GolosAlohaSmsService(SmsServiceConfiguration configuration)
        {
            if(configuration.ServiceName != SmsServiceTypes.GolosAloha)
            {
                throw new ArgumentException($"{nameof(SmsServiceConfiguration.ServiceName)} should be {SmsServiceTypes.GolosAloha}.", nameof(configuration));
            }
            _configuration = (VoiceServiceConfiguration) configuration;
        }
        public async Task<bool> SendAsync(string phone, string text)
        {
            phone = phone.TrimStart('+');
            GolosAlohaSmsSendRequest request = new GolosAlohaSmsSendRequest 
            {
                ApiKey = _configuration.ApiKey,
                OutgoingPhone = _configuration.OutgoingPhone,
                Phone = phone,
                Record = new GolosAlohaSmsSendRequest.GolosAlohaRecord
                {
                    Gender = 0,
                    Text = text
                }
            };
            HttpClient client = new HttpClient();
            var response = await client.PostAsJsonAsync(_configuration.ServiceUrl, request);
            return response.IsSuccessStatusCode;
        }
    }
}
