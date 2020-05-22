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
using NodeApp.CrossNodeClasses.Requests;
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeRequestHandlers
{
    public class CheckTokenRequestHandler : ICommunicationHandler
    {
        private readonly CheckTokenNodeRequest request;
        private readonly NodeConnection current;
        private readonly ITokensService tokensService;
        private readonly ILoadUsersService loadUsersService;
        public CheckTokenRequestHandler(CommunicationObject request, NodeConnection current, ITokensService tokensService, ILoadUsersService loadUsersService)
        {
            this.request = (CheckTokenNodeRequest)request;
            this.current = current;
            this.loadUsersService = loadUsersService;
            this.tokensService = tokensService;
        }
        public async Task HandleAsync()
        {
            NodeResponse response = default;
            try
            {                
                TokenVm token = await tokensService.CheckTokenAsync(request.Token, NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                UserVm user = await loadUsersService.GetUserAsync(token.UserId).ConfigureAwait(false);
                response = new UserTokensNodeResponse(request.RequestId, token, user);
            }
            catch (InvalidTokenException)
            {
                response = new ResultNodeResponse(request.RequestId, ErrorCode.InvalidAccessToken);
            }
            catch (TokensTimeoutException)
            {
                response = new ResultNodeResponse(request.RequestId, ErrorCode.AccessTokenTimeout);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                response = new ResultNodeResponse(request.RequestId, ErrorCode.UnknownError);
            }
            finally
            {
                NodeWebSocketCommunicationManager.SendResponse(response, current);               
            }
        }

        public bool IsObjectValid()
        {            
            return !string.IsNullOrWhiteSpace(request.Token.AccessToken)
                && request.UserId != 0
                && current.Node != null;
        }
    }
}