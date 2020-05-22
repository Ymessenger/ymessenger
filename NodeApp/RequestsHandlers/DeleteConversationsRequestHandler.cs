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
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class DeleteConversationsRequestHandler : IRequestHandler
    {
        public DeleteConversationsRequestHandler(
            Request request,
            ClientConnection clientConnection,
            INodeNoticeService nodeNoticeService,
            ILoadChatsService loadChatsService,
            IDeleteChatsService deleteChatsService,
            ILoadDialogsService loadDialogsService,
            IDeleteDialogsService deleteDialogsService,
            ILoadChannelsService loadChannelsService,
            IDeleteChannelsService deleteChannelsService)
        {
            this.request = (DeleteConversationsRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.loadChatsService = loadChatsService;
            this.deleteChatsService = deleteChatsService;
            this.loadDialogsService = loadDialogsService;
            this.deleteDialogsService = deleteDialogsService;
            this.loadChannelsService = loadChannelsService;
            this.deleteChannelsService = deleteChannelsService;
        }

        private readonly DeleteConversationsRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly ILoadChatsService loadChatsService;
        private readonly IDeleteChatsService deleteChatsService;
        private readonly ILoadDialogsService loadDialogsService;
        private readonly IDeleteDialogsService deleteDialogsService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly IDeleteChannelsService deleteChannelsService;

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                switch (request.ConversationType)
                {
                    case ConversationType.Dialog:
                        {
                            var users = await loadDialogsService.GetDialogUsersAsync(request.ConversationId).ConfigureAwait(false);
                            await deleteDialogsService.DeleteDialogAsync(request.ConversationId, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                            UsersConversationsCacheService.Instance.UpdateUsersDialogsAsync(users.Select(opt => opt.Id.Value));
                        }
                        break;
                    case ConversationType.Chat:
                        {
                            List<long> usersId = await loadChatsService.GetChatUsersIdAsync(request.ConversationId).ConfigureAwait(false);
                            await deleteChatsService.DeleteChatAsync(request.ConversationId, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                            UsersConversationsCacheService.Instance.UpdateUsersChatsAsync(usersId);
                            BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateDeleteChatSegmentAsync(
                                request.ConversationId, NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                            BlockGenerationHelper.Instance.AddSegment(segment);
                        }
                        break;
                    case ConversationType.Channel:
                        {
                            List<long> usersId = await loadChannelsService.GetChannelUsersIdAsync(request.ConversationId).ConfigureAwait(false);
                            await deleteChannelsService.DeleteChannelAsync(request.ConversationId, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                            UsersConversationsCacheService.Instance.UpdateUsersChannelsAsync(usersId);
                            BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateDeleteChannelSegmentAsync(
                                request.ConversationId, NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                            BlockGenerationHelper.Instance.AddSegment(segment);
                        }
                        break;
                }
                nodeNoticeService.SendDeleteConversationsNodeNoticeAsync(request.ConversationId, request.ConversationType, clientConnection.UserId.GetValueOrDefault());
                return new ResultResponse(request.RequestId);
            }
            catch (PermissionDeniedException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Conversation not found or user does not have access to conversation.", ErrorCode.DeleteConversationProblem);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
            {
                throw new UnauthorizedUserException();
            }

            if (!clientConnection.Confirmed)
            {
                throw new PermissionDeniedException("User is not confirmed.");
            }

            return request.ConversationId != 0
                && (request.ConversationType >= ConversationType.Dialog || request.ConversationType <= ConversationType.Channel);
        }
    }
}