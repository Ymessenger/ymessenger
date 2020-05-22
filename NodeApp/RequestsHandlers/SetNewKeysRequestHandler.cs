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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class SetNewKeysRequestHandler : IRequestHandler
    {
        private readonly SetNewKeysRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IKeysService keysService;

        public SetNewKeysRequestHandler(Request request, ClientConnection clientConnection, INodeNoticeService nodeNoticeService, IKeysService keysService)
        {
            this.request = (SetNewKeysRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.keysService = keysService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                foreach (var key in request.Keys)
                {
                    key.UserId = clientConnection.UserId.GetValueOrDefault();
                }
                List<KeyVm> keys =
                    await keysService.AddNewUserKeysAsync(request.Keys, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateNewUserKeysSegmentAsync(
                    keys.ToList(),
                    clientConnection.UserId.GetValueOrDefault(),
                    NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                BlockGenerationHelper.Instance.AddSegment(segment);
                nodeNoticeService.SendNewKeysBlockNoticeAsync(keys.ToList(), clientConnection.UserId.GetValueOrDefault());
                return new KeysResponse(request.RequestId, keys);
            }
            catch (ObjectAlreadyExistsException)
            {
                var errorObject = new
                {
                    Key = "Key with the specified identifier already exists"
                };
                return new ResultResponse(request.RequestId, errorObject.ToJson(), ObjectsLibrary.Enums.ErrorCode.WrongArgumentError);
            }
            catch (KeyTimeoutException)
            {
                return new ResultResponse(request.RequestId, "Key timeout.", ObjectsLibrary.Enums.ErrorCode.KeyTimeout);
            }
            catch (InvalidSignException)
            {
                return new ResultResponse(request.RequestId, "Invalid sign.", ObjectsLibrary.Enums.ErrorCode.InvalidSign);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
            {
                throw new UnauthorizedUserException();
            }

            if (request.Keys == null || !request.Keys.Any())
            {
                return false;
            }

            foreach (var key in request.Keys)
            {
                if (key.Data == null || !key.Data.Any())
                {
                    return false;
                }
            }
            return true;
        }
    }
}