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
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System;
using System.Threading.Tasks;

namespace NodeApp
{
#pragma warning disable CS1998
    public class DeleteAllMessagesRequestHandler : IRequestHandler
    {
        private readonly DeleteAllMessagesRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IDeleteMessagesService deleteMessagesService;
        private readonly INodeNoticeService nodeNoticeService;

        public DeleteAllMessagesRequestHandler(Request request, ClientConnection clientConnection, IDeleteMessagesService deleteMessagesService, INodeNoticeService nodeNoticeService)
        {
            this.request = (DeleteAllMessagesRequest)request;
            this.clientConnection = clientConnection;
            this.deleteMessagesService = deleteMessagesService;
            this.nodeNoticeService = nodeNoticeService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                Task deleteMessageTask = new Task(async () =>
                {
                    try
                    {
                        if (request.ConversationId != null && request.ConversationType != null)
                        {
                            await deleteMessagesService.DeleteMessagesAsync(request.ConversationId.Value, request.ConversationType.Value, clientConnection.UserId.Value).ConfigureAwait(false);
                        }
                        else
                        {
                            await deleteMessagesService.DeleteMessagesAsync(clientConnection.UserId.Value).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex);
                    }
                });
                deleteMessageTask.Start();
                nodeNoticeService.SendAllMessagesDeletedNodeNoticeAsync(clientConnection.UserId.Value, request.ConversationId, request.ConversationType);
                return new ResultResponse(request.RequestId);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return new ResultResponse(request.RequestId, "An error occurred while deleting messages.", ObjectsLibrary.Enums.ErrorCode.DeleteMessagesProblem);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
            {
                throw new UnauthorizedUserException();
            }

            return (request.ConversationId == null && request.ConversationType == null)
                || (request.ConversationType != null && request.ConversationId != null);
        }
    }
}