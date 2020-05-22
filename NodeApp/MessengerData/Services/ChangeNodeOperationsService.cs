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
using Microsoft.EntityFrameworkCore;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class ChangeNodeOperationsService : IChangeNodeOperationsService
    {
        private readonly ICreateChatsService createChatsService;
        private readonly ICreateMessagesService createMessagesService;
        private readonly IUpdateUsersService updateUsersService;
        private readonly ICreateChannelsService createChannelsService;
        private readonly IContactsService contactsService;
        private readonly IGroupsService groupsService;
        private readonly IFavoritesService favoritesService;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public ChangeNodeOperationsService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            createChatsService = appServiceProvider.CreateChatsService;
            createMessagesService = appServiceProvider.CreateMessagesService;
            updateUsersService = appServiceProvider.UpdateUsersService;
            createChannelsService = appServiceProvider.CreateChannelsService;
            contactsService = appServiceProvider.ContactsService;
            groupsService = appServiceProvider.GroupsService;
            favoritesService = appServiceProvider.FavoritesService;
            this.contextFactory = contextFactory;
        }
        public async Task<string> AddNewOperationAsync(long nodeId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                long currentTime = DateTime.UtcNow.ToUnixTime();
                var uncomplededOperation = await context.ChangeUserNodeOperations
                    .FirstOrDefaultAsync(opt => !opt.Completed && opt.UserId == userId && opt.ExpirationTime > currentTime).ConfigureAwait(false);
                if (uncomplededOperation != null)
                {
                    context.Remove(uncomplededOperation);
                }
                ChangeUserNodeOperation operation = new ChangeUserNodeOperation
                {
                    NodeId = nodeId,
                    UserId = userId,
                    OperationId = RandomExtensions.NextString(64),
                    Completed = false,
                    RequestTime = currentTime,
                    ExpirationTime = currentTime + (long)TimeSpan.FromDays(1).TotalSeconds
                };
                await context.AddAsync(operation).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return operation.OperationId;
            }
        }

        public async Task<long> GetOperationUserIdAsync(string operationId, long nodeId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                ChangeUserNodeOperation operation = await context.ChangeUserNodeOperations
                .AsNoTracking()
                .FirstOrDefaultAsync(opt => opt.NodeId == nodeId && opt.OperationId == operationId)
                .ConfigureAwait(false);
                if (operation != null)
                {
                    return operation.UserId;
                }
                else
                {
                    throw new ArgumentNullException(nameof(operationId));
                }
            }
        }

        public async Task<ChangeUserNodeOperation> CompleteOperationAsync(string operationId, long nodeId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                ChangeUserNodeOperation operation = await context.ChangeUserNodeOperations
                    .FirstOrDefaultAsync(opt => opt.NodeId == nodeId && opt.OperationId == operationId)
                    .ConfigureAwait(false);
                operation.Completed = true;
                context.ChangeUserNodeOperations.Update(operation);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return operation;
            }
        }

        public async Task<bool> SaveUserDataAsync(UserDto userData)
        {
            try
            {
                UserDto user = await updateUsersService.CreateOrUpdateUserAsync(userData).ConfigureAwait(false);
                IEnumerable<ChatDto> chats = await createChatsService.CreateOrUpdateUserChatsAsync(userData.Chats).ConfigureAwait(false);
                IEnumerable<ChannelDto> channels = await createChannelsService.CreateOrUpdateUserChannelsAsync(userData.Channels).ConfigureAwait(false);
                foreach (var chat in userData.Chats)
                {
                    await createMessagesService.SaveMessagesAsync(chat.Messages, user.Id).ConfigureAwait(false);
                }
                foreach (var channel in userData.Channels)
                {
                    await createMessagesService.SaveMessagesAsync(channel.Messages, user.Id).ConfigureAwait(false);
                }
                foreach (var dialog in userData.Dialogs)
                {
                    await createMessagesService.SaveMessagesAsync(dialog.Messages, user.Id).ConfigureAwait(false);
                }
                foreach (var contact in userData.Contacts)
                {
                    await contactsService.CreateOrEditContactAsync(contact).ConfigureAwait(false);
                }
                foreach (var group in userData.ContactGroups)
                {
                    await groupsService.CreateOrEditGroupAsync(group).ConfigureAwait(false);
                }
                await favoritesService.ChangeUserFavoritesAsync(userData.Favorites, userData.Id).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return false;
            }
        }
    }
}