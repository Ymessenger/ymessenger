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
using NodeApp.Blockchain;
using NodeApp.CacheStorageClasses;
using NodeApp.CrossNodeClasses;
using NodeApp.CrossNodeClasses.Services;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.Entities;
using NodeApp.MessengerData.Services;
using NodeApp.MessengerData.Services.Channels;
using NodeApp.MessengerData.Services.Chats;
using NodeApp.MessengerData.Services.Dialogs;
using NodeApp.MessengerData.Services.Messages;
using NodeApp.MessengerData.Services.Users;
using NodeApp.NotificationServices;
using NodeApp.Objects;
using ObjectsLibrary.Interfaces;
using System;

namespace NodeApp.Tests.Mocks
{
    public class MockAppServiceProvider : IAppServiceProvider
    {
        private readonly IServiceProvider serviceProvider;
        public IBlockchainDataRestorer BlockchainDataRestorer
        {
            get
            {
                return serviceProvider.GetService<IBlockchainDataRestorer>();
            }
        }
        public IConnectionsService ConnectionsService
        {
            get
            {
                return serviceProvider.GetService<IConnectionsService>();
            }
        }
        public INodeNoticeService NodeNoticeService
        {
            get
            {
                return serviceProvider.GetService<INodeNoticeService>();
            }
        }
        public IConversationsNoticeService ConversationsNoticeService
        {
            get
            {
                return serviceProvider.GetService<IConversationsNoticeService>();
            }
        }
        public IPushNotificationsService PushNotificationsService
        {
            get
            {
                return serviceProvider.GetService<IPushNotificationsService>();
            }
        }
        public INoticeService NoticeService
        {
            get
            {
                return serviceProvider.GetService<INoticeService>();
            }
        }
        public ILoadMessagesService LoadMessagesService
        {
            get
            {
                return serviceProvider.GetService<ILoadMessagesService>();
            }
        }
        public ICreateMessagesService CreateMessagesService
        {
            get
            {
                return serviceProvider.GetService<ICreateMessagesService>();
            }
        }
        public IDeleteMessagesService DeleteMessagesService
        {
            get
            {
                return serviceProvider.GetService<IDeleteMessagesService>();
            }
        }
        public IUpdateMessagesService UpdateMessagesService
        {
            get
            {
                return serviceProvider.GetService<IUpdateMessagesService>();
            }
        }
        public IAttachmentsService AttachmentsService
        {
            get
            {
                return serviceProvider.GetService<IAttachmentsService>();
            }
        }
        public ICreateChatsService CreateChatsService
        {
            get
            {
                return serviceProvider.GetService<ICreateChatsService>();
            }
        }
        public ILoadChatsService LoadChatsService
        {
            get
            {
                return serviceProvider.GetService<ILoadChatsService>();
            }
        }
        public IUpdateChatsService UpdateChatsService
        {
            get
            {
                return serviceProvider.GetService<IUpdateChatsService>();
            }
        }
        public IDeleteChatsService DeleteChatsService
        {
            get
            {
                return serviceProvider.GetService<IDeleteChatsService>();
            }
        }
        public IChangeNodeOperationsService ChangeNodeOperationsService
        {
            get
            {
                return serviceProvider.GetService<IChangeNodeOperationsService>();
            }
        }
        public IConversationsService ConversationsService
        {
            get
            {
                return serviceProvider.GetService<IConversationsService>();
            }
        }
        public ICreateUsersService CreateUsersService
        {
            get
            {
                return serviceProvider.GetService<ICreateUsersService>();
            }
        }
        public ILoadUsersService LoadUsersService
        {
            get
            {
                return serviceProvider.GetService<ILoadUsersService>();
            }
        }
        public IUpdateUsersService UpdateUsersService
        {
            get
            {
                return serviceProvider.GetService<IUpdateUsersService>();
            }
        }
        public IDeleteUsersService DeleteUsersService
        {
            get
            {
                return serviceProvider.GetService<IDeleteUsersService>();
            }
        }
        public ICreateChannelsService CreateChannelsService
        {
            get
            {
                return serviceProvider.GetService<ICreateChannelsService>();
            }
        }
        public ILoadChannelsService LoadChannelsService
        {
            get
            {
                return serviceProvider.GetService<ILoadChannelsService>();
            }
        }
        public IUpdateChannelsService UpdateChannelsService
        {
            get
            {
                return serviceProvider.GetService<IUpdateChannelsService>();
            }
        }
        public IDeleteChannelsService DeleteChannelsService
        {
            get
            {
                return serviceProvider.GetService<IDeleteChannelsService>();
            }
        }
        public IDeleteDialogsService DeleteDialogsService
        {
            get
            {
                return serviceProvider.GetService<IDeleteDialogsService>();
            }
        }
        public ILoadDialogsService LoadDialogsService
        {
            get
            {
                return serviceProvider.GetService<ILoadDialogsService>();
            }
        }
        public ITokensService TokensService
        {
            get
            {
                return serviceProvider.GetService<ITokensService>();
            }
        }
        public IFavoritesService FavoritesService
        {
            get
            {
                return serviceProvider.GetService<IFavoritesService>();
            }
        }
        public IContactsService ContactsService
        {
            get
            {
                return serviceProvider.GetService<IContactsService>();
            }
        }
        public IFilesService FilesService
        {
            get
            {
                return serviceProvider.GetService<IFilesService>();
            }
        }
        public IKeysService KeysService
        {
            get
            {
                return serviceProvider.GetService<IKeysService>();
            }
        }
        public IPollsService PollsService
        {
            get
            {
                return serviceProvider.GetService<IPollsService>();
            }
        }
        public IQRCodesService QRCodesService
        {
            get
            {
                return serviceProvider.GetService<IQRCodesService>();
            }
        }
        public IPoolsService PoolsService
        {
            get
            {
                return serviceProvider.GetService<IPoolsService>();
            }
        }
        public IPendingMessagesService PendingMessagesService
        {
            get
            {
                return serviceProvider.GetService<IPendingMessagesService>();
            }
        }
        public INodesService NodesService
        {
            get
            {
                return serviceProvider.GetService<INodesService>();
            }
        }
        public IGroupsService GroupsService
        {
            get
            {
                return serviceProvider.GetService<IGroupsService>();
            }
        }
        public INodeRequestSender NodeRequestSender
        {
            get
            {
                return serviceProvider.GetService<INodeRequestSender>();
            }
        }
        public IDbContextFactory<MessengerDbContext> MessengerDbContextFactory
        {
            get
            {
                return serviceProvider.GetService<IDbContextFactory<MessengerDbContext>>();
            }
        }

        public IVerificationCodesService VerificationCodesService => serviceProvider.GetService<IVerificationCodesService>();

        public ICacheRepository<VerificationCodeInfo> VerificationCodesRepository => serviceProvider.GetService<ICacheRepository<VerificationCodeInfo>>();

        public IPrivacyService PrivacyService => serviceProvider.GetService<IPrivacyService>();

        public ICrossNodeService CrossNodeService => serviceProvider.GetService<ICrossNodeService>();

        public IFileStorage FileStorage => throw new NotImplementedException();

        public ISmsService SmsService => throw new NotImplementedException();

        public ISystemMessagesService SystemMessagesService => throw new NotImplementedException();

        public MockAppServiceProvider(string dbName)
        {
            var services = new ServiceCollection();   
            services.AddSingleton<IDbContextFactory<MessengerDbContext>, MockMessengerDbContextFactory>(prov => new MockMessengerDbContextFactory(dbName));
            services.AddTransient<IBlockchainDataRestorer, BlockchainDataRestorer>();
            services.AddSingleton<IConnectionsService, ConnectionsService>();
            services.AddSingleton<INodeNoticeService, NodeNoticeService>();
            services.AddSingleton<IConversationsNoticeService, ConversationsNoticeService>();
            services.AddSingleton<IPushNotificationsService, PushNotificationsService>();
            services.AddSingleton<INoticeService, NoticeService>();
            services.AddTransient<ILoadMessagesService, LoadMessagesService>();
            services.AddTransient<ICreateMessagesService, CreateMessagesService>();
            services.AddTransient<IDeleteMessagesService, DeleteMessagesService>();
            services.AddTransient<IUpdateMessagesService, UpdateMessagesService>();
            services.AddTransient<IAttachmentsService, AttachmentsService>();
            services.AddTransient<ICreateChatsService, CreateChatsService>();
            services.AddTransient<ILoadChatsService, LoadChatsService>();
            services.AddTransient<IUpdateChatsService, UpdateChatsService>();
            services.AddTransient<IDeleteChatsService, DeleteChatsService>();
            services.AddTransient<IChangeNodeOperationsService, ChangeNodeOperationsService>();
            services.AddTransient<ICreateChannelsService, CreateChannelsService>();
            services.AddTransient<ILoadChannelsService, LoadChannelsService>();
            services.AddTransient<IUpdateChannelsService, UpdateChannelsService>();
            services.AddTransient<IDeleteChannelsService, DeleteChannelsService>();
            services.AddTransient<ILoadDialogsService, LoadDialogsService>();
            services.AddTransient<IDeleteDialogsService, DeleteDialogsService>();
            services.AddTransient<ICreateUsersService, CreateUsersService>();
            services.AddTransient<ILoadUsersService, LoadUsersService>();
            services.AddTransient<IUpdateUsersService, UpdateUsersService>();
            services.AddTransient<IDeleteUsersService, DeleteUsersService>();
            services.AddTransient<IContactsService, ContactsService>();
            services.AddTransient<IConversationsService, ConversationsService>();
            services.AddTransient<IFavoritesService, FavoritesService>();
            services.AddTransient<IFilesService, FilesService>();
            services.AddTransient<IGroupsService, GroupsService>();
            services.AddTransient<IKeysService, KeysService>();
            services.AddTransient<INodesService, NodesService>();
            services.AddTransient<IPendingMessagesService, PendingMessagesService>();
            services.AddTransient<IPollsService, PollsService>();
            services.AddTransient<IPoolsService, PoolsService>();
            services.AddTransient<IQRCodesService, QRCodesService>();
            services.AddTransient<ITokensService, TokensService>();
            services.AddTransient<INodeRequestSender, NodeRequestSender>();
            services.AddSingleton<ICacheRepository<VerificationCodeInfo>, MockVerificationCodesRepository>();
            services.AddSingleton<IVerificationCodesService, VerificationCodesService>();
            services.AddSingleton<IPrivacyService, PrivacyService>();
            services.AddSingleton<IAppServiceProvider, MockAppServiceProvider>(prov => new MockAppServiceProvider(dbName));
            services.AddSingleton<ICrossNodeService, CrossNodeService>();
            
            serviceProvider = services.BuildServiceProvider();
        }
    }
}
