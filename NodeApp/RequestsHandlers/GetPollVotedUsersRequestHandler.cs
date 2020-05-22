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
using NodeApp.CacheStorageClasses;
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetPollVotedUsersRequestHandler : IRequestHandler
    {
        private readonly GetPollVotedUsersRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IConnectionsService connectionsService;
        private readonly IPollsService pollsService;
        private readonly INodeRequestSender nodeRequestSender;
        public GetPollVotedUsersRequestHandler(Request request, ClientConnection clientConnection, IConnectionsService connectionsService, IPollsService pollsService, INodeRequestSender nodeRequestSender)
        {
            this.request = (GetPollVotedUsersRequest)request;
            this.clientConnection = clientConnection;
            this.connectionsService = connectionsService;
            this.pollsService = pollsService;
            this.nodeRequestSender = nodeRequestSender;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                var pollResults = await pollsService.GetPollVotedUsersAsync(
                    request.PollId,
                    request.ConversationId,
                    request.ConversationType,
                    request.OptionId,
                    clientConnection.UserId.GetValueOrDefault(),
                    30,
                    request.NavigationUserId.GetValueOrDefault()).ConfigureAwait(false);
                var usersGroups = pollResults.GroupBy(opt => opt.FirstValue.NodeId);
                List<Task> getUsersTasks = new List<Task>();
                var resultUsers = new ConcurrentBag<VoteInfo>();
                foreach (var group in usersGroups)
                {
                    var nodeConnection = connectionsService.GetNodeConnection(group.Key.GetValueOrDefault());
                    if (nodeConnection != null)
                    {
                        await MetricsHelper.Instance.SetCrossNodeApiInvolvedAsync(request.RequestId).ConfigureAwait(false);
                        getUsersTasks.Add(Task.Run(async () =>
                        {
                            var usersInfo = await nodeRequestSender.GetUsersInfoAsync(group.Select(opt => opt.FirstValue.Id).ToList(), clientConnection.UserId, nodeConnection).ConfigureAwait(false);
                            resultUsers.AddRange(usersInfo.Select(user => new VoteInfo(user, pollResults.FirstOrDefault(opt => opt.FirstValue.Id == user.Id).SecondValue)));
                        }));
                    }
                    else if (group.Key == NodeSettings.Configs.Node.Id)
                    {
                        var users = UserConverter.GetUsersVm(group.Select(opt => opt.FirstValue).ToList(), clientConnection.UserId);
                        resultUsers.AddRange(users.Select(user => new VoteInfo(user, pollResults.FirstOrDefault(opt => opt.FirstValue.Id == user.Id).SecondValue)));
                    }
                }
                await Task.WhenAll(getUsersTasks).ConfigureAwait(false);
                return new PollResultsResponse(request.RequestId, resultUsers.OrderBy(opt => opt.User.Id));
            }
            catch (PermissionDeniedException ex)
            {
                Logger.WriteLog(ex);
                return new ResultResponse(request.RequestId, "User does not have access to voted users list.", ErrorCode.PermissionDenied);
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

            return true;
        }
    }
}