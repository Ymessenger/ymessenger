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

namespace NodeApp.SmsServiceClasses.Responses
{
    public class SMSIntelSendSmsResponse
    {     
        public class ResultObject
        {
            public class PriceObject
            {
                [JsonProperty("sum")]
                public float Sum { get; set; }
                [JsonProperty("currencyId")]
                public string Currency { get; set; }
            }
            [JsonProperty("price")]
            public PriceObject Price { get; set; }
            [JsonProperty("successMessagesCount")]
            public int SuccessMessagesCount { get; set; }
            [JsonProperty("errorMessagesCount")]
            public int ErrorMessagesCount { get; set; }
        }
        public class ErrorObject
        {
            [JsonProperty("code")]
            public int ErrorCode { get; set; }
            [JsonProperty("descr")]
            public string Descryption { get; set; }
        }
        [JsonProperty("success")]
        public bool Success { get; set; }
        [JsonProperty("error")]
        public  ErrorObject Error { get; set; }
        [JsonProperty("result")]
        public ResultObject Result { get; set; }

    }
}
