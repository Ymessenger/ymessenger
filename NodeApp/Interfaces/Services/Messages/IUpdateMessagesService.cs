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
using NodeApp.MessengerData.DataTransferObjects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;

namespace NodeApp.Interfaces.Services.Messages
{
    public interface IUpdateMessagesService
    {
        Task<List<MessageDto>> DialogMessagesReadAsync(IEnumerable<Guid> messagesId, long dialogId, long userId);
        Task<MessageDto> EditMessageAsync(MessageDto message, long editorId);
        Task<List<MessageDto>> SetDialogMessagesReadAsync(IEnumerable<Guid> messagesId, long userId, long dialogId);
        Task<List<MessageDto>> SetDialogMessagesReadByUsersIdAsync(IEnumerable<Guid> messagesId, long firstUserId, long secondUserId);
        Task<List<MessageDto>> SetMessagesReadAsync(IEnumerable<Guid> messagesId, long conversationId, ConversationType conversationType, long readerId);
        Task UpdateMessagesNodesIdsAsync(List<MessageVm> messages, IEnumerable<long> nodesIds);
    }
}