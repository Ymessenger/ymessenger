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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class DeleteFilesRequestHandler : IRequestHandler
    {
        private readonly DeleteFilesRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IFilesService filesService;

        public DeleteFilesRequestHandler(Request request, ClientConnection clientConnection, INodeNoticeService nodeNoticeService, IFilesService filesService)
        {
            this.request = (DeleteFilesRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.filesService = filesService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                List<long> deletedFilesId =
                    await filesService.DeleteFilesAsync(request.FilesId, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                foreach (long fileId in deletedFilesId)
                {
                    BlockSegmentVm segment =
                        await BlockSegmentsService.Instance.CreateDeleteFileSegmentAsync(fileId,
                            NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                    BlockGenerationHelper.Instance.AddSegment(segment);
                }

                nodeNoticeService.SendDeleteFilesNodeNoticeAsync(deletedFilesId.ToList());
                return new ResultResponse(request.RequestId);
            }
            catch (ObjectDoesNotExistsException ex)
            {
                Logger.WriteLog(ex);
                return new ResultResponse(request.RequestId, "Files not found.", ObjectsLibrary.Enums.ErrorCode.DeleteFilesProblem);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
                throw new UnauthorizedUserException();
            if (!clientConnection.Confirmed)
                throw new PermissionDeniedException("User is not confirmed.");
            return request.FilesId != null
                   && request.FilesId.Any()
                   && request.FilesId.Count() < 100;
        }
    }
}