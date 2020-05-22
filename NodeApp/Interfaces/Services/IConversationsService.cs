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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;

namespace NodeApp.Interfaces
{
    public interface IConversationsService
    {
        Task<long> GetConversationNodeIdAsync(ConversationType conversationType, long conversationId);
        Task<List<long>> GetConversationNodesIdsAsync(ConversationType conversationType, long conversationId);
        Task<bool> IsUserInConversationAsync(ConversationType conversationType, long conversationId, long userId);
        Task MuteConversationAsync(ConversationType conversationType, long conversationId, long? userId);
        void UpdateConversationLastMessageId(long conversationId, ConversationType conversationType, Guid messageId);
        Task<List<ConversationPreviewVm>> GetUsersConversationsAsync(long userId, long? conversationId, ConversationType? conversationType, long requestId);
    }
}