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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Objects.SettingsObjects.SmsServicesConfiguration;
using NodeApp.SmsServiceClasses.Responses;
using ObjectsLibrary.Converters;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NodeApp.SmsServiceClasses
{
    public class SMSIntelSmsService : ISmsService
    {
        private readonly SMSIntelServiceConfiguration _configuration;
        public SMSIntelSmsService(SmsServiceConfiguration configuration)
        {
            if(configuration.ServiceName != SmsServiceTypes.SMSIntel)
            {
                throw new ArgumentException($"{nameof(SmsServiceConfiguration.ServiceName)} should be {SmsServiceTypes.SMSRU}.", nameof(configuration));
            }
            _configuration = (SMSIntelServiceConfiguration)configuration;
        }
        public async Task<bool> SendAsync(string phone, string text)
        {
            phone = phone.TrimStart('+');
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Token", _configuration.AuthToken);
            var requestObject = new
            {
                messages = new List<dynamic>
               {
                   new
                   {
                       recipient = phone,
                       text
                   }
               }
            };
            var requestJson = requestObject.ToJson();
            StringContent stringContent = new StringContent(requestJson);
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await httpClient.PostAsync(_configuration.ServiceUrl, stringContent);
            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObject = ObjectSerializer.JsonToObject<SMSIntelSendSmsResponse>(responseJson);
            return responseObject.Success;            
        }
    }
}
