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
using NodeApp.CacheStorageClasses;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Interfaces;

namespace NodeApp.Interfaces
{
    public interface IAppServiceProvider
    {
        IAttachmentsService AttachmentsService { get; }
        IBlockchainDataRestorer BlockchainDataRestorer { get; }
        IChangeNodeOperationsService ChangeNodeOperationsService { get; }
        IConnectionsService ConnectionsService { get; }
        IContactsService ContactsService { get; }
        IConversationsNoticeService ConversationsNoticeService { get; }
        IConversationsService ConversationsService { get; }
        ICreateChannelsService CreateChannelsService { get; }
        ICreateChatsService CreateChatsService { get; }
        ICreateMessagesService CreateMessagesService { get; }
        ICreateUsersService CreateUsersService { get; }
        IDeleteChannelsService DeleteChannelsService { get; }
        IDeleteChatsService DeleteChatsService { get; }
        IDeleteDialogsService DeleteDialogsService { get; }
        IDeleteMessagesService DeleteMessagesService { get; }
        IDeleteUsersService DeleteUsersService { get; }
        IFavoritesService FavoritesService { get; }
        IFilesService FilesService { get; }
        IGroupsService GroupsService { get; }
        IKeysService KeysService { get; }
        ILoadChannelsService LoadChannelsService { get; }
        ILoadChatsService LoadChatsService { get; }
        ILoadDialogsService LoadDialogsService { get; }
        ILoadMessagesService LoadMessagesService { get; }
        ILoadUsersService LoadUsersService { get; }
        INodeNoticeService NodeNoticeService { get; }
        INodeRequestSender NodeRequestSender { get; }
        INodesService NodesService { get; }
        INoticeService NoticeService { get; }
        IPendingMessagesService PendingMessagesService { get; }
        IPollsService PollsService { get; }
        IPoolsService PoolsService { get; }
        IPushNotificationsService PushNotificationsService { get; }
        IQRCodesService QRCodesService { get; }
        ITokensService TokensService { get; }
        IUpdateChannelsService UpdateChannelsService { get; }
        IUpdateChatsService UpdateChatsService { get; }
        IUpdateMessagesService UpdateMessagesService { get; }
        IUpdateUsersService UpdateUsersService { get; }
        ICacheRepository<VerificationCodeInfo> VerificationCodesRepository { get; }
        IVerificationCodesService VerificationCodesService { get; }
        IPrivacyService PrivacyService { get; }
        ICrossNodeService CrossNodeService { get; }
        IFileStorage FileStorage { get; }
        ISmsService SmsService { get; }
        ISystemMessagesService SystemMessagesService { get; }
    }
}