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
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetDialogsInformationRequestHandler : IRequestHandler
    {
        private readonly GetDialogsInformationRequest request;
        private readonly ClientConnection clientConnection;
        private readonly ILoadDialogsService loadDialogsService;

        public GetDialogsInformationRequestHandler(Request request, ClientConnection clientConnection, ILoadDialogsService loadDialogsService)
        {
            this.request = (GetDialogsInformationRequest)request;
            this.clientConnection = clientConnection;
            this.loadDialogsService = loadDialogsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<ConversationPreviewVm> userDialogs = await loadDialogsService.GetUserDialogsPreviewAsync(clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            List<ConversationPreviewVm> validDialogs = userDialogs.Where(opt => request.UsersId.Contains(opt.SecondUid.GetValueOrDefault())).ToList();
            return new ConversationsResponse
            {
                Conversations = validDialogs,
                RequestId = request.RequestId,
                ResponseType = ObjectsLibrary.Enums.ResponseType.Conversations
            };
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

            return request.UsersId != null && request.UsersId.Any();
        }
    }
}