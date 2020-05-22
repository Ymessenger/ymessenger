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
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class DeleteUserKeysNodeNoticeHandler : ICommunicationHandler
    {
        private readonly DeleteUserKeysNodeNotice notice;
        private readonly NodeConnection current;
        private readonly IKeysService keysService;

        public DeleteUserKeysNodeNoticeHandler(NodeNotice notice, NodeConnection current, IKeysService keysService)
        {
            this.notice = (DeleteUserKeysNodeNotice)notice;
            this.current = current;
            this.keysService = keysService;
        }

        public async Task HandleAsync()
        {
            List<KeyVm> deletedKeys = await keysService.DeleteUserKeysAsync(notice.KeysId, notice.UserId).ConfigureAwait(false);
            if (deletedKeys.Any())
            {
                List<long> deletedKeysId = deletedKeys.Select(opt => opt.KeyId).ToList();
                BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateDeleteUserKeysSegmentAsync(deletedKeysId, notice.UserId, current.Node.Id).ConfigureAwait(false);
                BlockGenerationHelper.Instance.AddSegment(segment);
            }
        }

        public bool IsObjectValid()
        {
            return notice.KeysId != null && notice.KeysId.Any() && notice.UserId != 0;
        }
    }
}