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
using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class EditFavoritesRequestHandler : IRequestHandler
    {
        private readonly EditFavoritesRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IFavoritesService favoritesService;

        public EditFavoritesRequestHandler(Request request, ClientConnection clientConnection, IFavoritesService favoritesService)
        {
            this.request = (EditFavoritesRequest)request;
            this.clientConnection = clientConnection;
            this.favoritesService = favoritesService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            var userFavorites = await favoritesService.ChangeUserFavoritesAsync(
                UserFavoritesConverter.GetUserFavoritesDtos(request.Favorites, clientConnection.UserId.Value), clientConnection.UserId.Value).ConfigureAwait(false);
            return new FavoritesResponse(request.RequestId, UserFavoritesConverter.GetUserFavoritesVms(userFavorites, clientConnection.UserId.Value));
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
            {
                throw new UnauthorizedUserException();
            }

            if (!clientConnection.Confirmed)
            {
                throw new PermissionDeniedException("User is not confirmed.");
            }

            if (request.Favorites == null || !request.Favorites.Any())
            {
                return false;
            }

            foreach (var item in request.Favorites)
            {
                if ((item.ChannelId != null && (item.ChatId != null || item.ContactId != null))
                || (item.ChatId != null && (item.ChannelId != null || item.ContactId != null))
                || (item.ContactId != null && (item.ChannelId != null || item.ChatId != null)))
                {
                    return false;
                }

                if (item.ChatId == null && item.ChannelId == null && item.ContactId == null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}