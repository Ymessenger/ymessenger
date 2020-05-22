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
using NodeApp.SmsServiceClasses.Requests;
using NodeApp.SmsServiceClasses.Responses;
using ObjectsLibrary;
using ObjectsLibrary.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NodeApp.SmsServiceClasses
{
    public class BSGSmsService : ISmsService
    {
        private readonly BSGServiceConfiguration _configuration;
        public BSGSmsService(SmsServiceConfiguration configuration)
        {
            if(configuration.ServiceName != SmsServiceTypes.BSG)
            {
                throw new ArgumentException($"{nameof(SmsServiceConfiguration.ServiceName)} should be {SmsServiceTypes.BSG}.", nameof(configuration));
            }
            _configuration = (BSGServiceConfiguration) configuration;
        }
        public async Task<bool> SendAsync(string phone, string text)
        {
            phone = phone.TrimStart('+');
            HttpWebRequest request = WebRequest.CreateHttp($"{_configuration.ServiceUrl}/create");
            request.Method = "POST";
            request.Headers.Add("X-API-KEY", _configuration.ApiKey);
            BSGSendSmsRequest smsRequest = new BSGSendSmsRequest(_configuration.SenderName, text, phone, RandomExtensions.NextString(13), 1, null);
            string response = await SendWebRequestAsync(request, smsRequest.ToJson()).ConfigureAwait(false);
            var deserializedResponse = ObjectSerializer.JsonToObject<BSGSingleMessageResponse>(response);
            return deserializedResponse.Result.Error == 0;
        }
        private async Task<string> SendWebRequestAsync(HttpWebRequest request, string content)
        {
            if (content != null)
            {
                using (Stream requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                {
                    byte[] contentArray = Encoding.UTF8.GetBytes(content);
                    await requestStream.WriteAsync(contentArray);
                }
            }
            using (WebResponse response = await request.GetResponseAsync().ConfigureAwait(false))
            {
                List<byte> resultResponseBytes = new List<byte>();
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (BinaryReader reader = new BinaryReader(responseStream))
                    {
                        int readedBytes = 0;
                        byte[] responseBuffer = new byte[1024];
                        while ((readedBytes = reader.Read(responseBuffer, 0, responseBuffer.Length)) > 0)
                        {
                            resultResponseBytes.AddRange(responseBuffer.Take(readedBytes));
                        }
                    }
                }
                return Encoding.UTF8.GetString(resultResponseBytes.ToArray());
            }
        }
    }
}
