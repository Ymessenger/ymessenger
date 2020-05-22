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

namespace NodeApp.MessengerData.Entities
{
    public partial class Attachment
    {
        public Attachment()
        {
            EditedMessageAttachments = new HashSet<EditedMessageAttachment>();
        }
        public long Id { get; set; }
        public short Type { get; set; }
        public byte[] Hash { get; set; }
        public long MessageId { get; set; }
        public string Payload { get; set; }
        public ICollection<EditedMessageAttachment> EditedMessageAttachments { get; set; }
        public Message Message { get; set; }
    }
}
