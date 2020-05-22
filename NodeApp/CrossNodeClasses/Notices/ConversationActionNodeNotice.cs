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
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.Notices
{
    [Serializable]
    public class ConversationActionNodeNotice : NodeNotice
    {
        public ConversationAction Action { get; set; }
        public ConversationType ConversationType { get; set; }
        public long ConversationId { get; set; }
        public long UserId { get; set; }
        public long? DialogUserId { get; set; }
        public ConversationActionNodeNotice(ConversationType conversationType, ConversationAction action, long conversationId, long userId, long? dialogUserId)
        {
            Action = action;
            ConversationType = conversationType;
            ConversationId = conversationId;
            UserId = userId;
            DialogUserId = dialogUserId;
            NoticeCode = Enums.NodeNoticeCode.ConversationAction;
        }
    }
}
