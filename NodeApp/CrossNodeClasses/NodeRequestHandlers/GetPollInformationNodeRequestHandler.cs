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
using NodeApp.Objects;
using System;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeRequestHandlers
{
    public class GetPollInformationNodeRequestHandler : ICommunicationHandler
    {
        private readonly GetPollInformationNodeRequest request;
        private readonly NodeConnection current;
        private readonly IPollsService pollsService;

        public GetPollInformationNodeRequestHandler(NodeRequest request, NodeConnection current, IPollsService pollsService)
        {
            this.request = (GetPollInformationNodeRequest) request;
            this.current = current;
            this.pollsService = pollsService;
        }

        public async Task HandleAsync()
        {
            try
            {
                var pollDto = await pollsService.GetPollAsync(request.PollId, request.ConversationId, request.ConversationType).ConfigureAwait(false);
                NodeWebSocketCommunicationManager.SendResponse(new PollNodeResponse(request.RequestId, pollDto), current);
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
                NodeWebSocketCommunicationManager.SendResponse(new ResultNodeResponse(request.RequestId, ObjectsLibrary.Enums.ErrorCode.UnknownError), current);
            }
        }

        public bool IsObjectValid()
        {
            return current.Node != null;
        }
    }
}