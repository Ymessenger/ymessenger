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

namespace NodeApp.CrossNodeClasses.Requests
{
    [Serializable]
    public class GetMessagesNodeRequest : NodeRequest
    {        
        public ConversationType ConversationType { get; set; }
        public long ConversationId { get; set; }
        public Guid? MessageId { get; set; }
        public bool? Direction { get; set; }
        public int Length { get; set; }      
        public List<AttachmentType> AttachmentsTypes { get; set; }
        public GetMessagesNodeRequest() { }
        public GetMessagesNodeRequest(ConversationType conversationType, long conversationId,  Guid? messageId, List<AttachmentType> attachmentsTypes, bool? direction, int length)
        {
            Length = length;
            Direction = direction;
            MessageId = messageId;
            ConversationType = conversationType;
            ConversationId = conversationId;
            AttachmentsTypes = attachmentsTypes;
            RequestType = Enums.NodeRequestType.GetMessages;
        }       
    }
}
