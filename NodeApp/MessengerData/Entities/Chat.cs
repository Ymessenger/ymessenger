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
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System;
using NpgsqlTypes;

namespace NodeApp.MessengerData.Entities
{
    public partial class Chat
    {
        public Chat()
        {
            ChatUsers = new HashSet<ChatUser>();
            Messages = new HashSet<Message>();
            Favorites = new HashSet<UserFavorite>();
        }

        public long Id { get; set; }
        public string Name { get; set; }
        [MaxLength(100)]
        public string Tag { get; set; }
        public string Photo { get; set; }
        public string About { get; set; }
        public BitArray Visible { get; set; }
        public BitArray Public { get; set; }
        public bool Deleted { get; set; }
        public long? LastMessageId { get; set; }
        public Guid? LastMessageGlobalId { get; set; }
        public long[] NodesId { get; set; }
        public short Type { get; set; }
        public BitArray Security { get; set; }

        public NpgsqlTsVector SearchVector { get; set; }

        public Message LastMessage { get; set; }
        public Key Key { get; set; }
        public ICollection<ChatUser> ChatUsers { get; set; }
        public ICollection<Message> Messages { get; set; }
        public ICollection<UserFavorite> Favorites { get; set; }
    }
}
