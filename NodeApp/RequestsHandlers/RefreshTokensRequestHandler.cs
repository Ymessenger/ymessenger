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
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class RefreshTokensRequestHandler : IRequestHandler
    {
        private readonly RefreshTokensRequest request;
        private readonly ClientConnection clientConnection;
        private readonly ITokensService tokensService;
        public RefreshTokensRequestHandler(Request request, ClientConnection clientConnection, ITokensService tokensService)
        {
            this.request = (RefreshTokensRequest)request;
            this.clientConnection = clientConnection;
            this.tokensService = tokensService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                TokenVm token = await tokensService.RefreshTokenPairAsync(request.UserId, request.RefreshToken).ConfigureAwait(false);
                clientConnection.FileAccessToken = RandomExtensions.NextString(64);
                return new TokensResponse(
                    request.RequestId,
                    token.UserId,
                    token.AccessToken,
                    token.RefreshToken,
                    clientConnection.FileAccessToken);
            }
            catch (InvalidTokenException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Invalid tokens.", ErrorCode.InvalidAccessToken);
            }
        }

        public bool IsRequestValid()
        {
            return !string.IsNullOrWhiteSpace(request.RefreshToken)
                    && request.UserId != 0;
        }
    }
}