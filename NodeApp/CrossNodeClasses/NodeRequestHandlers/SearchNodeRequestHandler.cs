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
using NodeApp.CrossNodeClasses.Requests;
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeRequestHandlers
{
    public class SearchNodeRequestHandler : ICommunicationHandler
    {
        private readonly SearchNodeRequest request;
        private readonly NodeConnection current;
        private readonly ILoadChatsService loadChatsService;
        private readonly ILoadUsersService loadUsersService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly IPrivacyService privacyService;

        public SearchNodeRequestHandler(NodeRequest request,
                                        NodeConnection current,
                                        ILoadChatsService loadChatsService,
                                        ILoadUsersService loadUsersService,
                                        ILoadChannelsService loadChannelsService,
                                        IPrivacyService privacyService)
        {
            this.request = (SearchNodeRequest) request;
            this.current = current;
            this.loadChatsService = loadChatsService;
            this.loadUsersService = loadUsersService;
            this.loadChannelsService = loadChannelsService;
            this.privacyService = privacyService;
        }

        public async Task HandleAsync()
        {
            try
            {
                List<UserVm> users = null;
                List<ChatVm> chats = null;
                List<ChannelVm> channels = null;
                foreach (var searchType in request.SearchTypes)
                {
                    switch (searchType)
                    {
                        case SearchType.Users:
                            users = await loadUsersService.FindUsersByStringQueryAsync(request.SearchQuery, request.NavigationId, request.Direction).ConfigureAwait(false);
                            break;
                        case SearchType.Chats:
                            chats = await loadChatsService.FindChatsByStringQueryAsync(request.SearchQuery, request.NavigationId, request.Direction, request.RequestorId).ConfigureAwait(false);
                            break;
                        case SearchType.Channels:
                            channels = await loadChannelsService.FindChannelsByStringQueryAsync(request.SearchQuery, request.NavigationId, request.Direction).ConfigureAwait(false);
                            break;
                        default:
                            continue;                            
                    }                   
                }
                SearchNodeResponse response = new SearchNodeResponse(
                    request.RequestId, 
                    privacyService.ApplyPrivacySettings(users, request.SearchQuery, request.RequestorId),
                    channels,
                    chats);
                NodeWebSocketCommunicationManager.SendResponse(response, current);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                NodeWebSocketCommunicationManager.SendResponse(new ResultNodeResponse(request.RequestId, ErrorCode.UnknownError, ex.ToString()), current);
            }
        }

        public bool IsObjectValid()
        {
            if (request.SearchTypes == null || !request.SearchTypes.Any())
                request.SearchTypes = new List<SearchType> { SearchType.Users, SearchType.Chats, SearchType.Channels };
            return request.SearchQuery != null && !string.IsNullOrWhiteSpace(request.SearchQuery) && current.Node != null;
        }
    }
}