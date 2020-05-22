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
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Objects;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class PollingNodeNoticeHandler : ICommunicationHandler
    {
        private readonly PollingNodeNotice notice;
        private readonly NodeConnection nodeConnection;
        private readonly IPollsService pollsService;
        private readonly INodeRequestSender nodeRequestSender;

        public PollingNodeNoticeHandler(NodeNotice notice, NodeConnection nodeConnection, IPollsService pollsService, INodeRequestSender nodeRequestSender)
        {
            this.notice = (PollingNodeNotice)notice;
            this.nodeConnection = nodeConnection;
            this.pollsService = pollsService;
            this.nodeRequestSender = nodeRequestSender;
        }

        public async Task HandleAsync()
        {
            try
            {
                if (notice.OptionsId != null && notice.OptionsId.Any())
                {
                    await pollsService.VotePollAsync(
                        notice.PollId,
                        notice.ConversationId,
                        notice.ConversationType,
                        notice.OptionsId,
                        notice.VotedUserId).ConfigureAwait(false);
                }
                else
                {
                    await pollsService.VotePollAsync(
                        notice.PollId,
                        notice.ConversationId,
                        notice.ConversationType,
                        notice.SignedOptions,
                        notice.VotedUserId).ConfigureAwait(false);
                }
            }
            catch (ObjectDoesNotExistsException)
            {
                var pollDto = await nodeRequestSender.GetPollInformationAsync(
                    notice.ConversationId,
                    notice.ConversationType,
                    notice.PollId,
                    nodeConnection).ConfigureAwait(false);
                await pollsService.SavePollAsync(pollDto).ConfigureAwait(false);
            }
        }

        public bool IsObjectValid()
        {
            return (notice.OptionsId != null && notice.OptionsId.Any())
                || (notice.SignedOptions != null && notice.SignedOptions.Any());
        }
    }
}