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
using ObjectsLibrary.Enums;
using System;

namespace NodeApp.MessengerData.Entities
{
    public class ConversationPreview
    {
        public ConversationType ConversationType { get; set; }
        public long ConversationId { get; set; }
        public string Title { get; set; }
        public string Photo { get; set; }
        public string PreviewText { get; set; }
        public int? UnreadedCount { get; set; }
        public long? LastMessageSenderId { get; set; }
        public string LastMessageSenderName { get; set; }
        public long? LastMessageTime { get; set; }
        public long? SecondUserId { get; set; }
        public bool? Read { get; set; }        
        public short[] AttachmentTypes { get; set; }
        public Guid? LastMessageId { get; set; }
        public bool? IsMuted { get; set; }
    }
}
 