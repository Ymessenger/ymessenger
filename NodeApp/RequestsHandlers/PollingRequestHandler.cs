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
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class PollingRequestHandler : IRequestHandler
    {
        private readonly PollingRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly ILoadChatsService loadChatsService;
        private readonly IPollsService pollsService;
        private readonly ILoadChannelsService loadChannelsService;

        public PollingRequestHandler(
            Request request,
            ClientConnection clientConnection,
            INodeNoticeService nodeNoticeService,
            ILoadChatsService loadChatsService,
            IPollsService pollsService,
            ILoadChannelsService loadChannelsService)
        {
            this.request = (PollingRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.loadChatsService = loadChatsService;
            this.pollsService = pollsService;
            this.loadChannelsService = loadChannelsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                PollDto poll;
                poll = await pollsService.VotePollAsync(
                    request.PollId,
                    request.ConversationId,
                    request.ConversationType,
                    request.Options,
                    clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);

                List<long> nodesId = null;
                switch (poll.ConversationType)
                {
                    case ConversationType.Chat:
                        {
                            nodesId = await loadChatsService.GetChatNodeListAsync(poll.ConversationId).ConfigureAwait(false);
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            nodesId = await loadChannelsService.GetChannelNodesIdAsync(poll.ConversationId).ConfigureAwait(false);
                        }
                        break;
                }
                nodeNoticeService.SendPollingNodeNoticeAsync(
                    poll.PollId,
                    poll.ConversationId,
                    poll.ConversationType,
                    request.Options,
                    clientConnection.UserId.GetValueOrDefault(),
                    nodesId);
                return new PollResponse(request.RequestId, PollConverter.GetPollVm(poll, clientConnection.UserId.GetValueOrDefault()));
            }
            catch (PermissionDeniedException ex)
            {
                return new ResultResponse(request.RequestId, ex.Message, ErrorCode.PermissionDenied);
            }
            catch (ObjectDoesNotExistsException ex)
            {
                return new ResultResponse(request.RequestId, ex.Message, ErrorCode.ObjectDoesNotExists);
            }
            catch (InvalidOperationException ex)
            {
                return new ResultResponse(request.RequestId, ex.Message, ErrorCode.InvalidArgument);
            }
            catch (InvalidSignException ex)
            {
                return new ResultResponse(request.RequestId, ex.Message, ErrorCode.InvalidSign);
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

            return (request.Options != null && request.Options.Any() && request.Options.Count <= 10);
        }
    }
}