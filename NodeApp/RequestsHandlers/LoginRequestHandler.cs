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
using NodeApp.CacheStorageClasses;
using NodeApp.ExceptionClasses;
using NodeApp.Helpers;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class LoginRequestHandler : IRequestHandler
    {
        private readonly LoginRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IConnectionsService connectionsService;
        private readonly INoticeService noticeService;
        private readonly ITokensService tokensService;
        private readonly ILoadUsersService loadUsersService;
        private readonly IPendingMessagesService pendingMessagesService;
        private readonly INodeRequestSender nodeRequestSender;
        public LoginRequestHandler(Request request,
                                   ClientConnection clientConnection,
                                   IConnectionsService connectionsService,
                                   INoticeService noticeService,
                                   ITokensService tokensService,
                                   ILoadUsersService loadUsersService,
                                   IPendingMessagesService pendingMessagesService,
                                   INodeRequestSender nodeRequestSender)
        {
            this.request = (LoginRequest)request;
            this.clientConnection = clientConnection;
            this.connectionsService = connectionsService;
            this.noticeService = noticeService;
            this.tokensService = tokensService;
            this.loadUsersService = loadUsersService;
            this.pendingMessagesService = pendingMessagesService;
            this.nodeRequestSender = nodeRequestSender;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                clientConnection.FileAccessToken = RandomExtensions.NextString(64);
                TokensResponse response = default;
                if (request.DeviceTokenId != null && !string.IsNullOrWhiteSpace(request.DeviceTokenId))
                {
                    await tokensService.SetDeviceTokenIdNullAsync(request.DeviceTokenId).ConfigureAwait(false);
                }

                UserVm user = null;
                switch (request.LoginType)
                {
                    case LoginType.VerificationCode:
                        {
                            TokenVm token = null;
                            if (request.UidType == UidType.Phone)
                            {
                                token = await tokensService.PhoneVCodeCreateTokenPairAsync(request.Uid, request.VCode.GetValueOrDefault(), request.DeviceTokenId).ConfigureAwait(false);
                            }
                            else if (request.UidType == UidType.Email)
                            {
                                token = await tokensService.EmailVCodeCreateTokenPairAsync(request.Uid, request.VCode.GetValueOrDefault(), request.DeviceTokenId).ConfigureAwait(false);
                            }
                            else if (request.UidType == UidType.UserId)
                            {
                                token = await tokensService.UserIdVCodeCreateTokenPairAsync(Convert.ToInt64(request.Uid), request.VCode.GetValueOrDefault(), request.DeviceTokenId).ConfigureAwait(false);
                            }
                            clientConnection.UserId = token.UserId;
                            token = await tokensService.UpdateTokenDataAsync(request.OSName, request.DeviceName, request.AppName, clientConnection.ClientIP.ToString(), token).ConfigureAwait(false);
                            clientConnection.CurrentToken = token;
                            user = await loadUsersService.GetUserInformationAsync(token.UserId).ConfigureAwait(false);
                            response = new TokensResponse(request.RequestId, token, clientConnection.FileAccessToken, null, user);
                            noticeService.SendNewSessionNoticeAsync(clientConnection);
                        }
                        break;
                    case LoginType.AccessTokenAndUserId:
                        {
                            TokenVm token = new TokenVm
                            {
                                UserId = request.Token.UserId,
                                AccessToken = request.Token.AccessToken,
                                RefreshToken = request.Token.RefreshToken,
                                DeviceTokenId = request.DeviceTokenId
                            };
                            token = await tokensService.CheckTokenAsync(token, NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                            clientConnection.UserId = token.UserId;
                            token = await tokensService.UpdateTokenDataAsync(request.OSName, request.DeviceName, request.AppName, clientConnection.ClientIP.ToString(), token).ConfigureAwait(false);
                            clientConnection.CurrentToken = token;
                            user = await loadUsersService.GetUserInformationAsync(token.UserId).ConfigureAwait(false);
                            response = new TokensResponse(request.RequestId, token, clientConnection.FileAccessToken, null, user);
                        }
                        break;
                    case LoginType.Password:
                        {
                            ValuePair<TokenVm, string> tokenPasswordPair;
                            if (request.UidType == UidType.Phone)
                            {
                                tokenPasswordPair = await tokensService.PhonePasswordCreateTokenPairAsync(request.Uid, request.Password, request.DeviceTokenId).ConfigureAwait(false);
                            }
                            else if (request.UidType == UidType.Email)
                            {
                                tokenPasswordPair = await tokensService.EmailPasswordCreateTokenPairAsync(request.Uid, request.Password, request.DeviceTokenId).ConfigureAwait(false);
                            }
                            else if (request.UidType == UidType.UserId)
                            {
                                tokenPasswordPair = await tokensService.UserIdPasswordCreateTokenPairAsync(Convert.ToInt64(request.Uid), request.Password, request.DeviceTokenId).ConfigureAwait(false);
                            }
                            else
                            {
                                var errorObject = new
                                {
                                    UidType = "Unknown UidType."
                                };
                                return new ResultResponse(request.RequestId, ObjectSerializer.ObjectToJson(errorObject), ErrorCode.WrongArgumentError);
                            }
                            clientConnection.UserId = tokenPasswordPair.FirstValue.UserId;
                            tokenPasswordPair.FirstValue =
                                await tokensService.UpdateTokenDataAsync(request.OSName, request.DeviceName, request.AppName, clientConnection.ClientIP.ToString(), tokenPasswordPair.FirstValue)
                                .ConfigureAwait(false);
                            clientConnection.CurrentToken = tokenPasswordPair.FirstValue;
                            user = await loadUsersService.GetUserInformationAsync(clientConnection.UserId.Value).ConfigureAwait(false);
                            response = new TokensResponse(
                                request.RequestId,
                                tokenPasswordPair.FirstValue,
                                clientConnection.FileAccessToken,
                                tokenPasswordPair.SecondValue,
                                user);
                            noticeService.SendNewSessionNoticeAsync(clientConnection);
                        }
                        break;
                }
                clientConnection.Confirmed = user.Confirmed;
                clientConnection.Banned = user.Banned;
                connectionsService.AddOrUpdateUserConnection(clientConnection.UserId.Value, clientConnection);
                SendPendingMessagesAsync();
                return response;
            }
            catch (InvalidTokenException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Invalid token.", ErrorCode.InvalidAccessToken);
            }
            catch (WrongVerificationCodeException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Invalid verification code.", ErrorCode.WrongVerificationCode);
            }
            catch (UserNotFoundException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "User not found.", ErrorCode.UserNotFound);
            }
            catch (CreateTokenPairException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Login failed.", ErrorCode.AuthorizationProblem);
            }
            catch (TokensTimeoutException ex)
            {
                Logger.WriteLog(ex, request);
                await noticeService.SendNeedLoginNoticeAsync(clientConnection).ConfigureAwait(false);
                return new ResultResponse(request.RequestId, "Refresh token expired.", ErrorCode.RefreshTokenTimeout);
            }
            catch (UserFromAnotherNodeException ex)
            {
                await MetricsHelper.Instance.SetCrossNodeApiInvolvedAsync(request.RequestId).ConfigureAwait(false);
                var userToken = await nodeRequestSender.CheckTokenAsync(
                    request.Token.UserId,
                    request.Token,
                    ex.NodeId.GetValueOrDefault()).ConfigureAwait(false);
                if (userToken != null)
                {
                    clientConnection.UserId = userToken.FirstValue.UserId;
                    clientConnection.ProxyNodeWebSocket = connectionsService.GetNodeConnection(ex.NodeId.GetValueOrDefault()).NodeWebSocket;
                    connectionsService.AddOrUpdateUserConnection(clientConnection.UserId.Value, clientConnection);
                    return new TokensResponse(request.RequestId, userToken.FirstValue, clientConnection.FileAccessToken, null, userToken.SecondValue);
                }
                return new ResultResponse(request.RequestId, "Login failed.", ErrorCode.AuthorizationProblem);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, null, ErrorCode.UnknownError);
            }
        }

        private async void SendPendingMessagesAsync()
        {
            try
            {
                var pendingMessages = await pendingMessagesService.GetUserPendingMessagesAsync(clientConnection.UserId.Value).ConfigureAwait(false);
                await pendingMessagesService.RemovePendingMessagesAsync(pendingMessages.Select(message => message.Id)).ConfigureAwait(false);
                noticeService.SendPendingMessagesAsync(pendingMessages, clientConnection.UserId.Value);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId.GetValueOrDefault() > 0)
            {
                return false;
            }
            if (string.IsNullOrEmpty(request.DeviceName) || string.IsNullOrEmpty(request.OSName) || string.IsNullOrEmpty(request.AppName))
            {
                return false;
            }

            switch (request.UidType)
            {
                case UidType.Phone:
                    {
                        if (!ValidationHelper.IsPhoneNumberValid(request.Uid))
                        {
                            return false;
                        }
                    }
                    break;
                case UidType.UserId:
                    {
                        if (request.Token?.UserId == 0 && !long.TryParse(request.Uid, out _))
                        {
                            return false;
                        }
                    }
                    break;
                case UidType.Email:
                    {
                        if (!ValidationHelper.IsEmailValid(request.Uid))
                        {
                            return false;
                        }
                    }
                    break;
                default: return false;
            }
            switch (request.LoginType)
            {
                case LoginType.AccessTokenAndUserId:
                    {
                        if (request.Token == null || string.IsNullOrWhiteSpace(request.Token.AccessToken) || request.Token.UserId == 0)
                        {
                            return false;
                        }
                    }
                    break;
                case LoginType.VerificationCode:
                    {
                        if (request.VCode == null || request.VCode == 0)
                        {
                            return false;
                        }
                    }
                    break;
                case LoginType.Password:
                    {
                        if (string.IsNullOrWhiteSpace(request.Password))
                        {
                            return false;
                        }
                    }
                    break;
                default:
                    return false;

            }
            return true;
        }
    }
}