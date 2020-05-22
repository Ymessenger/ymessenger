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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.MessengerData.DataTransferObjects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class FavoritesTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IFavoritesService favoritesService;
        public FavoritesTests()
        {
            TestsData testsData = TestsData.Create(nameof(FavoritesTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            favoritesService = testsData.AppServiceProvider.FavoritesService;
        }
        [Fact]
        public async Task AddUserFavorites()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var chat = fillTestDbHelper.Chats.FirstOrDefault(opt => opt.ChatUsers.Any(p => p.UserId == user.Id));
            var channel = fillTestDbHelper.Channels.FirstOrDefault(opt => opt.ChannelUsers.Any(p => p.UserId == user.Id));
            var channelFavorite = await favoritesService.AddUserFavoritesAsync(channel.ChannelId, null, null, user.Id);
            var chatFavorite = await favoritesService.AddUserFavoritesAsync(null, chat.Id, null, user.Id);
            var contactFavorite = await favoritesService.AddUserFavoritesAsync(null, null, user.Contacts.FirstOrDefault().ContactId, user.Id);
            Assert.True(channelFavorite.ChannelId == channel.ChannelId
                && chatFavorite.ChatId == chat.Id
                && contactFavorite.ContactId == user.Contacts.FirstOrDefault().ContactId);
        }
        [Fact]
        public async Task ChangeUserFavorites()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var chat = fillTestDbHelper.Chats.FirstOrDefault(opt => opt.ChatUsers.Any(p => p.UserId == user.Id));
            var channel = fillTestDbHelper.Channels.FirstOrDefault(opt => opt.ChannelUsers.Any(p => p.UserId == user.Id));
            var userFavorites = new List<UserFavoritesDto>
            {
                new UserFavoritesDto
                {
                    ChannelId = channel.ChannelId                   
                },
                new UserFavoritesDto
                {
                    ChatId = chat.Id
                },
                new UserFavoritesDto
                {
                    ContactId = user.Contacts.FirstOrDefault().ContactId
                }
            };
            var actualFavorites = await favoritesService.ChangeUserFavoritesAsync(userFavorites, user.Id);
            Assert.True(userFavorites.All(opt => actualFavorites.Any(p => p.ContactId == opt.ContactId)
            || actualFavorites.Any(p => p.ChatId == opt.ChatId)
            || actualFavorites.Any(p => p.ChannelId == opt.ChannelId)));
        }
        [Fact]
        public async Task GetUserFavorites()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var favorites = await favoritesService.GetUserFavoritesAsync(user.Id);
            Assert.True(user.Favorites.Count == favorites.Count);
        }
        [Fact]
        public async Task RemoveFavorites()
        {
            var user = fillTestDbHelper.Users.LastOrDefault(opt => !opt.Favorites.IsNullOrEmpty());
            var favorite = user.Favorites.FirstOrDefault();
            await favoritesService.RemoveUserFavoritesAsync(favorite.ChannelId, favorite.ChatId, favorite.ContactId, user.Id);
            var actualFavorites = fillTestDbHelper.Users.FirstOrDefault(opt => opt.Id == user.Id).Favorites;
            Assert.True(user.Favorites.Count - 1 == actualFavorites.Count);
        }
    }
}
