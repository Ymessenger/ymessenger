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
using NodeApp.Helpers;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class NewUserRequestHandler : IRequestHandler
    {
        private readonly NewUserRequest request;
        private readonly ClientConnection clientConn;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly ICreateUsersService createUsersService;
        private readonly ITokensService tokensService;
        private readonly IVerificationCodesService verificationCodesService;
        private readonly ILoadUsersService loadUsersService;

        public NewUserRequestHandler(
            Request request,
            ClientConnection clientConn,
            INodeNoticeService nodeNoticeService,
            ICreateUsersService createUsersService,
            ITokensService tokensService,
            IVerificationCodesService verificationCodesService,
            ILoadUsersService loadUsersService)
        {
            this.request = (NewUserRequest)request;
            this.clientConn = clientConn;
            this.nodeNoticeService = nodeNoticeService;
            this.createUsersService = createUsersService;
            this.tokensService = tokensService;
            this.verificationCodesService = verificationCodesService;
            this.loadUsersService = loadUsersService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            if (!NodeSettings.Configs.Node.UserRegistrationAllowed)
            {
                return new ResultResponse(request.RequestId, "User registration is not allowed.", ErrorCode.PermissionDenied);
            }
            UserVm user = request.User;
            try
            {
                VerificationCodeInfo verificationCode = null;
                if (!request.User.Phones.IsNullOrEmpty() && request.User.Emails.IsNullOrEmpty())
                {
                    if(NodeSettings.Configs.Node.RegistrationMethod == RegistrationMethod.EmailRequired)
                    {
                        var errorObject = new
                        {
                            Email = "Email required"
                        };
                        return new ResultResponse(request.RequestId, ObjectSerializer.ObjectToJson(errorObject), ErrorCode.WrongArgumentError);
                    }
                    if (await loadUsersService.IsPhoneExistsAsync(request.User.Phones.FirstOrDefault().FullNumber).ConfigureAwait(false))
                    {
                        var errorObject = new
                        {
                            Phone = "Phone already exists"
                        };
                        return new ResultResponse(request.RequestId, ObjectSerializer.ObjectToJson(errorObject), ErrorCode.WrongArgumentError);
                    }
                    verificationCode = await verificationCodesService.GetUserVerificationCodeAsync(request.User.Phones.FirstOrDefault().FullNumber).ConfigureAwait(false);
                }
                else if (request.User.Phones.IsNullOrEmpty() && !request.User.Emails.IsNullOrEmpty())
                {
                    if (NodeSettings.Configs.Node.RegistrationMethod == RegistrationMethod.PhoneRequired)
                    {
                        var errorObject = new
                        {
                            Email = "Phone required"
                        };
                        return new ResultResponse(request.RequestId, ObjectSerializer.ObjectToJson(errorObject), ErrorCode.WrongArgumentError);
                    }
                    if (await loadUsersService.IsEmailExistsAsync(request.User.Emails.FirstOrDefault()).ConfigureAwait(false))
                    {
                        var errorObject = new
                        {
                            Email = "Email already exists."
                        };
                        return new ResultResponse(request.RequestId, ObjectSerializer.ObjectToJson(errorObject), ErrorCode.WrongArgumentError);
                    }
                    verificationCode = await verificationCodesService.GetUserVerificationCodeAsync(request.User.Emails.FirstOrDefault()).ConfigureAwait(false);
                }
                else
                {
                    if (NodeSettings.Configs.Node.RegistrationMethod != RegistrationMethod.NothingRequired) 
                    {
                        var errorObject = new
                        {
                            Email = "Email only or phone only",
                            Phone = "Email only or phone only"
                        };
                        return new ResultResponse(request.RequestId, ObjectSerializer.ObjectToJson(errorObject), ErrorCode.WrongArgumentError);
                    }                    
                }
                if (verificationCode != null 
                    && verificationCode.VCode != request.VCode                     
                    && (!request.User.Emails.IsNullOrEmpty() || !request.User.Phones.IsNullOrEmpty()))
                {
                    var errorObject = new
                    {
                        VCode = "Wrong verification code"
                    };
                    return new ResultResponse(request.RequestId, ObjectSerializer.ObjectToJson(errorObject), ErrorCode.WrongVerificationCode);
                }
                ValuePair<UserVm, string> userPasswordPair = await createUsersService.CreateNewUserAsync(user, NodeSettings.Configs.Node.Id, NodeSettings.Configs.ConfirmUsers).ConfigureAwait(false);
                TokenVm tempTokens = await tokensService.CreateTokenPairByUserIdAsync(userPasswordPair.FirstValue.Id.GetValueOrDefault(), false, 30 * 60).ConfigureAwait(false);
                clientConn.FileAccessToken = RandomExtensions.NextString(64);
                BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateNewUserSegmentAsync(
                    userPasswordPair.FirstValue,
                    NodeSettings.Configs.Node.Id,
                    NodeData.Instance.NodeKeys.SignPrivateKey,
                    NodeData.Instance.NodeKeys.SymmetricKey,
                    NodeData.Instance.NodeKeys.Password,
                    NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
                BlockGenerationHelper.Instance.AddSegment(segment);
                ShortUser shortUser = new ShortUser
                {
                    UserId = userPasswordPair.FirstValue.Id.GetValueOrDefault(),
                    PrivateData = segment.PrivateData
                };
                nodeNoticeService.SendNewUsersNodeNoticeAsync(shortUser, segment);
                return new TokensResponse(
                    request.RequestId,
                    userPasswordPair.FirstValue,
                    tempTokens.AccessToken,
                    tempTokens.RefreshToken,
                    clientConn.FileAccessToken,
                    userPasswordPair.SecondValue);
            }
            catch (CreateNewUserException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, ex.Message, ErrorCode.UnknownError);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConn.UserId != null)
            {
                return false;
            }

            if (request.User == null)
            {
                return false;
            }

            if (request.User.Emails != null && request.User.Emails.Any())
            {
                if (request.User.Emails.Count > 1)
                {
                    return false;
                }

                foreach (string email in request.User.Emails)
                {
                    if (!ValidationHelper.IsEmailValid(email))
                    {
                        return false;
                    }
                }
            }
            if (request.User.Phones != null && request.User.Phones.Any())
            {
                if (request.User.Phones.Count > 1 || request.User.Phones.Count(opt => opt.IsMain == true) > 1)
                {
                    return false;
                }

                foreach (var phone in request.User.Phones)
                {
                    if (!ValidationHelper.IsPhoneNumberValid(phone))
                    {
                        return false;
                    }
                }
            }
            return !string.IsNullOrEmpty(request.User.NameFirst);
        }
    }
}