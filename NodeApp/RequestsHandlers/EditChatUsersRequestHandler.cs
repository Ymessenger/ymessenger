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
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class EditChatUsersRequestHandler : IRequestHandler
    {
        private readonly EditChatUsersRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly ILoadChatsService loadChatsService;
        private readonly IUpdateChatsService updateChatsService;
        private readonly ISystemMessagesService systemMessagesService;

        public EditChatUsersRequestHandler(
            Request request,
            ClientConnection clientConnection,
            INodeNoticeService nodeNoticeService,
            IConversationsNoticeService conversationsNoticeService,
            ILoadChatsService loadChatsService,
            IUpdateChatsService updateChatsService,
            ISystemMessagesService systemMessagesService)
        {
            this.request = (EditChatUsersRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.loadChatsService = loadChatsService;
            this.updateChatsService = updateChatsService;
            this.systemMessagesService = systemMessagesService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                List<ChatUserVm> chatUsers = await updateChatsService.EditChatUsersAsync(
                        request.ChatUsers, request.ChatId, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                var bannedUsers = chatUsers.Where(opt => opt.Banned == true);
                if (!bannedUsers.IsNullOrEmpty())
                {
                    foreach (var user in bannedUsers)
                    {
                        var systemMessageInfo = SystemMessageInfoFactory.CreateUserBannedMessageInfo(user.UserId);
                        var message = await systemMessagesService.CreateMessageAsync(ConversationType.Chat, user.ChatId.Value, systemMessageInfo);
                        conversationsNoticeService.SendSystemMessageNoticeAsync(message);
                    }
                }
                var deletedUsers = chatUsers.Where(opt => opt.Deleted == true);
                if (!deletedUsers.IsNullOrEmpty())
                {
                    foreach (var user in deletedUsers)
                    {
                        var systemMessageInfo = SystemMessageInfoFactory.CreateUserRemovedMessageInfo(user.UserId);
                        var message = await systemMessagesService.CreateMessageAsync(ConversationType.Chat, user.ChatId.Value, systemMessageInfo);
                        conversationsNoticeService.SendSystemMessageNoticeAsync(message);
                    }
                }

                ChatVm chat = await loadChatsService.GetChatByIdAsync(request.ChatId).ConfigureAwait(false);
                BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateChangeUsersChatSegmetAsync(
                    chatUsers,
                    request.ChatId,
                    clientConnection.UserId.GetValueOrDefault(),
                    NodeSettings.Configs.Node.Id,
                    chat.Type,
                    NodeData.Instance.NodeKeys.SignPrivateKey,
                    NodeData.Instance.NodeKeys.SymmetricKey,
                    NodeData.Instance.NodeKeys.Password,
                    NodeData.Instance.NodeKeys.KeyId)
                    .ConfigureAwait(false);
                BlockGenerationHelper.Instance.AddSegment(segment);
                nodeNoticeService.SendBlockSegmentsNodeNoticeAsync(new List<BlockSegmentVm> { segment });
                conversationsNoticeService.SendChangeChatUsersNoticeAsync(chatUsers, request.ChatId, clientConnection);
                nodeNoticeService.SendChangeChatUsersNodeNoticeAsync(chatUsers.ToList(), request.ChatId, clientConnection.UserId.GetValueOrDefault(), chat);
                UsersConversationsCacheService.Instance.UpdateUsersChatsAsync(chatUsers.Select(opt => opt.UserId));
                return new ChatUsersResponse(request.RequestId, chatUsers);
            }
            catch (ChatUserBlockedException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "User blocked in chat.", ErrorCode.UserBlocked);
            }
            catch (UserIsNotInConversationException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "User is not in chat.", ErrorCode.UserIsNotInChat);
            }
            catch (EditConversationUsersException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "User does not access to the chat.", ErrorCode.PermissionDenied);
            }
            catch (ConversationNotFoundException ex)
            {
                Logger.WriteLog(ex);
                return new ResultResponse(request.RequestId, "Chat not found.", ErrorCode.ObjectDoesNotExists);
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

            if (request.ChatUsers == null || !request.ChatUsers.Any())
            {
                return false;
            }

            request.ChatUsers = request.ChatUsers.Distinct().ToList();
            if (request.ChatId == 0 || request.ChatUsers.Count() > 100)
            {
                return false;
            }

            return true;
        }
    }
}
