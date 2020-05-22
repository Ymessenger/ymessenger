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
using NodeApp.Converters;
using NodeApp.Interfaces.Services.Chats;
using ObjectsLibrary.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class CreateChatsTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly ICreateChatsService createChatsService;
        public CreateChatsTests()
        {
            var testsData = TestsData.Create(nameof(CreateChatsTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            createChatsService = testsData.AppServiceProvider.CreateChatsService;
        }
        [Fact]
        public async Task CreateChat()
        {
            var creator = fillTestDbHelper.Users.FirstOrDefault();
            var users = fillTestDbHelper.Users.Where(opt => !opt.BlackList.Any(p => p.BadUid == creator.Id) && !creator.BlackList.Any(p => p.BadUid == opt.Id)).Take(5).ToList();
            ChatVm expectedChat = new ChatVm
            {
                About = "Create chat test",
                Name = "Chat",
                Users = users.Select(opt => new ChatUserVm 
                { 
                    UserId = opt.Id,
                    UserRole = opt.Id == creator.Id ? ObjectsLibrary.Enums.UserRole.Creator : ObjectsLibrary.Enums.UserRole.Moderator           
                }).ToList()
            };
            var actualChat = await createChatsService.CreateChatAsync(expectedChat, creator.Id);
            Assert.True(expectedChat.About == actualChat.About && expectedChat.Name == actualChat.Name);            
        }
        [Fact]
        public async Task CreateChats()
        {
            var creator = fillTestDbHelper.Users.FirstOrDefault();
            var users = fillTestDbHelper.Users.Where(opt => !opt.BlackList.Any(p => p.BadUid == creator.Id) && !creator.BlackList.Any(p => p.BadUid == opt.Id)).Take(5).ToList();
            ChatVm newChat = new ChatVm
            {
                About = "Create chat test 2",
                Name = "Chat",
                Users = users.Select(opt => new ChatUserVm
                {
                    UserId = opt.Id,
                    UserRole = opt.Id == creator.Id ? ObjectsLibrary.Enums.UserRole.Creator : ObjectsLibrary.Enums.UserRole.Moderator
                }).ToList()
            };
            var userChats = fillTestDbHelper.Chats.Where(opt => opt.ChatUsers.Any(p => p.UserId == creator.Id));
            var expectedChats = ChatConverter.GetChatsDto(userChats).Append(ChatConverter.GetChatDto(newChat)).ToList();
            var actualChats = await createChatsService.CreateOrUpdateUserChatsAsync(expectedChats);
            Assert.Equal(expectedChats.Count, actualChats.Count);
        }
    }
}
