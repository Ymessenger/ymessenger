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
    public class SMSIntelServiceConfiguration : SmsServiceConfiguration
    {
        public string AuthToken { get; set; } 
        public string ServiceUrl { get; set; }
        public string SenderName { get; set; }
        public override bool IsValid()
        {
            return this != null
                && !string.IsNullOrWhiteSpace(AuthToken)                 
                && !string.IsNullOrWhiteSpace(ServiceUrl);
        }
    }
}
