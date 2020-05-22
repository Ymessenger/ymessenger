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
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp
{
    public class SearchMessagesRequestHandler : IRequestHandler
    {
        private readonly SearchMessagesRequest request;
        private readonly ClientConnection clientConnection;
        private readonly ILoadMessagesService loadMessagesService;

        public SearchMessagesRequestHandler(Request request, ClientConnection clientConnection, ILoadMessagesService loadMessagesService)
        {
            this.request = (SearchMessagesRequest)request;
            this.clientConnection = clientConnection;
            this.loadMessagesService = loadMessagesService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                List<MessageDto> messages = await loadMessagesService.SearchMessagesAsync(
                    request.Query,
                    request.ConversationType,
                    request.ConversationId,
                    request.NavigationConversationType,
                    request.NavigationConversationId,
                    request.NavigationMessageId,
                    clientConnection.UserId).ConfigureAwait(false);
                return new MessagesResponse(request.RequestId, MessageConverter.GetMessagesVm(messages, clientConnection.UserId));
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return new ResultResponse(request.RequestId, "Error", ObjectsLibrary.Enums.ErrorCode.UnknownError);
            }
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

            return !string.IsNullOrWhiteSpace(request.Query);
        }
    }
}