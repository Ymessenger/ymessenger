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
using NpgsqlTypes;
using System;
using System.Collections.Generic;

namespace NodeApp.MessengerData.Entities
{
    public class Channel
    {
        public Channel()
        {
            Messages = new HashSet<Message>();
            ChannelUsers = new HashSet<ChannelUser>();
            Favorites = new HashSet<UserFavorite>();
        }
        public long ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string Tag { get; set; }
        public string Photo { get; set; }
        public string About { get; set; }
        public long? LastMessageId { get; set; }
        public Guid? LastMessageGlobalId { get; set; }
        public long[] NodesId { get; set; }
        public bool Deleted { get; set; }
        public long CreationTime { get; set; }

        public NpgsqlTsVector SearchVector { get; set; }

        public Message LastMessage { get; set; }
        public ICollection<ChannelUser> ChannelUsers { get; set; }
        public ICollection<Message> Messages { get; set; }
        public ICollection<UserFavorite> Favorites { get; set; }
    }
}
