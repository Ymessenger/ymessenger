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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.SmsServiceClasses.Requests
{
    public class GolosAlohaSmsSendRequest
    {
        public class GolosAlohaRecord
        {
            [JsonProperty("id")]
            public int? Id { get; set; }
            [JsonProperty("text")]
            public string Text { get; set; }
            [JsonProperty("gender")]
            public byte Gender { get; set; }
        }
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }
        [JsonProperty("phone")]
        public string Phone { get; set; }
        [JsonProperty("outgoingPhone")]
        public string OutgoingPhone { get; set; }
        [JsonProperty("record")]
        public GolosAlohaRecord Record { get; set; }
    }
}
