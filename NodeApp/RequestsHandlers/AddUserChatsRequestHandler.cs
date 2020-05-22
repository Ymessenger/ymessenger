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
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace NodeApp.RequestsHandlers
{
    public class AddUserChatsRequestHandler : IRequestHandler
    {
        private readonly AddUsersChatsRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IUpdateChatsService updateChatsService;
        private readonly ILoadChatsService loadChatsService;
        private readonly INodeRequestSender nodeRequestSender;
        private readonly IConnectionsService connectionsService;
        private readonly ISystemMessagesService systemMessagesService;
        public AddUserChatsRequestHandler(
            Request request,
            ClientConnection clientConnection,
            INodeNoticeService nodeNoticeService,
            IConversationsNoticeService conversationsNoticeService,
            IUpdateChatsService updateChatsService,
            ILoadChatsService loadChatsService,
            INodeRequestSender nodeRequestSender,
            IConnectionsService connectionsService,
            ISystemMessagesService systemMessagesService)
        {
            this.request = (AddUsersChatsRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.conversationsNoticeService = conversationsNoticeService;
            this.updateChatsService = updateChatsService;
            this.loadChatsService = loadChatsService;
            this.nodeRequestSender = nodeRequestSender;
            this.connectionsService = connectionsService;
            this.systemMessagesService = systemMessagesService;
        }

        public async Task<Response> CreateResponseAsync()
        {      
            try
            {                                         
                List<ChatUserVm> chatUsers = new List<ChatUserVm>();                
                List<ChatVm> updatedChats = new List<ChatVm>(); 
                foreach(long chatId in request.ChatsId)
                {
                    var chat = await loadChatsService.GetChatByIdAsync(chatId).ConfigureAwait(false);
                    if(chat != null && !(chat.Users?.Any(opt => opt.UserRole == UserRole.Creator) ?? false))
                    {
                        var nodeId = chat.NodesId.FirstOrDefault(id => id != NodeSettings.Configs.Node.Id);                        
                        var nodeConnection = connectionsService.GetNodeConnection(nodeId);
                        if (nodeConnection != null) 
                        {
                            await nodeRequestSender.GetFullChatInformationAsync(chatId, nodeConnection).ConfigureAwait(false);                                                        
                        }
                    }
                    updatedChats.Add(await updateChatsService.AddUsersToChatAsync(request.UsersId, chatId, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false));
                    foreach (var userId in request.UsersId) 
                    {
                        var message = await systemMessagesService.CreateMessageAsync(ConversationType.Chat, chatId, SystemMessageInfoFactory.CreateUserAddedMessageInfo(userId));
                        conversationsNoticeService.SendSystemMessageNoticeAsync(message);
                    }
                }               
                foreach(ChatVm chat in updatedChats)
                {                      
                    conversationsNoticeService.SendNewUsersAddedToChatNoticeAsync(chat, clientConnection);
                    if (chat.Users != null)
                    {
                        chatUsers.AddRange(chat.Users);
                        BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateAddUsersChatSegmentAsync(
                            chat.Users.Select(opt => opt.UserId).ToList(),
                            chat.Id.GetValueOrDefault(),
                            clientConnection.UserId.GetValueOrDefault(),
                            NodeSettings.Configs.Node.Id,
                            chat.Type,
                            NodeData.Instance.NodeKeys.SignPrivateKey,
                            NodeData.Instance.NodeKeys.SymmetricKey,
                            NodeData.Instance.NodeKeys.Password,
                            NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
                        BlockGenerationHelper.Instance.AddSegment(segment);
                        nodeNoticeService.SendAddUsersToChatNodeNoticeAsync(chat, clientConnection.UserId.GetValueOrDefault());
                        if (chat.Type == ChatType.Private)
                        {
                            nodeNoticeService.SendBlockSegmentsNodeNoticeAsync(new List<BlockSegmentVm> { segment });
                        }
                    }
                }
                return new ChatUsersResponse(request.RequestId, chatUsers);
            }
            catch(AddUserChatException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Failed to add user to chat.", ErrorCode.AddUserProblem);
            }
            catch(ConversationNotFoundException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Chat not found.", ErrorCode.ChatIsNotValid);
            }
            catch(ConversationIsNotValidException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Chat is deleted.", ErrorCode.ChatIsNotValid);
            }
            catch(UserBlockedException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "User is blocked in chat.", ErrorCode.UserBlocked);
            }               
            catch(Exception ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Internal server error.", ErrorCode.UnknownError);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)            
                throw new UnauthorizedUserException();            
            if (!clientConnection.Confirmed)
                throw new PermissionDeniedException("User is not confirmed.");
            if (request.ChatsId == null || !request.ChatsId.Any())            
                return false;            
            if (request.UsersId == null || !request.UsersId.Any())            
                return false;            
            request.ChatsId = request.ChatsId.Distinct().ToList();
            request.UsersId = request.UsersId.Distinct().ToList();
            if (request.ChatsId.Count() > 100 || request.UsersId.Count() > 100)            
                return false;            
            return true;
        }
    }
}