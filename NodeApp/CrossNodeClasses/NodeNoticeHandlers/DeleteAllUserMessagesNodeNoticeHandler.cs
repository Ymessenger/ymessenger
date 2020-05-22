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
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using System.Threading.Tasks;

namespace NodeApp
{
    public class DeleteAllUserMessagesNodeNoticeHandler : ICommunicationHandler
    {
        private readonly DeleteAllUserMessagesNodeNotice notice;
        private readonly NodeConnection nodeConnection;
        private readonly IDeleteMessagesService deleteMessagesService;
        private readonly ILoadUsersService loadUsersService;

        public DeleteAllUserMessagesNodeNoticeHandler(NodeNotice notice, NodeConnection nodeConnection, IDeleteMessagesService deleteMessagesService, ILoadUsersService loadUsersService)
        {
            this.notice = (DeleteAllUserMessagesNodeNotice)notice;
            this.nodeConnection = nodeConnection;
            this.deleteMessagesService = deleteMessagesService;
            this.loadUsersService = loadUsersService;
        }

        public async Task HandleAsync()
        {
            var user = await loadUsersService.GetUserAsync(notice.UserId).ConfigureAwait(false);
            if (user.NodeId != nodeConnection.Node.Id)
            {
                return;
            }

            if (notice.ConversationId != null && notice.ConversationType != null)
            {
                await deleteMessagesService.DeleteMessagesAsync(notice.ConversationId.Value, notice.ConversationType.Value, notice.UserId).ConfigureAwait(false);
            }
            else
            {
                await deleteMessagesService.DeleteMessagesAsync(notice.UserId).ConfigureAwait(false);
            }
        }

        public bool IsObjectValid()
        {
            return nodeConnection.Node != null;
        }
    }
}