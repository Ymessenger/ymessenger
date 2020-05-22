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
    public class NewChatRequestHandler : IRequestHandler
    {
        private readonly NewChatsRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly ICreateChatsService createChatsService;

        public NewChatRequestHandler(
            Request request,
            ClientConnection current,
            INodeNoticeService nodeNoticeService,
            IConversationsNoticeService conversationsNoticeService,
            ICreateChatsService createChatsService)
        {
            this.request = (NewChatsRequest)request;
            this.clientConnection = current;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.createChatsService = createChatsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            List<ChatVm> chats = new List<ChatVm>();
            try
            {
                foreach (var chat in request.Chats)
                {
                    ChatVm newChat = await createChatsService.CreateChatAsync(chat, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                    conversationsNoticeService.SendNewChatNoticeAsync(newChat, clientConnection);
                    UsersConversationsCacheService.Instance.UpdateUsersChatsAsync(newChat.Users.Select(opt => opt.UserId));
                    chats.Add(newChat);
                    List<BlockSegmentVm> segments = await BlockSegmentsService.Instance.CreateNewPrivateChatSegmentsAsync(
                        newChat,
                        NodeSettings.Configs.Node.Id,
                        NodeData.Instance.NodeKeys.SignPrivateKey,
                        NodeData.Instance.NodeKeys.SymmetricKey,
                        NodeData.Instance.NodeKeys.Password,
                        NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
                    if (segments.Any())
                    {
                        BlockGenerationHelper.Instance.AddSegments(segments);
                        nodeNoticeService.SendBlockSegmentsNodeNoticeAsync(segments.ToList());
                    }
                }
                nodeNoticeService.SendNewChatsNodeNoticeAsync(chats);
                return new ChatsResponse(request.RequestId, chats);
            }
            catch (UserBlockedException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "The user is blacklisted by another user.", ErrorCode.UserBlocked);
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

            if (request.Chats == null)
            {
                return false;
            }

            if (request.Chats.Count == 0 || request.RequestId == 0)
            {
                return false;
            }

            var chatsId = request.Chats.Select(opt => opt.Id).Distinct();
            if (request.Chats.Count > chatsId.Count())
            {
                return false;
            }

            foreach (var chat in request.Chats)
            {
                if (chat.Type > ChatType.Public || chat.Type < ChatType.Private)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(chat.Name))
                {
                    return false;
                }
            }
            return true;
        }
    }
}