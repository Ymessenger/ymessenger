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
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.Converters
{
    public static class UserFavoritesConverter
    {
        public static UserFavoritesDto GetUserFavoriteDto(UserFavoritesVm userFavorites, long userId)
        {
            if (userFavorites == null)
            {
                return null;
            }

            return new UserFavoritesDto
            {
                ChannelId = userFavorites.ChannelId,
                ChatId = userFavorites.ChatId,
                ContactId = userFavorites.ContactId,
                UserId = userId
            };
        }
        public static UserFavoritesVm GetUserFavoriteVm(UserFavoritesDto userFavorites, long userId)
        {
            if (userFavorites == null)
            {
                return null;
            }

            return new UserFavoritesVm
            {
                Channel = ChannelConverter.GetChannelVm(userFavorites.Channel, userId),
                ChannelId = userFavorites.ChannelId,
                Chat = ChatConverter.GetChatVm(userFavorites.Chat),
                ChatId = userFavorites.ChatId,
                Contact = ContactConverter.GetContactVm(userFavorites.Contact),
                ContactId = userFavorites.ContactId,
                SerialNumber = userFavorites.SerialNumber
            };
        }
        public static UserFavoritesDto GetUserFavoriteDto(UserFavorite userFavorites)
        {
            if (userFavorites == null)
            {
                return null;
            }

            return new UserFavoritesDto
            {
                Channel = ChannelConverter.GetChannelDto(userFavorites.Channel),
                ChannelId = userFavorites.ChannelId,
                Chat = ChatConverter.GetChatDto(userFavorites.Chat),
                ChatId = userFavorites.ChatId,
                Contact = ContactConverter.GetContactDto(userFavorites.Contact),
                ContactId = userFavorites.ContactId,
                SerialNumber = userFavorites.SerialNumber,
                UserId = userFavorites.UserId
            };
        }

        public static List<UserFavoritesDto> GetUserFavoritesDtos(List<UserFavorite> userFavorites)
        {
            return userFavorites?.Select(GetUserFavoriteDto).ToList();
        }

        public static List<UserFavorite> GetUserFavorites(List<UserFavoritesDto> userFavorites)
        {
            return userFavorites?.Select(GetUserFavorite).ToList();
        }

        public static List<UserFavoritesDto> GetUserFavoritesDtos(List<UserFavoritesVm> userFavorites, long userId)
        {
            return userFavorites?.Select(opt => GetUserFavoriteDto(opt, userId)).ToList();
        }

        public static List<UserFavoritesVm> GetUserFavoritesVms(List<UserFavoritesDto> userFavorites, long userId)
        {
            return userFavorites?.Select(opt => GetUserFavoriteVm(opt, userId)).ToList();
        }

        public static UserFavorite GetUserFavorite(UserFavoritesDto userFavorite)
        {
            if (userFavorite == null)
            {
                return null;
            }

            return new UserFavorite
            {
                ChannelId = userFavorite.ChannelId,
                ChatId = userFavorite.ChatId,
                ContactId = userFavorite.ContactId,
                UserId = userFavorite.UserId
            };
        }
    }
}
