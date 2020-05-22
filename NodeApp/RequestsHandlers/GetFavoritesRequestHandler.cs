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
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetFavoritesRequestHandler : IRequestHandler
    {
        private readonly GetFavoritesRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IFavoritesService favoritesService;

        public GetFavoritesRequestHandler(Request request, ClientConnection clientConnection, IFavoritesService favoritesService)
        {
            this.request = (GetFavoritesRequest)request;
            this.clientConnection = clientConnection;
            this.favoritesService = favoritesService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<UserFavoritesDto> usersFavorites = await favoritesService.GetUserFavoritesAsync(clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            return new FavoritesResponse(request.RequestId, UserFavoritesConverter.GetUserFavoritesVms(usersFavorites, clientConnection.UserId.GetValueOrDefault()));
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

            return true;
        }
    }
}