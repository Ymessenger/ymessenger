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
using ObjectsLibrary.Blockchain.Services;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class UsersAddedToUserBlacklistNoticeHandler : ICommunicationHandler
    {
        private readonly UsersAddedToUserBlacklistNodeNotice notice;
        private readonly NodeConnection current;
        private readonly IUpdateUsersService updateUsersService;

        public UsersAddedToUserBlacklistNoticeHandler(NodeNotice notice, NodeConnection current, IUpdateUsersService updateUsersService)
        {
            this.notice = (UsersAddedToUserBlacklistNodeNotice)notice;
            this.current = current;
            this.updateUsersService = updateUsersService;
        }

        public async Task HandleAsync()
        {
            await updateUsersService.AddUsersToBlackListAsync(notice.BlacklistedUsersIds, notice.UserId).ConfigureAwait(false);
            var segment = await BlockSegmentsService.Instance.CreateUsersAddedToUserBlacklistSegmentAsync(
                notice.BlacklistedUsersIds,
                notice.UserId,
                current.Node.Id,
                NodeData.Instance.NodeKeys.SignPrivateKey,
                NodeData.Instance.NodeKeys.SymmetricKey,
                NodeData.Instance.NodeKeys.Password,
                NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
            BlockGenerationHelper.Instance.AddSegment(segment);
        }

        public bool IsObjectValid()
        {
            return notice.BlacklistedUsersIds != null && notice.BlacklistedUsersIds.Any() && notice.UserId != 0;
        }
    }
}