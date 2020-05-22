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
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetChatUsersRequestHandler : IRequestHandler
    {
        private readonly GetChatUsersRequest request;
        private readonly ClientConnection clientConnection;
        private readonly ILoadChatsService loadChatsService;
        private readonly IPrivacyService privacyService;
        private readonly IConnectionsService connectionsService;
        private readonly INodeRequestSender nodeRequestSender;
        private readonly ICrossNodeService crossNodeService;

        public GetChatUsersRequestHandler(
            Request request, ClientConnection clientConnection, ILoadChatsService loadChatsService,
            IPrivacyService privacyService, IConnectionsService connectionsService, INodeRequestSender nodeRequestSender, ICrossNodeService crossNodeService)
        {
            this.request = (GetChatUsersRequest)request;
            this.clientConnection = clientConnection;
            this.loadChatsService = loadChatsService;
            this.privacyService = privacyService;
            this.connectionsService = connectionsService;
            this.nodeRequestSender = nodeRequestSender;
            this.crossNodeService = crossNodeService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                List<ChatUserVm> chatUsers = await loadChatsService.GetChatUsersAsync(
                    request.ChatId,
                    clientConnection.UserId.GetValueOrDefault(),
                    100,
                    request.NavigationUserId.GetValueOrDefault()).ConfigureAwait(false);
                if (!chatUsers.Any(user => user.UserRole == UserRole.Creator))
                {
                    var nodesIds = await loadChatsService.GetChatNodeListAsync(request.ChatId).ConfigureAwait(false);
                    var nodeConnection = connectionsService.GetNodeConnection(nodesIds.FirstOrDefault(id => id != NodeSettings.Configs.Node.Id));
                    if (nodeConnection != null)
                    {
                        var chat = await nodeRequestSender.GetFullChatInformationAsync(request.ChatId, nodeConnection).ConfigureAwait(false);
                        if (chat != null)
                        {
                            await crossNodeService.NewOrEditChatAsync(chat).ConfigureAwait(false);
                            chatUsers = chat.Users?.Take(100).ToList();
                        }
                    }
                }
                chatUsers.ForEach(item =>
                {
                    if (item.UserInfo != null)
                    {
                        item.UserInfo = privacyService.ApplyPrivacySettings(
                            item.UserInfo,
                            item.UserInfo.Privacy);
                    }
                });
                return new ChatUsersResponse(request.RequestId, chatUsers);
            }
            catch (GetUsersException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "User does not have access to the chat.", ErrorCode.PermissionDenied);
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

            if (request.ChatId == 0)
            {
                return false;
            }

            return true;
        }
    }
}