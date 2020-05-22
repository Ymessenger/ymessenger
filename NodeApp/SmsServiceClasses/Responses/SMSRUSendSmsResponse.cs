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
using System.Collections.Generic;

namespace NodeApp.SmsServiceClasses.Responses
{
    public class SMSRUSendSmsResponse
    {
        public class SmsStatus
        {
            [JsonProperty("status_code")]
            public int StatusCode { get; set; }
            [JsonProperty("status")]
            public string Status { get; set; }
            [JsonProperty("status_text")]
            public string StatusText { get; set; }
        }
        [JsonProperty("status_code")]
        public int StatusCode { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("balance")]
        public double Balance { get; set; }
        [JsonProperty("sms")]
        public Dictionary<string, SmsStatus> Sms { get; set; }
    }
}
