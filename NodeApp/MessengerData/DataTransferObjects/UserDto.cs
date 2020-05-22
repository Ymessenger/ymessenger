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
using System;
using System.Collections;
using System.Collections.Generic;

namespace NodeApp.MessengerData.DataTransferObjects
{
    [Serializable]
    public class UserDto
    {
        public long Id { get; set; }
        public string NameFirst { get; set; }
        public string NameSecond { get; set; }
        public string Tag { get; set; }
        public string About { get; set; }
        public string Photo { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public DateTime? Birthday { get; set; }
        public string Language { get; set; }        
        public long? Online { get; set; }        
        public string Password { get; set; }
        public long? RegistrationDate { get; set; }
        public long? NodeId { get; set; }        
        public int? ContactsPrivacy { get; set; }
        public int Privacy { get; set; }
        public bool Deleted { get; set; }
        public bool Confirmed { get; set; }
        public bool Banned { get; set; }
        public byte[] PasswordHash { get; set; }
        public bool SyncContacts { get; set; }
        public BitArray[] Security { get; set; }        
        public List<string> Phones { get; set; }
        public List<string> Emails { get; set; }
        public List<long> Blacklist { get; set; }
        public List<TokenDto> Tokens { get; set; }
        public List<ChatDto> Chats { get; set; }
        public List<DialogDto> Dialogs { get; set; }
        public List<ChannelDto> Channels { get; set; }
        public List<FileInfoDto> FilesInfo { get; set; }
        public List<KeyDto> Keys { get; set; }
        public List<ContactDto> Contacts { get; set; }
        public List<GroupDto> ContactGroups { get; set; }
        public List<UserFavoritesDto> Favorites { get; set; }       
    }
}
