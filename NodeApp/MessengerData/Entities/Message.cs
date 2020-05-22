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
    public partial class Message
    {
        public Message()
        {
            Attachments = new HashSet<Attachment>();
            Chats = new HashSet<Chat>();
            Dialogs = new HashSet<Dialog>();
            InverseReplytoNavigation = new HashSet<Message>();
        }

        public long Id { get; set; }
        public long SendingTime { get; set; }
        public long? SenderId { get; set; }
        public long? ReceiverId { get; set; }
        public Guid? Replyto { get; set; }
        public string Text { get; set; }
        public long? ChatId { get; set; }
        public long? DialogId { get; set; }
        public long? ChannelId { get; set; }
        public bool Read { get; set; }
        public long? SameMessageId { get; set; }
        public bool Deleted { get; set; }
        public long? UpdatedAt { get; set; }
        public long? ExpiredAt { get; set; }
        public long[] NodesIds { get; set; }
        public Guid GlobalId { get; set; }
        public NpgsqlTsVector SearchVector { get; set; }
        public Chat Chat { get; set; }
        public Dialog Dialog { get; set; }
        public Message ReplytoNavigation { get; set; }
        public User Sender { get; set; }
        public User Receiver { get; set; }
        public Channel Channel { get; set; }
        public ICollection<Attachment> Attachments { get; set; }
        public ICollection<Chat> Chats { get; set; }
        public ICollection<Dialog> Dialogs { get; set; }
        public ICollection<Message> InverseReplytoNavigation { get; set; }     
        public ICollection<EditedMessage> EditedMessages { get; set; }
    }
}
