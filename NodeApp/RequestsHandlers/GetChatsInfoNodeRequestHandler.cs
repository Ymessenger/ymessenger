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
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Chats;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetChatsInfoNodeRequestHandler : IRequestHandler
    {
        private readonly ILoadChatsService loadChatsService;
        public GetChatsInfoNodeRequestHandler(Request request, ILoadChatsService loadChatsService)
        {
            this.request = (GetChatsInformationNodeRequest)request;
            this.loadChatsService = loadChatsService;
        }

        private readonly GetChatsInformationNodeRequest request;

        public async Task<Response> CreateResponseAsync()
        {            
            List<ChatVm> chats = await loadChatsService.FindChatsAsync(request.Chat, 100, request.NavigationChatId.GetValueOrDefault()).ConfigureAwait(false);            
            return new FoundChatsResponse(request.RequestId, request.Chat, chats);
        }

        public bool IsRequestValid()
        {
            return true;
        }
    }
}
