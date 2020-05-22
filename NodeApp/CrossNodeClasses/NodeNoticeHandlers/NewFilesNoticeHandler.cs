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
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class NewFilesNoticeHandler : ICommunicationHandler
    {
        private readonly NewFilesNodeNotice notice;
        private readonly NodeConnection nodeConnection;
        private readonly IFilesService filesService;
        public NewFilesNoticeHandler(CommunicationObject @object, NodeConnection connection, IFilesService filesService)
        {
            notice = (NewFilesNodeNotice)@object;
            nodeConnection = connection;
            this.filesService = filesService;
        }
        public async Task HandleAsync()
        {
            foreach (var fileValuePair in notice.FilesValuePairs)
            {
                try
                {
                    fileValuePair.FirstValue.NodeId = nodeConnection.Node.Id;
                    await filesService.SaveFileAsync(fileValuePair.FirstValue, fileValuePair.FirstValue.UploaderId.GetValueOrDefault()).ConfigureAwait(false);
                    BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateNewFileSegmentAsync(
                        fileValuePair.FirstValue,
                        fileValuePair.SecondValue,
                        nodeConnection.Node.Id,
                        notice.KeyId).ConfigureAwait(false);
                    BlockGenerationHelper.Instance.AddSegment(segment);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex);
                }
            }
        }

        public bool IsObjectValid()
        {
            if (notice.FilesValuePairs == null || !notice.FilesValuePairs.Any() || nodeConnection.Node == null)
            {
                return false;
            }

            foreach (var item in notice.FilesValuePairs)
            {
                if (string.IsNullOrWhiteSpace(item.FirstValue.FileId))
                {
                    return false;
                }
            }
            return true;
        }
    }
}