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
using System.Linq;

namespace NodeApp.CrossNodeClasses.Notices
{
    [Serializable]
    public class MessagesReadNodeNotice : NodeNotice
    {
        public List<Guid> MessagesId { get; set; }       
        public ConversationType ConversationType { get; set; }
        public long ConversationOrUserId { get; set; }
        public long ReaderUserId { get; set; }
        public MessagesReadNodeNotice() { }
        public MessagesReadNodeNotice(IEnumerable<Guid> messagesId, ConversationType conversationType, long conversationOrUserId, long readerUserId)
        {
            MessagesId = messagesId.ToList();
            ConversationType = conversationType;
            ConversationOrUserId = conversationOrUserId;
            ReaderUserId = readerUserId;
            NoticeCode = Enums.NodeNoticeCode.MessagesRead;
        }
    }
}
