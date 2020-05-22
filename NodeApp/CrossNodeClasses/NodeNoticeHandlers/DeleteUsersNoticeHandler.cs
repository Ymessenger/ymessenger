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
using NodeApp.Blockchain;
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class DeleteUsersNoticeHandler : ICommunicationHandler
    {
        private readonly DeleteUsersNodeNotice notice;
        private readonly NodeConnection nodeConnection;
        private readonly IDeleteUsersService deleteUsersService;
        public DeleteUsersNoticeHandler(CommunicationObject @object, NodeConnection nodeConnection, IDeleteUsersService deleteUsersService)
        {
            notice = (DeleteUsersNodeNotice)@object;
            this.nodeConnection = nodeConnection;
            this.deleteUsersService = deleteUsersService;
        }
        public async Task HandleAsync()
        {
            List<BlockSegmentVm> segments = new List<BlockSegmentVm>();
            foreach (long userId in notice.UsersIds)
            {
                segments.Add(await BlockSegmentsService.Instance.CreateDeleteUserSegmentAsync(userId, null, nodeConnection.Node.Id).ConfigureAwait(false));
            }
            BlockGenerationHelper.Instance.AddSegments(segments);
            await deleteUsersService.DeleteUsersAsync(notice.UsersIds).ConfigureAwait(false);
        }

        public bool IsObjectValid()
        {
            return notice.UsersIds != null
                && notice.UsersIds.Any()
                && nodeConnection.Node != null;
        }
    }
}