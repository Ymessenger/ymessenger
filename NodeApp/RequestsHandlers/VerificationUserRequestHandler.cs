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
using NodeApp.Extensions;
using NodeApp.Helpers;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class VerificationUserRequestHandler : IRequestHandler
    {
        private readonly VerificationUserRequest request;
        private readonly ClientConnection clientConnection;
        private readonly ILoadUsersService loadUsersService;
        private readonly IUpdateUsersService updateUsersService;
        private readonly IVerificationCodesService verificationCodesService;
        private readonly ISmsService smsService;
        public VerificationUserRequestHandler(
            Request request,
            ClientConnection clientConnection,
            ILoadUsersService loadUsersService,
            IUpdateUsersService updateUsersService,
            IVerificationCodesService verificationCodesService,
            ISmsService smsService)
        {
            this.request = (VerificationUserRequest)request;
            this.clientConnection = clientConnection;
            this.loadUsersService = loadUsersService;
            this.updateUsersService = updateUsersService;
            this.verificationCodesService = verificationCodesService;
            this.smsService = smsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                object errorObject = null;
                if (request.IsRegistration)
                {
                    
                    if (ValidationHelper.IsEmailValid(request.Uid))
                    {
                        if (await loadUsersService.IsEmailExistsAsync(request.Uid).ConfigureAwait(false))
                        {
                            errorObject = new
                            {
                                Email = $"Email {request.Uid} already exists."
                            };
                        }
                    }
                    else if (ValidationHelper.IsPhoneNumberValid(request.Uid))
                    {
                        if (await loadUsersService.IsPhoneExistsAsync(request.Uid).ConfigureAwait(false))
                        {
                            errorObject = new
                            {
                                Phone = $"Phone number {request.Uid} already exists."
                            };
                        }
                    }                    
                }
                else
                {                   
                    if (ValidationHelper.IsEmailValid(request.Uid))
                    {                                             
                        if (!await loadUsersService.IsEmailExistsAsync(request.Uid).ConfigureAwait(false))
                        {
                            errorObject = new
                            {
                                Email = $"Email {request.Uid} does not exists."
                            };
                        }

                    }
                    else if (ValidationHelper.IsPhoneNumberValid(request.Uid))
                    {
                        if(!await loadUsersService.IsPhoneExistsAsync(request.Uid).ConfigureAwait(false))
                        {
                            errorObject = new 
                            {
                                Phone = $"Phone number {request.Uid} does not exists."
                            };
                        }
                    }
                }
                if (errorObject != null)
                {
                    return new ResultResponse(request.RequestId, errorObject.ToJson(), ErrorCode.WrongArgumentError);
                }
                short vCode = 0;
                switch (request.VerificationType)
                {
                    case VerificationType.Phone:
                        {
                            if (!NodeSettings.Configs.SmsServiceConfiguration.IsValid())
                            {
                                var error = new
                                {
                                    VerificationType = "Server does not support the specified type of verification"
                                };
                                return new ResultResponse(request.RequestId,error.ToJson(), ErrorCode.PermissionDenied);
                            }
                            if (await verificationCodesService.CanRequestSmsCodeAsync(DateTime.UtcNow.ToUnixTime(), request.Uid, clientConnection.UserId).ConfigureAwait(false))
                            {
                                vCode = await updateUsersService.CreateVCodeAsync(new UserPhoneVm() { FullNumber = request.Uid }, request.RequestType, clientConnection.UserId).ConfigureAwait(false);
                                bool messageResult = await smsService.SendAsync(request.Uid, $"Verification code: {vCode.ToString()}").ConfigureAwait(false);
                                if (!messageResult)
                                {
                                    throw new SendSmsException("Failed to send SMS.");
                                }
                            }
                            else
                            {
                                throw new SendingSmsIntervalNotExpired("The new SMS sending interval has not expired.");
                            }
                        }
                        break;
                    case VerificationType.Email:
                        {
                            if (NodeSettings.Configs.SmtpClient == null
                                || string.IsNullOrWhiteSpace(NodeSettings.Configs.SmtpClient.Email)
                                || string.IsNullOrWhiteSpace(NodeSettings.Configs.SmtpClient.Port)
                                || string.IsNullOrWhiteSpace(NodeSettings.Configs.SmtpClient.Host)
                                || string.IsNullOrWhiteSpace(NodeSettings.Configs.SmtpClient.Password))
                            {
                                var error = new
                                {
                                    VerificationType = "Server does not support the specified type of verification"
                                };
                                return new ResultResponse(request.RequestId, error.ToJson(), ErrorCode.PermissionDenied);
                            }
                            vCode = await updateUsersService.CreateVCodeAsync(request.Uid, clientConnection.UserId).ConfigureAwait(false);
                            EmailHandler.SendEmailAsync(request.Uid, vCode.ToString());

                        }
                        break;                        
                    default:
                        {
                            throw new UnknownVerificationTypeException($"Unknown verification type:{request.VerificationType}.");
                        }
                }
                return new ResultResponse(request.RequestId);
            }
            catch (CreateVerificationCodeException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Phone, Email or UserID does not found.", ErrorCode.CreateVerificationCodeProblem);
            }
            catch (UserNotFoundException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "User not found.", ErrorCode.UserNotFound);
            }
            catch (SendSmsException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Failed to send SMS.", ErrorCode.SendVerificationCodeProblem);
            }
            catch (SendingSmsIntervalNotExpired ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "The new SMS sending interval has not expired.", ErrorCode.SendVerificationCodeProblem);
            }
            catch (UnknownVerificationTypeException ex)
            {
                Logger.WriteLog(ex, request);
                var errorObject = new
                {
                    VerificationType = "Wrong verification type"
                };
                return new ResultResponse(request.RequestId, errorObject.ToJson(), ErrorCode.WrongArgumentError);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Internal server error.", ErrorCode.UnknownError);
            }
        }
        public bool IsRequestValid()
        {
            switch (request.VerificationType)
            {
                case VerificationType.Email:
                    {
                        return ValidationHelper.IsEmailValid(request.Uid);
                    }
                case VerificationType.Phone:
                    {
                        return ValidationHelper.IsPhoneNumberValid(request.Uid);
                    }
                case VerificationType.UserId:
                    {
                        bool IsNumber = long.TryParse(request.Uid, out long value);
                        return IsNumber && value > 0;
                    }
                default:
                    return false;
            }
        }       
    }
}