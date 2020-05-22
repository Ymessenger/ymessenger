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
    public class EditChatsRequestHandler : IRequestHandler
    {
        private readonly EditChatsRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IUpdateChatsService updateChatsService;
        private readonly ILoadChatsService loadChatsService;
        private readonly ISystemMessagesService systemMessagesService;
        public EditChatsRequestHandler(
            Request request,
            ClientConnection clientConnection,
            INodeNoticeService nodeNoticeService,
            IConversationsNoticeService conversationsNoticeService,
            IUpdateChatsService updateChatsService,
            ILoadChatsService loadChatsService,
            ISystemMessagesService systemMessagesService)
        {
            this.request = (EditChatsRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.updateChatsService = updateChatsService;
            this.loadChatsService = loadChatsService;
            this.systemMessagesService = systemMessagesService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<ChatVm> result = new List<ChatVm>();
            try
            {
                foreach (var chat in request.Chats)
                {
                    ChatVm editableChat = await loadChatsService.GetChatByIdAsync(chat.Id);
                    ChatVm editedChat =
                        await updateChatsService.EditChatAsync(chat, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                    result.Add(editedChat);
                    if (editableChat.Name != editedChat.Name)
                    {
                        var systemMessageInfo = SystemMessageInfoFactory.CreateNameChangedMessageInfo(editableChat.Name, editedChat.Name);
                        var message = await systemMessagesService.CreateMessageAsync(ConversationType.Chat, editedChat.Id.Value, systemMessageInfo);
                        conversationsNoticeService.SendSystemMessageNoticeAsync(message);
                    }
                    List<BlockSegmentVm> segments = await BlockSegmentsService.Instance.CreateEditPrivateChatSegmentsAsync(
                        editedChat,
                        NodeSettings.Configs.Node.Id,
                        NodeData.Instance.NodeKeys.SignPrivateKey,
                        NodeData.Instance.NodeKeys.SymmetricKey,
                        NodeData.Instance.NodeKeys.Password,
                        NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
                    if (segments.Any())
                    {
                        nodeNoticeService.SendBlockSegmentsNodeNoticeAsync(segments.ToList());
                        foreach (var segment in segments)
                        {
                            BlockGenerationHelper.Instance.AddSegment(segment);
                        }
                    }
                    editedChat.Users = null;
                    conversationsNoticeService.SendEditChatNoticeAsync(editedChat, clientConnection);
                    IEnumerable<long> chatUsersId = await loadChatsService.GetChatUsersIdAsync(chat.Id).ConfigureAwait(false);
                    UsersConversationsCacheService.Instance.UpdateUsersChatsAsync(chatUsersId);
                }

                nodeNoticeService.SendEditChatsNodeNoticeAsync(result);
                return new ChatsResponse(request.RequestId, result);
            }
            catch (PermissionDeniedException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Chat not found or user does not have access to chat.", ErrorCode.PermissionDenied);
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

            if (request.Chats == null || !request.Chats.Any())
            {
                return false;
            }

            if (request.Chats.Count(opt => opt.Name == "" || opt.Id == 0) > 0)
            {
                return false;
            }

            return true;
        }
    }
}