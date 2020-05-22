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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetChatsRequestHandler : IRequestHandler
    {
        private readonly GetChatsRequest request;
        private readonly ILoadChatsService loadChatsService;
        private readonly ClientConnection clientConnection;

        public GetChatsRequestHandler(Request request, ILoadChatsService loadChatsService, ClientConnection clientConnection)
        {
            this.request = (GetChatsRequest)request;
            this.loadChatsService = loadChatsService;
            this.clientConnection = clientConnection;
        }

        public async Task<Response> CreateResponseAsync()
        {
            IEnumerable<ChatVm> chats = await loadChatsService.GetChatsByIdAsync(request.ChatsId, clientConnection.UserId).ConfigureAwait(false);
            return new ChatsResponse(request.RequestId, chats);
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
            {
                throw new UnauthorizedUserException();
            }

            return request.ChatsId != null
                && request.ChatsId.Any()
                && request.ChatsId.Count() < 100;
        }
    }
}