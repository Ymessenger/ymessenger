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
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class DeleteFilesNoticeHandler : ICommunicationHandler
    {
        private readonly DeleteFilesNodeNotice notice;
        private readonly NodeConnection nodeConnection;
        private readonly IFilesService filesService;
        public DeleteFilesNoticeHandler(CommunicationObject @object, NodeConnection nodeConnection, IFilesService filesService)
        {
            notice = (DeleteFilesNodeNotice)@object;
            this.nodeConnection = nodeConnection;
            this.filesService = filesService;
        }
        public async Task HandleAsync()
        {
            await filesService.DeleteFilesAsync(notice.FilesIds).ConfigureAwait(false);
            foreach (long fileId in notice.FilesIds)
            {
                BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateDeleteFileSegmentAsync(fileId, nodeConnection.Node.Id).ConfigureAwait(false);
                BlockGenerationHelper.Instance.AddSegment(segment);
            }
        }
        public bool IsObjectValid()
        {
            return notice.FilesIds != null
                && notice.FilesIds.Any()
                && notice.NodeId != 0
                && nodeConnection.Node != null;
        }
    }
}