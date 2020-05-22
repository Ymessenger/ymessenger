﻿/** 
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

namespace NodeApp.SmsServiceClasses.Responses
{
    public class BSGSmsStatusResponse
    {
        [JsonProperty("error")]
        public int Error { get; set; }
        [JsonProperty("errorDescription")]
        public string ErrorDescription { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("msisdn")]
        public string Msisdn { get; set; }
        [JsonProperty("reference")]
        public string Reference { get; set; }
        [JsonProperty("time_in")]
        public DateTime TimeIn { get; set; }
        [JsonProperty("time_sent")]
        public DateTime TimeSent { get; set; }
        [JsonProperty("time_dr")]
        public DateTime TimeDr { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("price")]
        public float Price { get; set; }
        [JsonProperty("currency")]
        public string Currency { get; set; }
    }
}