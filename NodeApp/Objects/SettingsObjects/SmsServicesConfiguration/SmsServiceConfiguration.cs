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

namespace NodeApp.Objects.SettingsObjects.SmsServicesConfiguration
{
    public static class SmsServiceTypes
    {
        public const string BSG  = "BSG";
        public const string SMSIntel = "SMSIntel";
        public const string SMSRU = "SMSRU";
        public const string GolosAloha = "GolosAloha";
    }
    public class SmsServiceConfiguration    
    {
       
        public string ServiceName { get; set; }
        public virtual bool IsValid() 
        {
            return this!=null && !string.IsNullOrWhiteSpace(ServiceName);
        }
    }
}
