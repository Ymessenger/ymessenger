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
using NodeApp.SmsServiceClasses.Responses;
using ObjectsLibrary.Converters;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace NodeApp.SmsServiceClasses
{
    public class SMSRUSmsService : ISmsService
    {
        private readonly SMSRUServiceConfiguration _configuration;
        public SMSRUSmsService(SmsServiceConfiguration configuration)
        {
            if(configuration.ServiceName != SmsServiceTypes.SMSRU)
            {
                throw new ArgumentException($"{nameof(SmsServiceConfiguration.ServiceName)} should be {SmsServiceTypes.SMSRU}.", nameof(configuration));
            }
            _configuration = (SMSRUServiceConfiguration)configuration;
        }
        public async Task<bool> SendAsync(string phone, string text)
        {
            phone = phone.TrimStart('+');
            var requestObject = new SMSRUSendSmsRequest
            {
                Phone = phone,
                Text = text,
                ApiId = _configuration.ApiId,
                Login = _configuration.Login,
                Password = _configuration.Password,
                JsonResponse = 1,
                IsTest = _configuration.Test ? (byte)1 : (byte)0
            };
            HttpWebRequest request = WebRequest.CreateHttp($"{_configuration.SendUrl}?{requestObject.GetQueryString()}");
            request.Method = "GET";
            var response = await request.GetResponseAsync();
            using (var responseStream = response.GetResponseStream())
            {
                using (var streamReader = new StreamReader(responseStream))
                {
                    var responseJson = await streamReader.ReadToEndAsync();
                    var responseObject = ObjectSerializer.JsonToObject<SMSRUSendSmsResponse>(responseJson);
                    return responseObject.StatusCode == 100;
                }
            }
        }
    }
}
