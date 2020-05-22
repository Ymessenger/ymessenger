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
using System;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;

namespace NodeApp.MessengerData.Entities{
    
    [Serializable]
    public partial class ChatUser
    {
        public long UserId { get; set; }
        public long ChatId { get; set; }
        public UserRole UserRole { get; set; }
        public bool Deleted { get; set; }
        public bool Banned { get; set; }
        public long? Joined { get; set; }
        public long? LastReadedChatMessageId { get; set; }
        public Guid? LastReadedGlobalMessageId { get; set; }
        public long? InviterId { get; set; } 
        public bool IsMuted { get; set; }

        public Chat Chat { get; set; }
        public User User { get; set; }
        public User Inviter { get; set; }
        public ChatUser() { }
        public ChatUser(ChatUserVm chatUser)
        {
            UserId = chatUser.UserId;
            ChatId = chatUser.ChatId ?? 0;
            UserRole = chatUser.UserRole ?? UserRole;
            Deleted = chatUser.Deleted ?? Deleted;
            Banned = chatUser.Banned ?? Banned;        
        }
    }
}
