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
    public interface IDeleteMessagesService
    {
        Task<List<MessageDto>> DeleteForwardedDialogMessagesAsync(List<MessageVm> messages);
        Task DeleteMessagesAsync(long conversationId, ConversationType conversationType, long userId);
        Task DeleteMessagesAsync(long userId);
        Task<List<MessageDto>> DeleteMessagesInfoAsync(long conversationId, ConversationType conversationType, IEnumerable<Guid> messagesIds, long userId);
    }
}