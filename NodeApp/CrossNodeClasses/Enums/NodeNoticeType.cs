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
    public enum NodeNoticeCode : short
    {
        License = 1001,
        NoPay,
        BadLicense,
        BanLicense,
        NewLicenseKey,
        NewBlocks = 2001,
        UncorrectBlocks,
        NewComplication,
        SetComplication,
        BlockSegments,
        NewNodes = 3001,
        ConnectNodes,
        DisconnectNodes,
        ErrorNodes,
        EditNodes,
        NewUsers = 4001,
        EditUsers,
        DeleteUsers,
        NewPasswords,
        UsersAddedToUserBlacklist,
        UsersRemovedFromUserBlacklist,
        UserNodeChanged,
        NewChats = 5001,
        DeleteConversations,
        EditChats,
        AddUsersChat,
        ChangeUsersChat,
        NewFiles = 6001,
        DeleteFiles,
        Messages = 7001,
        NewMessagesNotice,
        MessagesRead,
        MessagesDeleted,
        MessagesUpdated,
        Polling,
        AllMessagesDeleted,
        ConversationAction,
        NewUserKeys = 8001,
        DeleteUserKeys,
        NewNodeKeys,
        Channels = 9001,
        ChannelsUsers,
        AddUsersToChannel,
        ClientDisconnected = 15001,
        Proxy = 15002
    }
}
