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
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Objects;
using System;
using System.Threading.Tasks;

namespace NodeApp
{
    public class FilesInformationNodeResponseHandler : ICommunicationHandler
    {
        private readonly FilesInformationResponse response;
        private readonly IFilesService filesService;
        private readonly NodeConnection nodeConnection;

        public FilesInformationNodeResponseHandler(NodeResponse response, NodeConnection nodeConnection, IFilesService filesService)
        {
            this.response = (FilesInformationResponse)response;
            this.filesService = filesService;
            this.nodeConnection = nodeConnection;
        }

        public async Task HandleAsync()
        {
            try
            {
                await filesService.CreateFilesInformationAsync(response.FilesInfo).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public bool IsObjectValid()
        {
            return nodeConnection.Node != null && !response.FilesInfo.IsNullOrEmpty();
        }
    }
}