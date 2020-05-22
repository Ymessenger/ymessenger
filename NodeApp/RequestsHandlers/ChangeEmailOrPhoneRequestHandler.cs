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
using NodeApp.Helpers;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using ObjectsLibrary.Converters;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Threading.Tasks;

namespace NodeApp
{
    public class ChangeEmailOrPhoneRequestHandler : IRequestHandler
    {
        private readonly ChangeEmailOrPhoneRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IUpdateUsersService updateUsersService;
        private readonly IVerificationCodesService verificationCodesService;
        private readonly ILoadUsersService loadUsersService;
        private readonly bool isPhoneEditing;
        private readonly bool isEmailEditing;

        public ChangeEmailOrPhoneRequestHandler(
            Request request,
            ClientConnection clientConnection,
            IUpdateUsersService updateUsersService,
            IVerificationCodesService verificationCodesService,
            ILoadUsersService loadUsersService)
        {
            this.request = (ChangeEmailOrPhoneRequest)request;
            this.clientConnection = clientConnection;
            this.updateUsersService = updateUsersService;
            this.verificationCodesService = verificationCodesService;
            isPhoneEditing = ValidationHelper.IsPhoneNumberValid(this.request.Value);
            isEmailEditing = ValidationHelper.IsEmailValid(this.request.Value);
            this.loadUsersService = loadUsersService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            UserVm resultUser;
            VerificationCodeInfo verificationCode = await verificationCodesService
                    .GetUserVerificationCodeAsync(request.Value, clientConnection.UserId).ConfigureAwait(false);
            if (isPhoneEditing)
            {
                if (await loadUsersService.IsPhoneExistsAsync(request.Value).ConfigureAwait(false))
                {
                    var errorObject = new
                    {
                        Phone = $"Phone number {request.Value} already exists"
                    };
                    return new ResultResponse(request.RequestId, ObjectSerializer.ObjectToJson(errorObject), ObjectsLibrary.Enums.ErrorCode.WrongArgumentError);
                }
                if (verificationCode != null && verificationCode.VCode == request.VCode && request.Value == verificationCode.Id)
                {
                    resultUser = await updateUsersService.UpdateUserPhoneAsync(clientConnection.UserId.Value, request.Value).ConfigureAwait(false);
                }
                else
                {
                    return new ResultResponse(request.RequestId, "Wrong verification code.", ObjectsLibrary.Enums.ErrorCode.WrongVerificationCode);
                }
            }
            else
            {
                if (await loadUsersService.IsEmailExistsAsync(request.Value).ConfigureAwait(false))
                {
                    var errorObject = new
                    {
                        Email = $"Email {request.Value} already exists"
                    };
                    return new ResultResponse(request.RequestId, ObjectSerializer.ObjectToJson(errorObject), ObjectsLibrary.Enums.ErrorCode.WrongArgumentError);
                }
                if (verificationCode != null && verificationCode.VCode == request.VCode && request.Value == verificationCode.Id)
                {
                    resultUser = await updateUsersService.UpdateUserEmailAsync(clientConnection.UserId.Value, request.Value).ConfigureAwait(false);
                }
                else
                {
                    return new ResultResponse(request.RequestId, "Wrong verification code.", ObjectsLibrary.Enums.ErrorCode.WrongVerificationCode);
                }
            }
            return new UsersResponse(request.RequestId, resultUser);

        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
            {
                throw new UnauthorizedUserException();
            }

            if (string.IsNullOrWhiteSpace(request.Value) || !request.VCode.HasValue)
            {
                return false;
            }

            return isPhoneEditing || isEmailEditing;
        }
    }
}