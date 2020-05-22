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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces.Services.Chats;
using ObjectsLibrary.Enums;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class DeleteChatsTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IDeleteChatsService deleteChatsService;
        public DeleteChatsTests()
        {
            TestsData testsData = TestsData.Create(nameof(DeleteChatsTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            deleteChatsService = testsData.AppServiceProvider.DeleteChatsService;
        }
        [Fact]
        public async Task DeleteChats()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var creatorChat = chat.ChatUsers.FirstOrDefault(opt => opt.UserRole == UserRole.Creator);
            await deleteChatsService.DeleteChatAsync(chat.Id, creatorChat.UserId);
        }
        [Fact]
        public async Task DeleteChatsException()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var creatorChat = chat.ChatUsers.FirstOrDefault(opt => opt.UserRole != UserRole.Creator);
            await Assert.ThrowsAsync<PermissionDeniedException>(async () => await deleteChatsService.DeleteChatAsync(chat.Id, creatorChat.UserId));
        }
    }
}
