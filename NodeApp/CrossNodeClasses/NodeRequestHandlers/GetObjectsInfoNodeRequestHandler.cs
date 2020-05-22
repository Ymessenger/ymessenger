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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeRequestHandlers
{
    public class GetObjectsInfoNodeRequestHandler : ICommunicationHandler
    {
        private readonly GetObjectsInfoNodeRequest request;
        private readonly NodeConnection nodeConnection;
        private readonly ILoadChatsService loadChatsService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly ILoadUsersService loadUsersService;
        private readonly IPrivacyService privacyService;
        private readonly IFilesService filesService;

        public GetObjectsInfoNodeRequestHandler(NodeRequest request,
                                                NodeConnection nodeConnection,
                                                ILoadChatsService loadChatsService,
                                                ILoadUsersService loadUsersService,
                                                ILoadChannelsService loadChannelsService,
                                                IPrivacyService privacyService,
                                                IFilesService filesService)
        {
            this.request = (GetObjectsInfoNodeRequest) request;
            this.nodeConnection = nodeConnection;
            this.loadChatsService = loadChatsService;
            this.loadUsersService = loadUsersService;
            this.loadChannelsService = loadChannelsService;
            this.privacyService = privacyService;
            this.filesService = filesService;
        }
        public async Task HandleAsync()
        {            
            NodeResponse response;
            try
            {
                switch (request.RequestType)
                {
                    case Enums.NodeRequestType.GetChats:
                        {                            
                            IEnumerable<ChatVm> chats = await loadChatsService.GetChatsByIdAsync(request.ObjectsId, request.RequestorUserId).ConfigureAwait(false);                            
                            response = new ChatsNodeResponse(request.RequestId, chats);
                        }
                        break;
                    case Enums.NodeRequestType.GetUsers:
                        {                          
                            IEnumerable<UserVm> users = await loadUsersService.GetUsersByIdAsync(request.ObjectsId, request.RequestorUserId).ConfigureAwait(false);
                            users = await privacyService.ApplyPrivacySettingsAsync(users, request.RequestorUserId).ConfigureAwait(false);
                            response = new UsersNodeResponse(request.RequestId, users);
                        }
                        break;
                    case Enums.NodeRequestType.GetChannels:
                        {
                            IEnumerable<ChannelDto> channels = await loadChannelsService.GetChannelsWithSubscribersAsync(request.ObjectsId).ConfigureAwait(false);
                            response = new ChannelsNodeResponse(request.RequestId, channels);
                        }
                        break;
                    case Enums.NodeRequestType.GetFiles:
                        {
                            IEnumerable<FileInfoVm> files = await filesService.GetFilesInfoAsync(request.FilesIds).ConfigureAwait(false);
                            response = new FilesInformationResponse(request.RequestId, files);
                        }
                        break;
                    default:
                        response = new ResultNodeResponse(request.RequestId, ObjectsLibrary.Enums.ErrorCode.InvalidRequestData, "Unsupported request type.");
                        break;
                }
            }
            catch
            {
                response = new ResultNodeResponse(request.RequestId, ObjectsLibrary.Enums.ErrorCode.UnknownError, "An error occurred while processing the request.");
            }
            NodeWebSocketCommunicationManager.SendResponse(response, nodeConnection);                
        }

        public bool IsObjectValid()
        {
            return nodeConnection.Node != null && !request.ObjectsId.IsNullOrEmpty() || !request.FilesIds.IsNullOrEmpty();
        }
    }
}
