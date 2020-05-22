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
using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class FavoritesService : IFavoritesService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public FavoritesService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task<UserFavoritesDto> AddUserFavoritesAsync(long? channelId, long? chatId, Guid? contactId, long userId)
        {
            if (channelId != null)
            {
                return await AddChannelToUserFavoritesAsync(channelId.Value, userId).ConfigureAwait(false);
            }
            else if (chatId != null)
            {
                return await AddChatToUserFavoritesAsync(chatId.Value, userId).ConfigureAwait(false);
            }
            else if (contactId != null)
            {
                return await AddContactToUserFavoritesAsync(contactId.Value, userId).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentNullException();
            }
        }
        private async Task<UserFavoritesDto> AddContactToUserFavoritesAsync(Guid contactId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var contact = await context.Contacts
                .AsNoTracking()
                .Include(opt => opt.ContactUser)
                .FirstOrDefaultAsync(opt => opt.ContactId == contactId && opt.UserId == userId)
                .ConfigureAwait(false);
                UserFavorite lastUserFavorites = await context.UsersFavorites
                    .AsNoTracking()
                    .OrderByDescending(opt => opt.SerialNumber)
                    .FirstOrDefaultAsync(opt => opt.UserId == userId)
                    .ConfigureAwait(false);
                short newSerialNumber = lastUserFavorites != null ? (short)(lastUserFavorites.SerialNumber + 1) : (short)1;
                if (contact != null)
                {
                    UserFavorite userFavorites = new UserFavorite
                    {
                        ContactId = contactId,
                        UserId = userId,
                        SerialNumber = newSerialNumber
                    };
                    await context.UsersFavorites.AddAsync(userFavorites).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return UserFavoritesConverter.GetUserFavoriteDto(userFavorites);

                }
                else
                {
                    throw new ObjectDoesNotExistsException();
                }
            }
        }
        private async Task<UserFavoritesDto> AddChatToUserFavoritesAsync(long chatId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var chat = await context.Chats
                .AsNoTracking()
                .FirstOrDefaultAsync(chatOpt =>
                    chatOpt.Id == chatId
                    && chatOpt.ChatUsers.Any(chatUser => chatUser.UserId == userId && !chatUser.Banned && !chatUser.Deleted && !chatOpt.Deleted))
                .ConfigureAwait(false);
                UserFavorite lastUserFavorites = await context.UsersFavorites
                    .AsNoTracking()
                    .OrderByDescending(opt => opt.SerialNumber)
                    .FirstOrDefaultAsync(opt => opt.UserId == userId)
                    .ConfigureAwait(false);
                short newSerialNumber = lastUserFavorites != null ? (short)(lastUserFavorites.SerialNumber + 1) : (short)1;
                if (chat != null)
                {
                    UserFavorite userFavorites = new UserFavorite
                    {
                        ChatId = chatId,
                        UserId = userId,
                        SerialNumber = newSerialNumber
                    };
                    await context.UsersFavorites.AddAsync(userFavorites).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return UserFavoritesConverter.GetUserFavoriteDto(userFavorites);
                }
                else
                {
                    throw new ObjectDoesNotExistsException();
                }
            }
        }
        private async Task<UserFavoritesDto> AddChannelToUserFavoritesAsync(long channelId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var channel = await context.Channels
                .AsNoTracking()
                .FirstOrDefaultAsync(channelOpt =>
                    channelOpt.ChannelId == channelId
                    && channelOpt.ChannelUsers.Any(channelUser => channelUser.UserId == userId && !channelUser.Banned && !channelUser.Deleted && !channelOpt.Deleted))
                .ConfigureAwait(false);
                UserFavorite lastUserFavorites = await context.UsersFavorites
                    .AsNoTracking()
                    .OrderByDescending(opt => opt.SerialNumber)
                    .FirstOrDefaultAsync(opt => opt.UserId == userId)
                    .ConfigureAwait(false);
                short newSerialNumber = lastUserFavorites != null ? (short)(lastUserFavorites.SerialNumber + 1) : (short)1;
                if (channel != null)
                {
                    UserFavorite userFavorites = new UserFavorite
                    {
                        ChannelId = channelId,
                        UserId = userId,
                        SerialNumber = newSerialNumber
                    };
                    await context.UsersFavorites.AddAsync(userFavorites).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return UserFavoritesConverter.GetUserFavoriteDto(userFavorites);
                }
                else
                {
                    throw new ObjectDoesNotExistsException();
                }
            }
        }
        public async Task<List<UserFavoritesDto>> ChangeUserFavoritesAsync(List<UserFavoritesDto> newFavorites, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                using (var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        var userFavorites = await context.UsersFavorites
                            .Where(opt => opt.UserId == userId)
                            .ToListAsync()
                            .ConfigureAwait(false);
                        context.UsersFavorites.RemoveRange(userFavorites);
                        var favorites = UserFavoritesConverter.GetUserFavorites(newFavorites);
                        short counter = 1;
                        foreach (var favorite in favorites)
                        {
                            favorite.SerialNumber = counter;
                            counter++;
                        }
                        await context.UsersFavorites.AddRangeAsync(favorites).ConfigureAwait(false);
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        transaction.Commit();
                        return UserFavoritesConverter.GetUserFavoritesDtos(favorites);
                    }
                    catch (Exception ex)
                    {
                        throw new InternalErrorException("Error adding favorites.", ex);
                    }
                }
            }
        }
        public async Task<List<UserFavoritesDto>> GetUserFavoritesAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var userFavorites = await context.UsersFavorites
                .Include(opt => opt.Channel)
                .Include(opt => opt.Contact)
                    .ThenInclude(opt => opt.ContactUser)
                .Include(opt => opt.Chat)
                .Where(opt => opt.UserId == userId)
                .OrderBy(opt => opt.SerialNumber)
                .ToListAsync()
                .ConfigureAwait(false);
                return UserFavoritesConverter.GetUserFavoritesDtos(userFavorites);
            }
        }
        public async Task RemoveUserFavoritesAsync(long? channelId, long? chatId, Guid? contactId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var query = context.UsersFavorites.Where(opt => opt.UserId == userId);
                if (channelId != null)
                {
                    query = query.Where(opt => opt.ChannelId == channelId);
                }
                else if (chatId != null)
                {
                    query = query.Where(opt => opt.ChatId == chatId);
                }
                else if (contactId != null)
                {
                    query = query.Where(opt => opt.ContactId == contactId);
                }
                else
                {
                    throw new ArgumentNullException();
                }
                var favorite = await query.FirstOrDefaultAsync().ConfigureAwait(false);
                if (favorite != null)
                {
                    context.Remove(favorite);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
                else
                {
                    throw new ObjectDoesNotExistsException("Object not found in favorites.");
                }
            }
        }
    }
}