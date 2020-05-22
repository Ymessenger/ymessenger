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
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.Interfaces
{
    public interface INodeRequestSender
    {
        Task<ValuePair<TokenVm, UserVm>> CheckTokenAsync(long userId, TokenVm token, long nodeId);
        Task DownloadFileNodeRequestAsync(string fileId, NodeConnection nodeConnection);
        Task<UserDto> DownloadUserDataAsync(string operationId, long nodeId);
        Task<List<BlockVm>> GetBlocksAsync(long start, long end);
        Task<ChannelDto> GetChannelInformationAsync(long channelId, NodeConnection nodeConnection);
        Task<GetObjectsInfoNodeRequest> GetChatsInfoAsync(List<long> chatsId, NodeConnection nodeConnection);
        Task<List<ChatUserVm>> GetChatUsersInformationAsync(List<long> usersId, long chatId, NodeConnection nodeConnection);
        Task<ChatVm> GetFullChatInformationAsync(long chatId, NodeConnection nodeConnection);
        Task<List<MessageDto>> GetMessagesAsync(NodeConnection connection, long conversationId, ConversationType conversationType, Guid? messageId, List<AttachmentType> attachmentsTypes, bool direction = true, int length = 1000);
        Task<NodeKeysDto> GetNodePublicKeyAsync(NodeConnection nodeConnection, long? keyId = null);
        Task<PollDto> GetPollInformationAsync(long conversationId, ConversationType conversationType, Guid pollId, NodeConnection nodeConnection);
        Task<NodeResponse> GetResponseAsync(NodeRequest request, int timeoutMilliseconds = 5000);
        Task<SearchNodeResponse> GetSearchResponseAsync(string searchQuery, long? navigationUserId, bool? direction, List<SearchType> searchTypes, long? requestorId, NodeConnection node);
        Task<List<UserVm>> GetUsersInfoAsync(List<long> usersId, long? requestorUserId, NodeConnection nodeConnection);
        void SendConnectRequestAsync(List<NodeConnection> nodes);
        void SendConnectRequestAsync(NodeConnection nodeConnection);
        Task<ProxyUsersCommunicationsNodeResponse> SendProxyUsersCommunicationsNodeRequestAsync(byte[] communicationData, long userId, NodeConnection nodeConnection, ObjectType objectType, byte[] userPublicKey, byte[] signPublicKey);
        Task<List<UserVm>> BatchPhonesSearchAsync(NodeConnection nodeConnection, List<string> phones, long? requestorId);
        Task<List<FileInfoVm>> GetFilesInformationAsync(List<string> list, long? nodeId);
    }
}