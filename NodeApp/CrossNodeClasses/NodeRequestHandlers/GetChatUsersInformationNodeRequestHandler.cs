﻿/** 
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
using NodeApp.CrossNodeClasses.Requests;
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeRequestHandlers
{
    public class GetChatUsersInformationNodeRequestHandler : ICommunicationHandler
    {
        private readonly GetChatUsersInformationNodeRequest request;
        private readonly NodeConnection current;
        private readonly ILoadChatsService loadChatsService;

        public GetChatUsersInformationNodeRequestHandler(NodeRequest request, NodeConnection current, ILoadChatsService loadChatsService)
        {
            this.request = (GetChatUsersInformationNodeRequest) request;
            this.current = current;
            this.loadChatsService = loadChatsService;
        }

        public async Task HandleAsync()
        {
            try
            {
                List<ChatUserDto> chatUsers = await loadChatsService.GetChatUsersAsync(request.UsersId, request.ChatId).ConfigureAwait(false);
                ChatUsersNodeResponse response = new ChatUsersNodeResponse(request.RequestId, ChatUserConverter.GetChatUsersVm(chatUsers));
                NodeWebSocketCommunicationManager.SendResponse(response, current);
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
                NodeWebSocketCommunicationManager.SendResponse(new ResultNodeResponse(request.RequestId, ObjectsLibrary.Enums.ErrorCode.UnknownError), current);
            }
        }

        public bool IsObjectValid()
        {
            return current.Node != null 
                && request.ChatId != 0 
                && request.UsersId != null 
                && request.UsersId.Any();
        }
    }
}