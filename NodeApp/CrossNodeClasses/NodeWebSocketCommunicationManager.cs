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
using NodeApp.CrossNodeClasses.Enums;
using NodeApp.CrossNodeClasses.NodeNoticeHandlers;
using NodeApp.CrossNodeClasses.NodeRequestHandlers;
using NodeApp.CrossNodeClasses.NodeResponseHandlers;
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.CrossNodeClasses.Requests;
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.Interfaces;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses
{
    public class NodeWebSocketCommunicationManager
    {        
        private NodeConnection nodeConnection;         
        private readonly IAppServiceProvider appServiceProvider;
        
        public NodeWebSocketCommunicationManager(IAppServiceProvider appServiceProvider)
        {
            this.appServiceProvider = appServiceProvider;
        }
        public void Handle(byte[] data, NodeConnection current)
        {
            try
            {
                CommunicationObject @object;                
                if (current.IsEncryptedConnection)
                {
                    byte[] decryptedData = Encryptor.SymmetricDataDecrypt(
                        data, 
                        current.SignPublicKey, 
                        current.SymmetricKey, 
                        NodeData.Instance.NodeKeys.Password)
                        .DecryptedData;
                    @object = ObjectSerializer.ByteArrayToObject<CommunicationObject>(decryptedData);
                }
                else
                {
                    @object = ObjectSerializer.ByteArrayToObject<CommunicationObject>(data);
                }
                nodeConnection = current;
                switch (@object.Type)
                {
                    case ObjectType.NodeNotice:
                        {
                            var notice = (NodeNotice)@object;                            
                            HandleNotice(notice);
                        }
                        break;
                    case ObjectType.NodeRequest:
                        {
                            var request = (NodeRequest)@object;                            
                            HandleRequest(request, current);
                        }
                        break;
                    case ObjectType.NodeResponse:
                        {
                            var response = (NodeResponse)@object;                            
                            HandleResponse(response);
                        }
                        break;
                }
                Logger.WriteLog(@object);
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        private async void HandleRequest(NodeRequest request, NodeConnection current)
        {
            try
            {
                ICommunicationHandler requestHandler = null;
                switch (request.RequestType)
                {
                    case NodeRequestType.CheckToken:
                        {
                            requestHandler = new CheckTokenRequestHandler(request, current, appServiceProvider.TokensService, appServiceProvider.LoadUsersService);
                        }
                        break;
                    case NodeRequestType.Connect:
                        {
                            requestHandler = new ConnectRequestHandler(
                                request,
                                current,
                                appServiceProvider.ConnectionsService,
                                appServiceProvider.NodesService,
                                appServiceProvider.NodeNoticeService);
                        }
                        break;
                    case NodeRequestType.GetInfoBlocks:
                        {
                            requestHandler = new GetInfoBlocksRequestHandler(request, current);
                        }
                        break;
                    case NodeRequestType.GetMessages:
                        {
                            requestHandler = new GetMessagesRequestHandler(request, current, appServiceProvider.LoadMessagesService, appServiceProvider.ConversationsService);
                        }
                        break;
                    case NodeRequestType.Proxy:
                        {
                            requestHandler = new ProxyUsersCommunicationsNodeRequestHandler(request, current, appServiceProvider);
                        }
                        break;
                    case NodeRequestType.GetUsers:
                    case NodeRequestType.GetChats:
                    case NodeRequestType.GetChannels:
                    case NodeRequestType.GetFiles:
                        {
                            requestHandler = new GetObjectsInfoNodeRequestHandler(request, current,
                                                                                  appServiceProvider.LoadChatsService,
                                                                                  appServiceProvider.LoadUsersService,
                                                                                  appServiceProvider.LoadChannelsService,
                                                                                  appServiceProvider.PrivacyService,
                                                                                  appServiceProvider.FilesService);
                        }
                        break;
                    case NodeRequestType.Search:
                        {
                            requestHandler = new SearchNodeRequestHandler(request,
                                                                          current,
                                                                          appServiceProvider.LoadChatsService,
                                                                          appServiceProvider.LoadUsersService,
                                                                          appServiceProvider.LoadChannelsService,
                                                                          appServiceProvider.PrivacyService);
                        }
                        break;
                    case NodeRequestType.GetFullChatInformation:
                        {
                            requestHandler = new GetFullChatInformationNodeRequestHandler(request, current, appServiceProvider.LoadChatsService);
                        }
                        break;
                    case NodeRequestType.GetChatUsersInformation:
                        {
                            requestHandler = new GetChatUsersInformationNodeRequestHandler(request, current, appServiceProvider.LoadChatsService);
                        }
                        break;
                    case NodeRequestType.GetPublicKey:
                        {
                            requestHandler = new GetPublicKeyNodeRequestHandler(request, current, appServiceProvider.KeysService);
                        }
                        break;
                    case NodeRequestType.GetPolls:
                        {
                            requestHandler = new GetPollInformationNodeRequestHandler(request, current, appServiceProvider.PollsService);
                        }
                        break;
                    case NodeRequestType.BatchPhonesSearch:
                        {
                            requestHandler = new BatchPhonesSearchNodeRequestHandler(request, current, appServiceProvider.LoadUsersService, appServiceProvider.PrivacyService);
                        }
                        break;                    
                    case NodeRequestType.GetConversationsUsers:
                        {
                            requestHandler = new GetConversationsUsersNodeRequestHandler(request, current, appServiceProvider.LoadChannelsService, appServiceProvider.LoadChatsService);
                        }
                        break;
                }
                if (requestHandler.IsObjectValid())
                {
                    await requestHandler.HandleAsync().ConfigureAwait(false);
                }
                else
                {
                    ResultNodeResponse response = new ResultNodeResponse(request.RequestId, ErrorCode.InvalidRequestData);
                    SendResponse(response, current);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private async void HandleNotice(NodeNotice notice)
        {
            try
            {
                ICommunicationHandler noticeHandler = null;
                switch (notice.NoticeCode)
                {                    
                    case NodeNoticeCode.NewBlocks:
                        {
                            noticeHandler = new NewBlocksNoticeHandler(notice, nodeConnection, appServiceProvider.KeysService);
                        }
                        break;                                 
                    case NodeNoticeCode.NewNodes:
                        {
                            noticeHandler = new NewNodesNoticeHandler(notice, nodeConnection, appServiceProvider.CrossNodeService);
                        }
                        break;                    
                    case NodeNoticeCode.EditNodes:
                        {
                            noticeHandler = new EditNodesNoticeHandler(notice, nodeConnection, appServiceProvider.CrossNodeService);
                        }
                        break;
                    case NodeNoticeCode.NewUsers:
                        {
                            noticeHandler = new NewUsersNoticeHandler(notice, nodeConnection, appServiceProvider.CrossNodeService);
                        }
                        break;
                    case NodeNoticeCode.EditUsers:
                        {
                            noticeHandler = new EditUsersNoticeHandler(notice, nodeConnection, appServiceProvider.CrossNodeService);
                        }
                        break;
                    case NodeNoticeCode.DeleteUsers:
                        {
                            noticeHandler = new DeleteUsersNoticeHandler(notice, nodeConnection, appServiceProvider.DeleteUsersService);
                        }
                        break;                    
                    case NodeNoticeCode.NewChats:
                        {
                            noticeHandler = new NewChatsNoticeHandler(notice, nodeConnection, appServiceProvider.NodeNoticeService, appServiceProvider.ConversationsNoticeService, appServiceProvider.CrossNodeService);
                        }
                        break;
                    case NodeNoticeCode.DeleteConversations:
                        {
                            noticeHandler = new DeleteConversationsNoticeHandler(
                                notice,
                                appServiceProvider.DeleteChatsService,
                                appServiceProvider.LoadChatsService,
                                appServiceProvider.LoadDialogsService,
                                appServiceProvider.DeleteDialogsService,
                                appServiceProvider.LoadChannelsService,
                                appServiceProvider.DeleteChannelsService);
                        }
                        break;
                    case NodeNoticeCode.EditChats:
                        {
                            noticeHandler = new EditChatsNoticeHandler(
                                notice,
                                nodeConnection,
                                appServiceProvider.NodeNoticeService,
                                appServiceProvider.ConversationsNoticeService,
                                appServiceProvider.LoadChatsService,
                                appServiceProvider.CrossNodeService,
                                appServiceProvider.SystemMessagesService);
                        }
                        break;
                    case NodeNoticeCode.NewFiles:
                        {
                            noticeHandler = new NewFilesNoticeHandler(notice, nodeConnection, appServiceProvider.FilesService);
                        }
                        break;
                    case NodeNoticeCode.DeleteFiles:
                        {
                            noticeHandler = new DeleteFilesNoticeHandler(notice, nodeConnection, appServiceProvider.FilesService);
                        }
                        break;                   
                    case NodeNoticeCode.NewMessagesNotice:
                        {
                            noticeHandler = new NewMessagesNoticeHandler(
                                notice,
                                nodeConnection,
                                appServiceProvider.ConversationsNoticeService,
                                appServiceProvider.AttachmentsService,
                                appServiceProvider.CreateMessagesService,
                                appServiceProvider.CreateChannelsService,
                                appServiceProvider.NodeRequestSender,
                                appServiceProvider.CrossNodeService,
                                appServiceProvider.LoadDialogsService);
                        }
                        break;
                    case NodeNoticeCode.AddUsersChat:
                        {
                            noticeHandler = new AddUsersChatNoticeHandler(
                                notice,
                                nodeConnection,
                                appServiceProvider.ConversationsNoticeService,
                                appServiceProvider.UpdateChatsService,
                                appServiceProvider.NodeRequestSender,
                                appServiceProvider.CrossNodeService,
                                appServiceProvider.SystemMessagesService);
                        }
                        break;
                    case NodeNoticeCode.ChangeUsersChat:
                        {
                            noticeHandler = new ChangeUsersChatNoticeHandler(
                                notice,
                                nodeConnection,
                                appServiceProvider.ConversationsNoticeService,
                                appServiceProvider.UpdateChatsService,
                                appServiceProvider.LoadChatsService,
                                appServiceProvider.NodeRequestSender,
                                appServiceProvider.CrossNodeService,
                                appServiceProvider.SystemMessagesService);
                        }
                        break;
                    case NodeNoticeCode.MessagesRead:
                        {
                            noticeHandler = new MessagesReadNoticeHandler(
                                notice,
                                nodeConnection,
                                appServiceProvider.ConversationsNoticeService,
                                appServiceProvider.UpdateMessagesService,
                                appServiceProvider.UpdateChatsService,
                                appServiceProvider.LoadDialogsService,
                                appServiceProvider.UpdateChannelsService);
                        }
                        break;
                    case NodeNoticeCode.BlockSegments:
                        {
                            noticeHandler = new BlockSegmentsNoticeHandler(notice, nodeConnection);
                        }
                        break;
                    case NodeNoticeCode.MessagesDeleted:
                        {
                            noticeHandler = new MessagesDeletedNoticeHandler(
                                notice,
                                appServiceProvider.ConversationsNoticeService,
                                appServiceProvider.DeleteMessagesService,
                                appServiceProvider.LoadChatsService,
                                appServiceProvider.LoadDialogsService,
                                appServiceProvider.LoadChannelsService);
                        }
                        break;
                    case NodeNoticeCode.UsersAddedToUserBlacklist:
                        {
                            noticeHandler = new UsersAddedToUserBlacklistNoticeHandler(notice, nodeConnection, appServiceProvider.UpdateUsersService);
                        }
                        break;
                    case NodeNoticeCode.UsersRemovedFromUserBlacklist:
                        {
                            noticeHandler = new UsersRemovedFromUserBlacklistNoticeHandler(notice, nodeConnection, appServiceProvider.UpdateUsersService);
                        }
                        break;
                    case NodeNoticeCode.NewUserKeys:
                        {
                            noticeHandler = new NewUserKeysNodeNoticeHandler(notice, nodeConnection, appServiceProvider.KeysService);
                        }
                        break;
                    case NodeNoticeCode.DeleteUserKeys:
                        {
                            noticeHandler = new DeleteUserKeysNodeNoticeHandler(notice, nodeConnection, appServiceProvider.KeysService);
                        }
                        break;
                    case NodeNoticeCode.Channels:
                        {
                            noticeHandler = new ChannelNodeNoticeHandler(notice, nodeConnection, appServiceProvider.ConversationsNoticeService, appServiceProvider.CreateChannelsService, appServiceProvider.LoadChannelsService,appServiceProvider.SystemMessagesService);
                        }
                        break;
                    case NodeNoticeCode.ChannelsUsers:
                        {
                            noticeHandler = new ChannelUsersNodeNoticeHandler(notice, nodeConnection, appServiceProvider.CreateChannelsService, appServiceProvider.NodeRequestSender, appServiceProvider.CrossNodeService);
                        }
                        break;
                    case NodeNoticeCode.ClientDisconnected:
                        {
                            noticeHandler = new ClientDisconnectedNoticeHandler(notice, appServiceProvider.ConnectionsService);
                        }
                        break;
                    case NodeNoticeCode.UserNodeChanged:
                        {
                            noticeHandler = new UserNodeChangedNodeNoticeHandler(notice, nodeConnection, appServiceProvider.UpdateUsersService);
                        }
                        break;
                    case NodeNoticeCode.Polling:
                        {
                            noticeHandler = new PollingNodeNoticeHandler(notice, nodeConnection, appServiceProvider.PollsService, appServiceProvider.NodeRequestSender);
                        }
                        break;
                    case NodeNoticeCode.Proxy:
                        {
                            noticeHandler = new ProxyUsersNotificationsNodeNoticeHandler(notice, nodeConnection, appServiceProvider.ConnectionsService);
                        }
                        break;
                    case NodeNoticeCode.MessagesUpdated:
                        {
                            noticeHandler = new MessagesUpdatedNodeNoticeHandler(
                                notice,
                                nodeConnection,
                                appServiceProvider.ConversationsNoticeService,
                                appServiceProvider.UpdateMessagesService,
                                appServiceProvider.AttachmentsService);
                        }
                        break;
                    case NodeNoticeCode.NewNodeKeys:
                        {
                            noticeHandler = new NewNodeKeysNodeNoticeHandler(notice, nodeConnection);
                        }
                        break;
                    case NodeNoticeCode.AllMessagesDeleted:
                        {
                            noticeHandler = new DeleteAllUserMessagesNodeNoticeHandler(notice, nodeConnection, appServiceProvider.DeleteMessagesService, appServiceProvider.LoadUsersService);
                        }
                        break;
                    case NodeNoticeCode.ConversationAction:
                        {
                            noticeHandler = new ConversationActionNodeNoticeHandler(notice, nodeConnection, appServiceProvider.ConversationsService, appServiceProvider.ConversationsNoticeService,appServiceProvider.LoadDialogsService,
                                appServiceProvider.SystemMessagesService);
                        }
                        break;
                    default:
                        {
                            throw new ArgumentException($"Unknown NoticeCode: {notice.NoticeCode}");
                        }
                }
                try
                {
                    if (noticeHandler.IsObjectValid())
                    {
                        await noticeHandler.HandleAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex, notice);
                }
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private async void HandleResponse(NodeResponse response)
        {
            try
            {
                ICommunicationHandler handler = null;
                switch (response.ResponseType)
                {
                    case NodeResponseType.Chats:
                        handler = new ChatsNodeResponseHandler(response, nodeConnection, appServiceProvider.CrossNodeService);
                        break;
                    case NodeResponseType.Users:
                        handler = new UsersNodeResponseHandler(response, nodeConnection, appServiceProvider.CrossNodeService);
                        break;              
                    case NodeResponseType.Channels:
                        handler = new ChannelsNodeResponseHandler(response, nodeConnection, appServiceProvider.CreateChannelsService);
                        break;                    
                    case NodeResponseType.Proxy:
                        handler = new ProxyUsersCommunicationsNodeResponseHandler(response, appServiceProvider.ConnectionsService);
                        break;                                       
                    case NodeResponseType.Files:
                        handler = new FilesInformationNodeResponseHandler(response, nodeConnection, appServiceProvider.FilesService);
                        break;
                }                               
                if (handler != null && handler.IsObjectValid())
                    await handler.HandleAsync().ConfigureAwait(false);        
                if (NodeDataReceiver.ResponseTasks.TryGetValue(response.RequestId, out var taskCompletionSource))
                {
                    taskCompletionSource.TrySetResult(response);                    
                }                
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        public static async Task SendResponseAsync(NodeResponse response, NodeConnection nodeConnection)
        {
            byte[] responseData;
            if (nodeConnection.IsEncryptedConnection)
            {
                responseData = Encryptor.SymmetricDataEncrypt(
                    ObjectSerializer.ObjectToByteArray(response),
                    NodeData.Instance.NodeKeys.SignPrivateKey,
                    nodeConnection.SymmetricKey,
                    MessageDataType.Response,
                    NodeData.Instance.NodeKeys.Password);
            }
            else
            {
                responseData = ObjectSerializer.ObjectToByteArray(response);
            }
            await nodeConnection.NodeWebSocket.SendAsync(
                responseData,
                System.Net.WebSockets.WebSocketMessageType.Binary,
                true,
                CancellationToken.None).ConfigureAwait(false);
        }
        public static async void SendResponse(NodeResponse response, NodeConnection nodeConnection)
        {
            try
            {
                await SendResponseAsync(response, nodeConnection).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex, "Error send response.");
            }
        }
    }
}