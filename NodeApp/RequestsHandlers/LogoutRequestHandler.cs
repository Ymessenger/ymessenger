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
using NodeApp.Interfaces.Services;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class LogoutRequestHandler : IRequestHandler
    {
        private readonly LogoutRequest request;
        private readonly long userId;
        private readonly ClientConnection current;
        private readonly IConnectionsService connectionsService;
        private readonly ITokensService tokensService;
        public LogoutRequestHandler(Request request, ClientConnection current, IConnectionsService connectionsService, ITokensService tokensService)
        {
            this.request = (LogoutRequest)request;
            userId = current.UserId.GetValueOrDefault();
            this.current = current;
            this.connectionsService = connectionsService;
            this.tokensService = tokensService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                var removedTokens = await tokensService.RemoveTokensAsync(userId, request.AccessToken, request.TokensIds).ConfigureAwait(false);
                var clientConnections = connectionsService.GetUserClientConnections(userId);
                if (clientConnections != null)
                {
                    foreach (var connection in clientConnections)
                    {
                        if (removedTokens.Any(token => token.AccessToken == connection.CurrentToken.AccessToken))
                        {
                            Task closeConnection = new Task(async () =>
                            {
                                try
                                {
                                    if (connection == current)
                                    {
                                        await Task.Delay(4000).ConfigureAwait(false);
                                    }

                                    await connection.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection is closing.", CancellationToken.None).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteLog(ex);
                                }
                            });
                            closeConnection.Start();
                        }
                    }
                }
                return new ResultResponse(request.RequestId);
            }
            catch (LogoutException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Invalid access token.", ErrorCode.InvalidAccessToken);
            }
        }

        public bool IsRequestValid()
        {
            if (userId == 0)
            {
                throw new UnauthorizedUserException();
            }

            return !string.IsNullOrWhiteSpace(request.AccessToken)
                || (request.TokensIds != null && request.TokensIds.Any());
        }
    }
}