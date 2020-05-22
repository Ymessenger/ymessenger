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
using System.Collections.Generic;

namespace NodeApp.MessengerData.DataTransferObjects
{
    [Serializable]
    public class MessageDto : ICloneable
    {
        public long SendingTime { get; set; }
        public long? SenderId { get; set; }
        public long? ReceiverId { get; set; }
        public Guid? Replyto { get; set; }
        public string Text { get; set; }
        public long ConversationId { get; set; }
        public ConversationType ConversationType { get; set; }
        public bool Read { get; set; }        
        public bool Deleted { get; set; }
        public long? UpdatedAt { get; set; }
        public Guid GlobalId { get; set; }
        public List<AttachmentDto> Attachments { get; set; }
        public long? Lifetime { get; set; }
        public List<long> NodesIds { get; set; }
        public MessageDto()
        {

        }

        public MessageDto(MessageDto message)
        {
            SenderId = message.SenderId;
            SendingTime = message.SendingTime;
            ReceiverId = message.ReceiverId;
            Replyto = message.Replyto;
            Text = message.Text;
            ConversationId = message.ConversationId;
            ConversationType = message.ConversationType;
            Read = message.Read;
            Deleted = message.Deleted;
            UpdatedAt = message.UpdatedAt;
            GlobalId = message.GlobalId;
            Attachments = message.Attachments;
            Lifetime = message.Lifetime;
            NodesIds = message.NodesIds;
        }

        public object Clone()
        {
            return new MessageDto
            {
                Attachments = Attachments,
                ConversationId = ConversationId,
                ConversationType = ConversationType,
                Deleted = Deleted,
                GlobalId = GlobalId,
                Lifetime = Lifetime,
                NodesIds = NodesIds,
                Read = Read,
                ReceiverId = ReceiverId,
                Replyto = Replyto,
                SenderId = SenderId,
                SendingTime = SendingTime,
                Text = Text,
                UpdatedAt = UpdatedAt
            };
        }
    }
}
