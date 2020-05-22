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
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Entities
{
    public class EditedMessage
    { 
        public long RecordId { get; set; }
        public long EditorId { get; set; }
        public long MessageId { get; set; }               
        public string Text { get; set; }         
        public long? UpdatedTime { get; set; }
        public long SendingTime { get; set; }
        public byte[] Sign { get; set; }
        public List<EditedMessageAttachment> Attachments { get; set; }
        public Message ActualMessage { get; set; }

        public EditedMessage(Message message, long editorId)
        {
            EditorId = editorId;
            MessageId = message.Id;            
            UpdatedTime = message.UpdatedAt;
            SendingTime = message.SendingTime;
            Text = message.Text;
            Attachments = message.Attachments?.Select(attachment => new EditedMessageAttachment
            {
                ActualAttachmentId = attachment.Id,
                AttachmentType = (AttachmentType) attachment.Type,
                EditedMessageId = message.Id,
                Hash = attachment.Hash,
                Payload = attachment.Payload
            }).ToList();
        }
        public EditedMessage() { }
    }
}
