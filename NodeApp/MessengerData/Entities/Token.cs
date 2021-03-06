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
using System.ComponentModel.DataAnnotations;

namespace NodeApp.MessengerData.Entities
{
    public partial class Token
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }     
        [MaxLength(1000)]
        public string DeviceTokenId { get; set; }
        public long AccessTokenExpirationTime { get; set; }
        public long? RefreshTokenExpirationTime { get; set; }        
        public string OSName { get; set; }
        public string DeviceName { get; set; }
        public string AppName { get; set; }
        public string IPAddress { get; set; }
        public long LastActivityTime { get; set; }
        public User User { get; set; }
    }
}
