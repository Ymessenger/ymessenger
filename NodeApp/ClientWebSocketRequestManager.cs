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
using Newtonsoft.Json;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Objects;
using NodeApp.RequestsHandlers;
using ObjectsLibrary;
using ObjectsLibrary.ClientResponses;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp
{
    public class ClientWebSocketRequestManager
    {
        private readonly ClientConnection clientConnection;
        private readonly ClientRequestService clientRequestService;
        private readonly IAppServiceProvider appServiceProvider;
        private long prevRequestTime = 0;
        private readonly static HashSet<RequestType> allowedRequestTypes = new HashSet<RequestType>
        {
            RequestType.Login,
            RequestType.Logout,
            RequestType.ChangeNode,
            RequestType.SetConnectionEncrypted,
            RequestType.GetInformationNode
        };

        public ClientWebSocketRequestManager(ClientConnection clientConnection, IAppServiceProvider serviceProvider)
        {
            this.clientConnection = clientConnection;
            appServiceProvider = serviceProvider;
            clientRequestService = new ClientRequestService(appServiceProvider.NoticeService);
        }

        public async Task HandleRequestAsync(Request request, int requestSize)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                Response response = default;
                try
                {
                    if (!clientConnection.Banned.GetValueOrDefault(false) || allowedRequestTypes.Contains(request.RequestType))
                    {
                        IRequestHandler requestHandler = InitRequestHandler(
                            request,
                            clientConnection,
                            clientRequestService,
                            appServiceProvider);
                        Logger.WriteLog(request, clientConnection);
                        if (!requestHandler.IsRequestValid())
                        {
                            throw new InvalidRequestDataException();
                        }
                        response = await requestHandler.CreateResponseAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        response = new ResultResponse(request.RequestId, "User is banned.", ErrorCode.PermissionDenied);
                    }
                }
                catch (InvalidRequestDataException ex)
                {
                    Logger.WriteLog(ex, request);
                    response = new ResultResponse(request.RequestId, "Invalid request data.", ErrorCode.InvalidRequestData);
                }
                catch (UnknownRequestTypeException)
                {
                    response = new ResultResponse(request.RequestId, "Invalid request type.", ErrorCode.InvalidRequestData);
                }
                catch (UnauthorizedUserException)
                {
                    response = new ResultResponse(request.RequestId, "User is not authorized.", ErrorCode.UserNotAuthorized);
                }
                catch (PermissionDeniedException ex)
                {
                    response = new ResultResponse(request.RequestId, ex.Message, ErrorCode.PermissionDenied);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex, request);
                    response = new ResultResponse(request?.RequestId ?? -1, "Internal server error.", ErrorCode.UnknownError);
                }
                finally
                {
                    long currentTime = DateTime.UtcNow.ToUnixTime();
                    await SendResponseAsync(response).ConfigureAwait(false);
                    stopwatch.Stop();
                    Logger.WriteRequestMetrics(request.RequestType, stopwatch.ElapsedMilliseconds, requestSize, request.RequestId);
                    if (currentTime - prevRequestTime >= 5)
                    {
                        await appServiceProvider.UpdateUsersService.UpdateUserActivityTimeAsync(clientConnection).ConfigureAwait(false);
                    }
                    prevRequestTime = currentTime;
                    Logger.WriteLog(response, clientConnection);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public static IRequestHandler InitRequestHandler(
            Request request,
            ClientConnection clientConnection,
            ClientRequestService clientRequestService,
            IAppServiceProvider appServiceProvider)
        {
            IRequestHandler requestHandler;
            switch (request.RequestType)
            {
                case RequestType.VerificationUser:
                    {
                        requestHandler = new VerificationUserRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.LoadUsersService,
                            appServiceProvider.UpdateUsersService,
                            appServiceProvider.VerificationCodesService,
                            appServiceProvider.SmsService);
                    }
                    break;
                case RequestType.NewUser:
                    {
                        requestHandler = new NewUserRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.CreateUsersService,
                            appServiceProvider.TokensService,
                            appServiceProvider.VerificationCodesService,
                            appServiceProvider.LoadUsersService);
                    }
                    break;
                case RequestType.Login:
                    {
                        requestHandler = new LoginRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.ConnectionsService,
                            appServiceProvider.NoticeService,
                            appServiceProvider.TokensService,
                            appServiceProvider.LoadUsersService,
                            appServiceProvider.PendingMessagesService,
                            appServiceProvider.NodeRequestSender);
                    }
                    break;
                case RequestType.GetMessages:
                    {
                        requestHandler = new GetMessagesRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.ConnectionsService,
                            appServiceProvider.LoadMessagesService,
                            appServiceProvider.CreateMessagesService,
                            appServiceProvider.AttachmentsService,
                            appServiceProvider.LoadChatsService,
                            appServiceProvider.ConversationsService,
                            appServiceProvider.NodeRequestSender);
                    }
                    break;
                case RequestType.Logout:
                    {
                        requestHandler = new LogoutRequestHandler(request, clientConnection, appServiceProvider.ConnectionsService, appServiceProvider.TokensService);
                    }
                    break;
                case RequestType.SendMessages:
                    {
                        requestHandler = new SendMessagesRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.ConversationsNoticeService,
                            appServiceProvider.AttachmentsService,
                            appServiceProvider.CreateMessagesService,
                            appServiceProvider.LoadDialogsService);
                    }
                    break;
                case RequestType.NewChats:
                    {
                        requestHandler = new NewChatRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.ConversationsNoticeService,
                            appServiceProvider.CreateChatsService);
                    }
                    break;
                case RequestType.EditUser:
                    {
                        requestHandler = new EditUserRequestHandler(request, clientConnection, appServiceProvider.NodeNoticeService, appServiceProvider.UpdateUsersService);
                    }
                    break;
                case RequestType.DeleteConversation:
                    {
                        requestHandler = new DeleteConversationsRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.LoadChatsService,
                            appServiceProvider.DeleteChatsService,
                            appServiceProvider.LoadDialogsService,
                            appServiceProvider.DeleteDialogsService,
                            appServiceProvider.LoadChannelsService,
                            appServiceProvider.DeleteChannelsService);
                    }
                    break;
                case RequestType.RefreshTokens:
                    {
                        requestHandler = new RefreshTokensRequestHandler(request, clientConnection, appServiceProvider.TokensService);
                    }
                    break;
                case RequestType.DownloadFile:
                    {
                        throw new WebSocketNotSupportException();
                    }
                case RequestType.GetUserFilesInformation:
                    {
                        requestHandler = new GetUserFilesRequestHandler(request, clientConnection, appServiceProvider.FilesService);
                    }
                    break;
                case RequestType.GetInformationNode:
                    {
                        requestHandler = new GetInformationNodeRequestHandler(request, appServiceProvider.NodesService);
                    }
                    break;
                case RequestType.EditChats:
                    {
                        requestHandler = new EditChatsRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.ConversationsNoticeService,
                            appServiceProvider.UpdateChatsService,
                            appServiceProvider.LoadChatsService,
                            appServiceProvider.SystemMessagesService);
                    }
                    break;
                case RequestType.UploadFile:
                    {
                        throw new WebSocketNotSupportException();
                    }
                case RequestType.AddUsersChats:
                    {
                        requestHandler = new AddUserChatsRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.ConversationsNoticeService,
                            appServiceProvider.UpdateChatsService,
                            appServiceProvider.LoadChatsService,
                            appServiceProvider.NodeRequestSender,
                            appServiceProvider.ConnectionsService,
                            appServiceProvider.SystemMessagesService);
                    }
                    break;
                case RequestType.MessagesRead:
                    {
                        requestHandler = new MessagesReadRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.ConversationsNoticeService,
                            appServiceProvider.UpdateMessagesService,
                            appServiceProvider.LoadDialogsService);
                    }
                    break;
                case RequestType.GetUsersInformationNode:
                    {
                        requestHandler = new GetUsersInfoNodeRequestHandler(request, appServiceProvider.LoadUsersService, appServiceProvider.PrivacyService);
                    }
                    break;
                case RequestType.GetChatsInformationNode:
                    {
                        requestHandler = new GetChatsInfoNodeRequestHandler(request, appServiceProvider.LoadChatsService);
                    }
                    break;
                case RequestType.GetInformationWeb:
                    {
                        requestHandler = new GetInformationWebRequestHandler(request, appServiceProvider.NodesService);
                    }
                    break;
                case RequestType.DeleteUser:
                    {
                        requestHandler = new DeleteUserRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.DeleteUsersService,
                            appServiceProvider.VerificationCodesService,
                            appServiceProvider.ConnectionsService);
                    }
                    break;
                case RequestType.BlockUsers:
                    {
                        requestHandler = new BlockUsersRequestHandler(request, clientConnection, appServiceProvider.NodeNoticeService, appServiceProvider.UpdateUsersService);
                    }
                    break;
                case RequestType.GetSelf:
                    {
                        requestHandler = new GetSelfRequestHandler(request, clientConnection, appServiceProvider.LoadUsersService);
                    }
                    break;
                case RequestType.GetChatUsers:
                    {
                        requestHandler = new GetChatUsersRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.LoadChatsService,
                            appServiceProvider.PrivacyService,
                            appServiceProvider.ConnectionsService,
                            appServiceProvider.NodeRequestSender,
                            appServiceProvider.CrossNodeService);
                    }
                    break;
                case RequestType.EditChatUsers:
                    {
                        requestHandler = new EditChatUsersRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.ConversationsNoticeService,
                            appServiceProvider.LoadChatsService,
                            appServiceProvider.UpdateChatsService,
                            appServiceProvider.SystemMessagesService);
                    }
                    break;
                case RequestType.UnblockUsers:
                    {
                        requestHandler = new UnblockUsersRequestHandler(request, clientConnection, appServiceProvider.NodeNoticeService, appServiceProvider.UpdateUsersService);
                    }
                    break;
                case RequestType.GetFilesInformation:
                    {
                        requestHandler = new GetFilesInfoRequestHandler(request, clientConnection, appServiceProvider.FilesService);
                    }
                    break;
                case RequestType.GetAllUserConversationsNode:
                    {
                        requestHandler = new GetAllUserConversationsNodeRequestHandler(request, clientConnection, appServiceProvider.ConversationsService);
                    }
                    break;
                case RequestType.DeleteFiles:
                    {
                        requestHandler = new DeleteFilesRequestHandler(request, clientConnection, appServiceProvider.NodeNoticeService, appServiceProvider.FilesService);
                    }
                    break;
                case RequestType.GetUsers:
                    {
                        requestHandler = new GetUsersRequestHandler(request, clientConnection, appServiceProvider.ConnectionsService, appServiceProvider.LoadUsersService, appServiceProvider.PrivacyService, appServiceProvider.NodeRequestSender);
                    }
                    break;
                case RequestType.DeleteMessages:
                    {
                        requestHandler = new DeleteMessagesRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.ConversationsNoticeService,
                            appServiceProvider.DeleteMessagesService,
                            appServiceProvider.LoadChatsService,
                            appServiceProvider.PendingMessagesService,
                            appServiceProvider.LoadChannelsService,
                            appServiceProvider.LoadDialogsService);
                    }
                    break;
                case RequestType.GetMessagesUpdates:
                    {
                        requestHandler = new GetMessagesUpdatesRequestHandler(request, clientConnection, appServiceProvider.LoadMessagesService);
                    }
                    break;
                case RequestType.GetChats:
                    {
                        requestHandler = new GetChatsRequestHandler(request, appServiceProvider.LoadChatsService, clientConnection);
                    }
                    break;
                case RequestType.GetDialogsInformation:
                    {
                        requestHandler = new GetDialogsInformationRequestHandler(request, clientConnection, appServiceProvider.LoadDialogsService);
                    }
                    break;
                case RequestType.Search:
                    {
                        requestHandler = new SearchRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.ConnectionsService,
                            appServiceProvider.LoadChatsService,
                            appServiceProvider.NodesService,
                            appServiceProvider.LoadUsersService,
                            appServiceProvider.LoadChannelsService,
                            appServiceProvider.PrivacyService,
                            appServiceProvider.NodeRequestSender);
                    }
                    break;
                case RequestType.SetNewKeys:
                    {
                        requestHandler = new SetNewKeysRequestHandler(request, clientConnection, appServiceProvider.NodeNoticeService, appServiceProvider.KeysService);
                    }
                    break;
                case RequestType.DeleteKeys:
                    {
                        requestHandler = new DeleteKeysRequestHandler(request, clientConnection, appServiceProvider.NodeNoticeService, appServiceProvider.KeysService);
                    }
                    break;
                case RequestType.GetRandomSequence:
                    {
                        requestHandler = new GetRandomSequenceRequestHandler(request, clientConnection);
                    }
                    break;
                case RequestType.SetNewKeyForChat:
                    {
                        requestHandler = new SetNewKeysRequestHandler(request, clientConnection, appServiceProvider.NodeNoticeService, appServiceProvider.KeysService);
                    }
                    break;
                case RequestType.GetUserPublicKeys:
                    {
                        requestHandler = new GetUserPublicKeysRequestHandler(request, clientConnection, appServiceProvider.KeysService);
                    }
                    break;
                case RequestType.CreateChannel:
                    {
                        requestHandler = new CreateChannelRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.ConversationsNoticeService,
                            appServiceProvider.LoadChannelsService,
                            appServiceProvider.CreateChannelsService);
                    }
                    break;
                case RequestType.AddUsersToChannels:
                    {
                        requestHandler = new AddUsersToChannelsRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.ConversationsNoticeService,
                            appServiceProvider.UpdateChannelsService,
                            appServiceProvider.LoadChannelsService,
                            appServiceProvider.ConnectionsService,
                            appServiceProvider.NodeRequestSender);
                    }
                    break;
                case RequestType.EditChannel:
                    {
                        requestHandler = new EditChannelRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.ConversationsNoticeService,
                            appServiceProvider.LoadChannelsService,
                            appServiceProvider.UpdateChannelsService,
                            appServiceProvider.SystemMessagesService);
                    }
                    break;
                case RequestType.EditChannelUsers:
                    {
                        requestHandler = new EditChannelUsersRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.LoadChannelsService,
                            appServiceProvider.UpdateChannelsService);
                    }
                    break;
                case RequestType.GetChannelUsers:
                    {
                        requestHandler = new GetChannelUsersRequestHandler(request, clientConnection, appServiceProvider.LoadChannelsService);
                    }
                    break;
                case RequestType.GetChannels:
                    {
                        requestHandler = new GetChannelsRequestHandler(request, clientConnection, appServiceProvider.LoadChannelsService);
                    }
                    break;
                case RequestType.SetConnectionEncrypted:
                    {
                        requestHandler = new SetConnectionEncryptedRequestHandler(request, clientConnection,
                                                                                  appServiceProvider.ConnectionsService,
                                                                                  appServiceProvider.KeysService,
                                                                                  appServiceProvider.NodeRequestSender);
                    }
                    break;
                case RequestType.ChangeNode:
                    {
                        requestHandler = new ChangeNodeRequestHandler(request, clientConnection, appServiceProvider.ChangeNodeOperationsService);
                    }
                    break;
                case RequestType.Polling:
                    {
                        requestHandler = new PollingRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.LoadChatsService,
                            appServiceProvider.PollsService,
                            appServiceProvider.LoadChannelsService);
                    }
                    break;
                case RequestType.GetPollVotedUsers:
                    {
                        requestHandler = new GetPollVotedUsersRequestHandler(request, clientConnection,
                                                                             appServiceProvider.ConnectionsService,
                                                                             appServiceProvider.PollsService,
                                                                             appServiceProvider.NodeRequestSender);
                    }
                    break;
                case RequestType.EditMessage:
                    {
                        requestHandler = new EditMessageRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NodeNoticeService,
                            appServiceProvider.ConversationsNoticeService,
                            appServiceProvider.UpdateMessagesService,
                            appServiceProvider.AttachmentsService);
                    }
                    break;
                case RequestType.CreateOrEditContact:
                    {
                        requestHandler = new CreateOrEditContactRequestHandler(request, clientConnection, appServiceProvider.ContactsService);
                    }
                    break;
                case RequestType.DeleteContacts:
                    {
                        requestHandler = new DeleteContactsRequestHandler(request, clientConnection, appServiceProvider.ContactsService);
                    }
                    break;
                case RequestType.GetUserContacts:
                    {
                        requestHandler = new GetUserContactsRequestHandler(request, clientConnection,
                                                                           appServiceProvider.ConnectionsService,
                                                                           appServiceProvider.ContactsService,
                                                                           appServiceProvider.NodeRequestSender);
                    }
                    break;
                case RequestType.CreateOrEditGroup:
                    {
                        requestHandler = new CreateOrEditGroupRequestHandler(request, clientConnection, appServiceProvider.GroupsService);
                    }
                    break;
                case RequestType.DeleteGroup:
                    {
                        requestHandler = new DeleteGroupRequestHandler(request, clientConnection, appServiceProvider.GroupsService);
                    }
                    break;
                case RequestType.AddUsersToGroup:
                    {
                        requestHandler = new AddUsersToGroupRequestHandler(request, clientConnection, appServiceProvider.GroupsService);
                    }
                    break;
                case RequestType.RemoveUsersFromGroup:
                    {
                        requestHandler = new RemoveUsersFromGroupRequestHandler(request, clientConnection, appServiceProvider.GroupsService);
                    }
                    break;
                case RequestType.GetGroupContacts:
                    {
                        requestHandler = new GetGroupContactsRequestHandler(request, clientConnection,
                                                                            appServiceProvider.ConnectionsService,
                                                                            appServiceProvider.GroupsService,
                                                                            appServiceProvider.NodeRequestSender);
                    }
                    break;
                case RequestType.GetUserGroups:
                    {
                        requestHandler = new GetUserGroupsRequestHandler(request, clientConnection, appServiceProvider.GroupsService);
                    }
                    break;
                case RequestType.GetMessageEditHistory:
                    {
                        requestHandler = new GetMessageEditHistoryRequestHandler(request, clientConnection, appServiceProvider.LoadMessagesService);
                    }
                    break;
                case RequestType.GetFavorites:
                    {
                        requestHandler = new GetFavoritesRequestHandler(request, clientConnection, appServiceProvider.FavoritesService);
                    }
                    break;
                case RequestType.AddFavorites:
                    {
                        requestHandler = new AddFavoritesRequestHandler(request, clientConnection, appServiceProvider.FavoritesService);
                    }
                    break;
                case RequestType.EditFavorites:
                    {
                        requestHandler = new EditFavoritesRequestHandler(request, clientConnection, appServiceProvider.FavoritesService);
                    }
                    break;
                case RequestType.VerifyNode:
                    {
                        requestHandler = new VerifyNodeRequestHandler(request);
                    }
                    break;
                case RequestType.GetDevicesPrivateKeys:
                    {
                        requestHandler = new GetDevicesPrivateKeysRequestHandler(request, clientConnection, clientRequestService, appServiceProvider.ConnectionsService);
                    }
                    break;
                case RequestType.GetSessions:
                    {
                        requestHandler = new GetSessionsRequestHandler(request, clientConnection, appServiceProvider.LoadUsersService);
                    }
                    break;
                case RequestType.SearchMessages:
                    {
                        requestHandler = new SearchMessagesRequestHandler(request, clientConnection, appServiceProvider.LoadMessagesService);
                    }
                    break;
                case RequestType.MuteConversation:
                    {
                        requestHandler = new MuteConversationRequestHandler(request, clientConnection, appServiceProvider.ConversationsService);
                    }
                    break;
                case RequestType.GetQRCode:
                    {
                        requestHandler = new GetQRCodeRequestHandler(request, clientConnection, appServiceProvider.QRCodesService);
                    }
                    break;
                case RequestType.CheckQRCode:
                    {
                        requestHandler = new CheckQRCodeRequestHandler(
                            request,
                            clientConnection,
                            appServiceProvider.NoticeService,
                            appServiceProvider.QRCodesService,
                            appServiceProvider.LoadUsersService);
                    }
                    break;
                case RequestType.BatchPhonesSearch:
                    {
                        requestHandler = new BatchPhonesSearchRequestHandler(request, clientConnection,
                                                                             appServiceProvider.LoadUsersService,
                                                                             appServiceProvider.ConnectionsService,
                                                                             appServiceProvider.PrivacyService,
                                                                             appServiceProvider.NodeRequestSender);
                    }
                    break;
                case RequestType.ChangeEmailOrPhone:
                    {
                        requestHandler = new ChangeEmailOrPhoneRequestHandler(request, clientConnection,
                                                                              appServiceProvider.UpdateUsersService,
                                                                              appServiceProvider.VerificationCodesService,
                                                                              appServiceProvider.LoadUsersService);
                    }
                    break;
                case RequestType.DeleteAllMessages:
                    {
                        requestHandler = new DeleteAllMessagesRequestHandler(request, clientConnection, appServiceProvider.DeleteMessagesService, appServiceProvider.NodeNoticeService);
                    }
                    break;
                case RequestType.ConversationAction:
                    {
                        requestHandler = new ConversationActionRequestHandler(request,
                                                                              clientConnection,
                                                                              appServiceProvider.ConversationsService,
                                                                              appServiceProvider.ConversationsNoticeService,
                                                                              appServiceProvider.NodeNoticeService,
                                                                              appServiceProvider.LoadDialogsService,
                                                                              appServiceProvider.SystemMessagesService);
                    }
                    break;
                default:
                    throw new UnknownRequestTypeException();
            }
            return requestHandler;
        }

        public void HandleClientResponse(ClientResponse clientResponse)
        {
            ClientRequestService.AddResponse(clientConnection, clientResponse);
        }

        private async Task SendResponseAsync(Response response)
        {
            if (response == null)
            {
                return;
            }

            try
            {
                byte[] data;
                byte[] responseBytes = ObjectSerializer.CommunicationObjectToBytes(response, NullValueHandling.Include);
                if (clientConnection.IsEncryptedConnection)
                {
                    data = Encryptor.SymmetricDataEncrypt(
                        responseBytes,
                        NodeData.Instance.NodeKeys.SignPrivateKey,
                        clientConnection.SymmetricKey,
                        MessageDataType.Response,
                        NodeData.Instance.NodeKeys.Password);
                }
                else
                {
                    data = responseBytes;
                }
                await clientConnection.ClientSocket.SendAsync(
                  data,
                  WebSocketMessageType.Binary,
                  true,
                  CancellationToken.None).ConfigureAwait(false);
                if (response.ResponseType == ResponseType.EncryptedKey)
                {
                    clientConnection.SentKey = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
    }
}