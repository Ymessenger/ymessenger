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
using System.Collections.Generic;
using System.Threading.Tasks;
using NodeApp.MessengerData.DataTransferObjects;
using ObjectsLibrary.ViewModels;

namespace NodeApp.Interfaces.Services.Chats
{
    public interface ILoadChatsService
    {
        Task<List<ChatVm>> FindChatsAsync(SearchChatVm template, int limit = 100, long navigationChatId = 0, long? nodeId = null);
        Task<List<ChatVm>> FindChatsByStringQueryAsync(string searchQuery, long? navigationId = 0, bool? direction = true, long? userId = null);
        Task<ChatVm> GetChatByIdAsync(long chatId);
        Task<List<long>> GetChatNodeListAsync(long chatId);
        Task<List<ChatVm>> GetChatsByIdAsync(IEnumerable<long> chatsId, long? userId);
        Task<List<ChatVm>> GetChatsNodeAsync(long userId = 0, byte limit = 100, long navigationId = 0);
        Task<List<ChatUserDto>> GetChatUsersAsync(IEnumerable<long> usersId, long chatId);
        Task<List<ChatUserVm>> GetChatUsersAsync(long chatId, long? userId, int limit = 100, long navUserId = 0);
        Task<List<long>> GetChatUsersIdAsync(long chatId, bool banned = false, bool deleted = false);
        Task<List<ConversationPreviewVm>> GetUserChatPreviewAsync(long userId, long? navigationTime);
        Task<List<ChatDto>> GetUserChatsAsync(long userId);
        Task<List<long>> GetUserChatsIdAsync(long userId);
        Task<bool> IsUserJoinedToChatAsync(long chatId, long userId);
    }
}