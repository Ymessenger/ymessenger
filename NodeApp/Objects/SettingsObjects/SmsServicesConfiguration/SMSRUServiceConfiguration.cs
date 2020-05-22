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
    public class SMSRUServiceConfiguration : SmsServiceConfiguration
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string ApiId { get; set; }
        public string PartnerId { get; set; }
        public bool Test { get; set; }
        public string TokenUrl { get; set; } = "https://sms.ru/auth/get_token";
        public string SendUrl { get; set; } = "https://sms.ru/sms/send";
        public string StatusUrl { get; set; } = "https://sms.ru/sms/status";
        public string CostUrl { get; set; } = "https://sms.ru/sms/cost";
        public string BalanceUrl { get; set; } = "https://sms.ru/my/balance";
        public string LimitUrl { get; set; } = "https://sms.ru/my/limit";
        public string SendersUrl { get; set; } = "https://sms.ru/my/senders";
        public string AuthUrl { get; set; } = "https://sms.ru/auth/check";

        public override bool IsValid()
        {
            return this != null
                && (!string.IsNullOrWhiteSpace(Login) && !string.IsNullOrWhiteSpace(Password)
                    || !string.IsNullOrWhiteSpace(ApiId));
        }
    }
}
