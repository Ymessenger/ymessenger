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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetFilesInfoRequestHandler : IRequestHandler
    {
        private readonly GetFilesInfomationRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IFilesService filesService;

        public GetFilesInfoRequestHandler(Request request, ClientConnection clientConnection, IFilesService filesService)
        {
            this.request = (GetFilesInfomationRequest)request;
            this.clientConnection = clientConnection;
            this.filesService = filesService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<FileInfoVm> filesInfo = await filesService.GetFilesInfoAsync(request.FilesId).ConfigureAwait(false);
            return new FilesResponse(request.RequestId, filesInfo);
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

            return request.FilesId != null
                && request.FilesId.Count < 100
                && request.FilesId.Any();
        }
    }
}
