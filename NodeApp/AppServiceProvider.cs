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
using Microsoft.Extensions.DependencyInjection;
using NodeApp.CacheStorageClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Interfaces;
using System;

namespace NodeApp
{
    public class AppServiceProvider : IAppServiceProvider
    {
        private readonly IServiceProvider serviceProvider;
        public static IAppServiceProvider Instance { get; set; }
        public IBlockchainDataRestorer BlockchainDataRestorer => serviceProvider.GetService<IBlockchainDataRestorer>();
        public IConnectionsService ConnectionsService => serviceProvider.GetService<IConnectionsService>();
        public INodeNoticeService NodeNoticeService => serviceProvider.GetService<INodeNoticeService>();
        public IConversationsNoticeService ConversationsNoticeService => serviceProvider.GetService<IConversationsNoticeService>();
        public IPushNotificationsService PushNotificationsService => serviceProvider.GetService<IPushNotificationsService>();
        public INoticeService NoticeService => serviceProvider.GetService<INoticeService>();
        public ILoadMessagesService LoadMessagesService => serviceProvider.GetService<ILoadMessagesService>();
        public ICreateMessagesService CreateMessagesService => serviceProvider.GetService<ICreateMessagesService>();
        public IDeleteMessagesService DeleteMessagesService => serviceProvider.GetService<IDeleteMessagesService>();
        public IUpdateMessagesService UpdateMessagesService => serviceProvider.GetService<IUpdateMessagesService>();
        public IAttachmentsService AttachmentsService => serviceProvider.GetService<IAttachmentsService>();
        public ICreateChatsService CreateChatsService => serviceProvider.GetService<ICreateChatsService>();
        public ILoadChatsService LoadChatsService => serviceProvider.GetService<ILoadChatsService>();
        public IUpdateChatsService UpdateChatsService => serviceProvider.GetService<IUpdateChatsService>();
        public IDeleteChatsService DeleteChatsService => serviceProvider.GetService<IDeleteChatsService>();
        public IChangeNodeOperationsService ChangeNodeOperationsService => serviceProvider.GetService<IChangeNodeOperationsService>();
        public IConversationsService ConversationsService => serviceProvider.GetService<IConversationsService>();
        public ICreateUsersService CreateUsersService => serviceProvider.GetService<ICreateUsersService>();
        public ILoadUsersService LoadUsersService => serviceProvider.GetService<ILoadUsersService>();
        public IUpdateUsersService UpdateUsersService => serviceProvider.GetService<IUpdateUsersService>();
        public IDeleteUsersService DeleteUsersService => serviceProvider.GetService<IDeleteUsersService>();
        public ICreateChannelsService CreateChannelsService => serviceProvider.GetService<ICreateChannelsService>();
        public ILoadChannelsService LoadChannelsService => serviceProvider.GetService<ILoadChannelsService>();
        public IUpdateChannelsService UpdateChannelsService => serviceProvider.GetService<IUpdateChannelsService>();
        public IDeleteChannelsService DeleteChannelsService => serviceProvider.GetService<IDeleteChannelsService>();
        public IDeleteDialogsService DeleteDialogsService => serviceProvider.GetService<IDeleteDialogsService>();
        public ILoadDialogsService LoadDialogsService => serviceProvider.GetService<ILoadDialogsService>();
        public ITokensService TokensService => serviceProvider.GetService<ITokensService>();
        public IFavoritesService FavoritesService => serviceProvider.GetService<IFavoritesService>();
        public IContactsService ContactsService => serviceProvider.GetService<IContactsService>();
        public IFilesService FilesService => serviceProvider.GetService<IFilesService>();
        public IKeysService KeysService => serviceProvider.GetService<IKeysService>();
        public IPollsService PollsService => serviceProvider.GetService<IPollsService>();
        public IQRCodesService QRCodesService => serviceProvider.GetService<IQRCodesService>();
        public IPoolsService PoolsService => serviceProvider.GetService<IPoolsService>();
        public IPendingMessagesService PendingMessagesService => serviceProvider.GetService<IPendingMessagesService>();
        public INodesService NodesService => serviceProvider.GetService<INodesService>();
        public IGroupsService GroupsService => serviceProvider.GetService<IGroupsService>();
        public INodeRequestSender NodeRequestSender => serviceProvider.GetService<INodeRequestSender>();
        public IVerificationCodesService VerificationCodesService => serviceProvider.GetService<IVerificationCodesService>();
        public IPrivacyService PrivacyService => serviceProvider.GetService<IPrivacyService>();

        public ICacheRepository<VerificationCodeInfo> VerificationCodesRepository => serviceProvider.GetService<ICacheRepository<VerificationCodeInfo>>();
        public IFileStorage FileStorage => serviceProvider.GetService<IFileStorage>();
        public ICrossNodeService CrossNodeService => serviceProvider.GetService<ICrossNodeService>();
        public ISmsService SmsService => serviceProvider.GetService<ISmsService>();
        public ISystemMessagesService SystemMessagesService => serviceProvider.GetService<ISystemMessagesService>();

        public AppServiceProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            Instance = this;
        }
    }
}
