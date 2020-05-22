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
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.Converters
{
    public static class ChatUserConverter
    {
        public static List<ChatUserVm> GetChatUsersVm(IEnumerable<ChatUser> chatUsers)
        {
            return chatUsers?.Select(GetChatUserVm).ToList();
        }

        public static ChatUserVm GetChatUserVm(ChatUser chatUser)
        {
            return chatUser == null
                ? null
                : new ChatUserVm
                {
                    ChatId = chatUser.ChatId,
                    Banned = chatUser.Banned,
                    Deleted = chatUser.Deleted,
                    UserId = chatUser.UserId,
                    UserRole = chatUser.UserRole,
                    UserInfo = chatUser.User != null
                           ? UserConverter.GetUserVm(chatUser.User)
                           : null,
                    Joined = chatUser.Joined,
                    IsMuted = chatUser.IsMuted
                };
        }

        private static ChatUser GetChatUser(ChatUserDto chatUser)
        {
            return new ChatUser
            {
                Banned = chatUser.Banned,
                ChatId = chatUser.ChatId,
                Deleted = chatUser.Deleted,
                InviterId = chatUser.InviterId,
                Joined = chatUser.Joined,
                UserId = chatUser.UserId,
                UserRole = chatUser.UserRole,
                IsMuted = chatUser.IsMuted
            };
        }

        public static List<ChatUser> GetChatUsers(IEnumerable<ChatUserDto> chatUsers) => chatUsers?.Select(GetChatUser).ToList();

        public static List<ChatUser> GetChatUsers(IEnumerable<ChatUserVm> chatUsers) => chatUsers?.Select(GetChatUser).ToList();

        private static ChatUser GetChatUser(ChatUserVm chatUser)
        {
            return chatUser == null
                ? null
                : new ChatUser
                {
                    Banned = chatUser.Banned ?? false,
                    Deleted = chatUser.Deleted ?? false,
                    ChatId = chatUser.ChatId ?? 0,
                    UserId = chatUser.UserId,
                    UserRole = chatUser.UserRole ?? UserRole.User,
                    InviterId = chatUser.InviterId,
                    Joined = chatUser.Joined,
                    IsMuted = chatUser.IsMuted.GetValueOrDefault()
                };
        }

        public static ChatUser GetNewChatUser(long chatId, long userId, long? inviterId)
        {
            return new ChatUser
            {
                ChatId = chatId,
                UserId = userId,
                InviterId = inviterId,
                Banned = false,
                Deleted = false,
                Joined = DateTime.UtcNow.ToUnixTime(),
                UserRole = UserRole.User,
                IsMuted = false
            };
        }

        public static ChatUserDto GetChatUserDto(ChatUser chatUser)
        {
            return new ChatUserDto
            {
                Banned = chatUser.Banned,
                Deleted = chatUser.Deleted,
                ChatId = chatUser.ChatId,
                InviterId = chatUser.InviterId,
                Joined = chatUser.Joined,
                LastReadedGlobalMessageId = chatUser.LastReadedGlobalMessageId,
                UserId = chatUser.UserId,
                UserRole = chatUser.UserRole,
                IsMuted = chatUser.IsMuted
            };
        }

        public static ChatUserVm GetChatUserVm(ChatUserDto chatUser)
        {
            return new ChatUserVm
            {
                Banned = chatUser.Banned,
                ChatId = chatUser.ChatId,
                Deleted = chatUser.Deleted,
                InviterId = chatUser.InviterId,
                Joined = chatUser.Joined,
                UserId = chatUser.UserId,
                UserRole = chatUser.UserRole,
                IsMuted = chatUser.IsMuted
            };
        }

        public static List<ChatUserVm> GetChatUsersVm(List<ChatUserDto> chatUsers) => chatUsers?.Select(GetChatUserVm).ToList();

        public static List<ChatUserDto> GetChatUsersDto(IEnumerable<ChatUser> chatUsers) => chatUsers?.Select(GetChatUserDto).ToList();
    }
}
