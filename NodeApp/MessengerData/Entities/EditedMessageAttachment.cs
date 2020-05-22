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
using ObjectsLibrary.Enums;

namespace NodeApp.MessengerData.Entities
{
    public class EditedMessageAttachment
    {
        public long RecordId { get; set; }
        public long ActualAttachmentId { get; set; }
        public long EditedMessageId { get; set; }
        public AttachmentType AttachmentType { get; set; }
        public string Payload { get; set; }
        public byte[] Hash { get; set; }

        public Attachment ActualAttachment { get; set; }
        public EditedMessage Message { get; set; }
    }
}
