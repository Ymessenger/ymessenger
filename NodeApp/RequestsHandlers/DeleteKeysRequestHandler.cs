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
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class DeleteKeysRequestHandler : IRequestHandler
    {
        private readonly DeleteKeysRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IKeysService keysService;

        public DeleteKeysRequestHandler(Request request, ClientConnection clientConnection, INodeNoticeService nodeNoticeService, IKeysService keysService)
        {
            this.request = (DeleteKeysRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.keysService = keysService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<KeyVm> deletedKeys = await keysService.DeleteUserKeysAsync(request.KeysId,
                    clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
            if (deletedKeys.Any())
            {
                List<long> deletedKeysId = deletedKeys.Select(opt => opt.KeyId).ToList();
                BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateDeleteUserKeysSegmentAsync(
                    deletedKeysId,
                    clientConnection.UserId.GetValueOrDefault(),
                    NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                BlockGenerationHelper.Instance.AddSegment(segment);
                nodeNoticeService.SendDeleteUserKeysNodeNoticeAsync(deletedKeysId,
                    clientConnection.UserId.GetValueOrDefault());
                return new KeysResponse(request.RequestId, deletedKeys);
            }
            return new ResultResponse(request.RequestId, "Keys not found.", ErrorCode.ObjectDoesNotExists);
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

            return request.KeysId != null && request.KeysId.Any();
        }
    }
}