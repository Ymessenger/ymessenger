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
namespace NodeApp.CrossNodeClasses.Enums
{
    public enum NodeResponseType : byte
    {
        Chats = 0,
        Users = 1,
        ChatsPreview = 2,
        BlockchainInfo = 3,
        Blocks = 4,
        Messages = 5,
        UserTokens = 6,
        Result = 7,
        ChatUsers = 8,
        ChannelUsers = 9,
        Channels = 10,
        PublicKey = 11,
        NodeInformation = 12,
        Poll = 13,
        Proxy = 14,
        Search = 15,
        ConversationsUsers = 16,
        Files = 17
    }
}
