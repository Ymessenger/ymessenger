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
using MoreLinq;
using NodeApp.CacheStorageClasses;
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class SearchRequestHandler : IRequestHandler
    {
        private readonly SearchRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IConnectionsService connectionsService;
        private readonly ILoadChatsService loadChatsService;
        private readonly INodesService nodesService;
        private readonly ILoadUsersService loadUsersService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly IPrivacyService privacyService;
        private readonly INodeRequestSender nodeRequestSender;

        public SearchRequestHandler(
            Request request,
            ClientConnection clientConnection,
            IConnectionsService connectionsService,
            ILoadChatsService loadChatsService,
            INodesService nodesService,
            ILoadUsersService loadUsersService,
            ILoadChannelsService loadChannelsService,
            IPrivacyService privacyService,
            INodeRequestSender nodeRequestSender)
        {
            this.request = (SearchRequest)request;
            this.clientConnection = clientConnection;
            this.connectionsService = connectionsService;
            this.loadChatsService = loadChatsService;
            this.nodesService = nodesService;
            this.loadUsersService = loadUsersService;
            this.loadChannelsService = loadChannelsService;
            this.privacyService = privacyService;
            this.nodeRequestSender = nodeRequestSender;
        }

        public async Task<Response> CreateResponseAsync()
        {
            ConcurrentBag<UserVm> foundUsers = new ConcurrentBag<UserVm>();
            ConcurrentBag<ChannelVm> foundChannels = new ConcurrentBag<ChannelVm>();
            ConcurrentBag<ChatVm> foundChats = new ConcurrentBag<ChatVm>();
            var nodesInfo = await nodesService.GetAllNodesInfoAsync().ConfigureAwait(false);
            List<Task> searchTasks = new List<Task>();
            foreach (var node in nodesInfo)
            {
                Task task = Task.Run(async () =>
                {
                    var nodeConnection = connectionsService.GetNodeConnection(node.Id);
                    if (nodeConnection != null)
                    {
                        SearchNodeResponse searchNodeResponse = await nodeRequestSender.GetSearchResponseAsync(
                            request.SearchQuery, request.NavigationId, request.Direction, request.SearchTypes, clientConnection.UserId, nodeConnection).ConfigureAwait(false);
                        if (searchNodeResponse?.Users != null)
                        {
                            foundUsers.AddRange(searchNodeResponse.Users);
                        }

                        if (searchNodeResponse?.Chats != null)
                        {
                            foundChats.AddRange(searchNodeResponse.Chats);
                        }

                        if (searchNodeResponse?.Channels != null)
                        {
                            foundChannels.AddRange(searchNodeResponse.Channels);
                        }

                        await MetricsHelper.Instance.SetCrossNodeApiInvolvedAsync(request.RequestId).ConfigureAwait(false);
                    }
                    else if (NodeSettings.Configs.Node.Id == node.Id)
                    {
                        foreach (var searchType in request.SearchTypes.Distinct())
                        {
                            switch (searchType)
                            {
                                case SearchType.Users:
                                    {
                                        List<UserVm> users = await loadUsersService.FindUsersByStringQueryAsync(
                                            request.SearchQuery,
                                            request.NavigationId.GetValueOrDefault(),
                                            request.Direction.GetValueOrDefault(true)).ConfigureAwait(false);
                                        foundUsers.AddRange(privacyService.ApplyPrivacySettings(users, request.SearchQuery, clientConnection.UserId));
                                    }
                                    break;
                                case SearchType.Chats:
                                    {
                                        List<ChatVm> chats = await loadChatsService.FindChatsByStringQueryAsync(
                                            request.SearchQuery,
                                            request.NavigationId.GetValueOrDefault(),
                                            request.Direction.GetValueOrDefault(true),
                                            clientConnection.UserId).ConfigureAwait(false);
                                        foundChats.AddRange(chats);
                                    }
                                    break;
                                case SearchType.Channels:
                                    {
                                        List<ChannelVm> channels = await loadChannelsService.FindChannelsByStringQueryAsync(
                                           request.SearchQuery,
                                           request.NavigationId.GetValueOrDefault(),
                                           request.Direction.GetValueOrDefault(true)).ConfigureAwait(false);
                                        foundChannels.AddRange(channels);
                                    }
                                    break;
                                default:
                                    continue;
                            }
                        }
                    }
                });
                searchTasks.Add(task);
            }
            await Task.WhenAll(searchTasks).ConfigureAwait(false);
            foundChats = new ConcurrentBag<ChatVm>(foundChats.DistinctBy(opt => opt.Id));
            foundChannels = new ConcurrentBag<ChannelVm>(foundChannels.DistinctBy(opt => opt.ChannelId));
            return new SearchResponse(request.RequestId,
                request.Direction.GetValueOrDefault(true) ? foundUsers.OrderBy(opt => opt.Id) : foundUsers.OrderByDescending(opt => opt.Id),
                request.Direction.GetValueOrDefault(true) ? foundChats.OrderBy(opt => opt.Id) : foundChats.OrderByDescending(opt => opt.Id),
                request.Direction.GetValueOrDefault(true) ? foundChannels.OrderBy(opt => opt.ChannelId) : foundChannels.OrderByDescending(opt => opt.ChannelId));
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

            if (request.SearchTypes == null || !request.SearchTypes.Any())
            {
                request.SearchTypes = new List<SearchType> { SearchType.Users, SearchType.Chats, SearchType.Channels };
            }

            return request.SearchQuery != null;
        }
    }
}