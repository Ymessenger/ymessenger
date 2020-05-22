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
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.Converters
{
    public static class ChatConverter
    {
        public static ChatVm GetChatVm(Chat chat)
        {
            return chat == null
                ? null
                : new ChatVm
                {
                    About = chat.About,
                    Id = chat.Id,
                    Name = chat.Name,
                    Photo = chat.Photo,
                    Public = chat.Public,
                    Security = chat.Security,
                    Visible = chat.Visible,
                    Users = chat.ChatUsers != null
                        ? ChatUserConverter.GetChatUsersVm(chat.ChatUsers)?.ToList()
                        : null,
                    Tag = chat.Tag,
                    Type = (ChatType)chat.Type,
                    NodesId = chat.NodesId?.ToList()
                };
        }

        public static Chat GetChat(ChatVm chat)
        {
            return chat == null
                ? null
                : new Chat
                {
                    About = chat.About,
                    Id = chat.Id.GetValueOrDefault(),
                    Name = chat.Name,
                    Photo = chat.Photo,
                    Public = chat.Public,
                    Security = chat.Security,
                    Visible = chat.Visible,
                    Type = (short)chat.Type,
                    NodesId = chat.NodesId?.ToArray(),
                    Tag = chat.Tag,
                    Deleted = false
                    /*ChatUsers = chat.Users != null 
                         ? ChatUserBuilder.GetChatUsers(chat.Users) 
                         : null*/
                };
        }

        public static Chat GetChat(Chat editableChat, EditChatVm editChat)
        {
            if (editChat != null)
            {
                editableChat.About = editChat.About ?? editableChat.About;
                editableChat.Name = editChat.Name ?? editableChat.Name;
                editableChat.Photo = editChat.Photo ?? editableChat.Photo;
                editableChat.Public = editChat.Public ?? editableChat.Public;
                editableChat.Security = editChat.Security ?? editableChat.Security;
                editableChat.Visible = editChat.Visible ?? editableChat.Visible;
            }
            return editableChat;
        }

        public static Chat GetChat(Chat editableChat, ChatDto editedChat)
        {
            if (editableChat == null)
            {
                return new Chat
                {
                    About = editedChat.About,
                    ChatUsers = ChatUserConverter.GetChatUsers(editedChat.ChatUsers)?.ToList(),
                    Deleted = false,
                    Id = editedChat.Id,
                    Name = editedChat.Name,
                    NodesId = editedChat.NodesId?.ToArray(),
                    Photo = editedChat.Photo,
                    Public = editedChat.Public,
                    Security = editedChat.Security,
                    Tag = editedChat.Tag,
                    Type = editedChat.Type,
                    Visible = editedChat.Visible
                };
            }
            editableChat.About = editedChat.About;
            editableChat.Name = editedChat.Name;
            editableChat.NodesId = editedChat.NodesId?.ToArray() ?? editableChat.NodesId;
            editableChat.Photo = editedChat.Photo;
            editableChat.Public = editedChat.Public;
            editableChat.Security = editedChat.Security;
            editableChat.Tag = editedChat.Tag;
            editableChat.Type = editedChat.Type;
            editableChat.Visible = editedChat.Visible;
            editableChat.ChatUsers = ChatUserConverter.GetChatUsers(editedChat.ChatUsers)?.ToList() ?? editableChat.ChatUsers;
            return editableChat;
        }


        public static List<ChatVm> GetChatsVm(IEnumerable<Chat> chats)
        {
            return chats?.Select(GetChatVm).ToList();
        }

        public static ChatDto GetChatDto(Chat chat)
        {
            if (chat == null)
            {
                return null;
            }

            return new ChatDto
            {
                About = chat.About,
                Deleted = chat.Deleted,
                Id = chat.Id,
                Name = chat.Name,
                Photo = chat.Photo,
                NodesId = chat.NodesId,
                Public = chat.Public,
                Security = chat.Security,
                Tag = chat.Tag,
                Type = chat.Type,
                Visible = chat.Visible,
                Messages = MessageConverter.GetMessagesDto(chat.Messages),
                ChatUsers = ChatUserConverter.GetChatUsersDto(chat.ChatUsers)
            };
        }
        public static List<ChatDto> GetChatsDto(List<ChatVm> chatsVm)
        {
            return chatsVm?.Select(GetChatDto).ToList();
        }

        public static ChatDto GetChatDto(ChatVm chat)
        {
            if (chat == null)
            {
                return null;
            }

            return new ChatDto
            {
                About = chat.About,
                Id = chat.Id.GetValueOrDefault(),
                Name = chat.Name,
                NodesId = chat.NodesId?.ToArray(),
                Photo = chat.Photo,
                Tag = chat.Tag,
                Visible = chat.Visible,
                Type = (short)chat.Type,
                Public = chat.Public,
                Security = chat.Security,
                ChatUsers = chat.Users?.Select(opt => new ChatUserDto
                {
                    ChatId = chat.Id.GetValueOrDefault(),
                    UserId = opt.UserId,
                    UserRole = opt.UserRole.GetValueOrDefault(),
                    Banned = opt.Banned.GetValueOrDefault(),
                    Deleted = opt.Deleted.GetValueOrDefault(),
                    InviterId = opt.InviterId.GetValueOrDefault(),
                    IsMuted = opt.IsMuted.GetValueOrDefault(),
                    Joined = opt.Joined.GetValueOrDefault()
                })?.ToList()
            };
        }

        public static ChatVm GetChatVm(ChatDto chat)
        {
            if (chat == null)
            {
                return null;
            }

            return new ChatVm
            {
                About = chat.About,
                Id = chat.Id,
                Name = chat.Name,
                NodesId = chat.NodesId?.ToList(),
                Photo = chat.Photo,
                Public = chat.Public,
                Security = chat.Security,
                Tag = chat.Tag,
                Type = (ChatType)chat.Type,
                Visible = chat.Visible
            };
        }

        public static List<ChatVm> GetChatsVm(List<ChatDto> chats) => chats?.Select(GetChatVm).ToList();



        public static List<ChatDto> GetChatsDto(IEnumerable<Chat> chat)
        {
            return chat?.Select(GetChatDto).ToList();
        }
    }
}
