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
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Threading.Tasks;

namespace NodeApp
{
    public class CheckQRCodeRequestHandler : IRequestHandler
    {
        private readonly CheckQRCodeRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INoticeService noticeService;
        private readonly IQRCodesService qrCodesService;
        private readonly ILoadUsersService loadUsersService;

        public CheckQRCodeRequestHandler(Request request, ClientConnection clientConnection, INoticeService noticeService, IQRCodesService qrCodesService, ILoadUsersService loadUsersService)
        {
            this.request = (CheckQRCodeRequest)request;
            this.clientConnection = clientConnection;
            this.noticeService = noticeService;
            this.qrCodesService = qrCodesService;
            this.loadUsersService = loadUsersService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                string fileAccessToken = RandomExtensions.NextString(64);
                var token = await qrCodesService.CreateTokenByQRCodeAsync(request.QR, request.DeviceTokenId, request.OSName, request.DeviceName, request.AppName).ConfigureAwait(false);
                var user = await loadUsersService.GetUserAsync(token.UserId).ConfigureAwait(false);
                clientConnection.UserId = user.Id;
                clientConnection.FileAccessToken = fileAccessToken;
                clientConnection.CurrentToken = token;
                clientConnection.Confirmed = user.Confirmed;
                clientConnection.CurrentDeviceTokenId = token.DeviceTokenId;
                noticeService.SendNewSessionNoticeAsync(clientConnection);
                return new TokensResponse(request.RequestId, token, fileAccessToken, null, user);
            }
            catch (ObjectDoesNotExistsException ex)
            {
                return new ResultResponse(request.RequestId, ex.Message, ObjectsLibrary.Enums.ErrorCode.ObjectDoesNotExists);
            }
        }

        public bool IsRequestValid()
        {
            return request.QR != null && !string.IsNullOrEmpty(request.QR.Sequence);
        }
    }
}