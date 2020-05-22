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
using NodeApp.SmsServiceClasses.ViewModels;
using System.Collections.Generic;

namespace NodeApp.SmsServiceClasses.Requests
{
    public class BSGSendSmsRequest
    {
        [JsonProperty("destination")]
        public string Destination { get; set; }
        [JsonProperty("originator")]
        public string Originator { get; set; }
        [JsonProperty("body")]
        public string Body { get; set; }
        [JsonProperty("msisdn")]
        public string Msisdn { get; set; }
        [JsonProperty("reference")]
        public string Reference { get; set; }  
        [JsonProperty("phones")]
        public IEnumerable<BSGPhoneVm> Phones { get; set; }
        [JsonProperty("validity")]
        public int? Validity { get; set; }
        [JsonProperty("tariff")]
        public int? Tariff { get; set; }

        public BSGSendSmsRequest() { }
        public BSGSendSmsRequest(string originator, string body, string msisdn, string reference, int? validity = null, int? tariff = null)
        {
            Originator = originator;
            Body = body;
            Msisdn = msisdn;
            Reference = reference;
            Validity = validity;
            Tariff = tariff;
            Destination = "phone";
        }
        public BSGSendSmsRequest(string originator, string body, IEnumerable<BSGPhoneVm> phones, int? validity = null, int? tariff = null)
        {
            Originator = originator;
            Body = body;
            Phones = phones;
            Validity = validity;
            Tariff = tariff;
            Destination = "phones";
        }
    }
}
