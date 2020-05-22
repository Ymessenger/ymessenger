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
using Newtonsoft.Json;

namespace NodeApp.SmsServiceClasses.Requests
{
    public class SMSRUSendSmsRequest
    {
        [JsonProperty("to")]
        public string Phone { get; set; }
        [JsonProperty("msg")]
        public string Text { get; set; }
        [JsonProperty("from")]
        public string SenderName { get; set; }
        [JsonProperty("ip")]
        public string RequestorIP { get; set; }
        [JsonProperty("time")]
        public long? SentAt { get; set; }
        [JsonProperty("ttl")]
        public short? TTL { get; set; }
        [JsonProperty("translit")]
        public byte? Translit { get; set; }
        [JsonProperty("test")]
        public byte? IsTest { get; set; }
        [JsonProperty("partner_id")]
        public string PartnerId { get; set; }
        [JsonProperty("api_id")]
        public string ApiId { get; set; }
        [JsonProperty("login")]
        public string Login { get; set; }
        [JsonProperty("password")]
        public string Password { get; set; }
        [JsonProperty("json")]
        public byte JsonResponse { get; set; }

        public string GetQueryString()
        {
            var queryString = $"api_id={ApiId}&to={Phone}&msg={Text}";
            if (!string.IsNullOrWhiteSpace(SenderName))
            {
                queryString += $"&from={SenderName}";
            }
            if (!string.IsNullOrWhiteSpace(RequestorIP))
            {
                queryString += $"&ip={RequestorIP}";
            }
            if (SentAt != null)
            {
                queryString += $"&time={SentAt}";
            }
            if (TTL != null)
            {
                queryString += $"&ttl={TTL}";
            }
            if (Translit != null)
            {
                queryString += $"&translit={Translit}";
            }
            if (!string.IsNullOrWhiteSpace(PartnerId))
            {
                queryString += $"&partner_id={PartnerId}";
            }       
            if(IsTest != null)
            {
                queryString += $"&test={IsTest}";
            }
            queryString += $"&json={JsonResponse}";
            return queryString;
        }
    }
}
