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
using System;

namespace NodeApp.MessengerData.Entities
{
    public partial class Dialog
    {
        public Dialog()
        {
            Messages = new HashSet<Message>();
        }

        public long Id { get; set; }
        public long FirstUID { get; set; }
        public long SecondUID { get; set; }
        public BitArray Security { get; set; }
        public long? LastMessageId { get; set; }
        public Guid? LastMessageGlobalId { get; set; }
        public bool IsMuted { get; set; }

        public User FirstU { get; set; }
        public Message LastMessage { get; set; }
        public User SecondU { get; set; }
        public ICollection<Message> Messages { get; set; }
    }
}
