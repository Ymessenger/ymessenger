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
using NodeApp.Interfaces.Services.Chats;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class UpdateChatsTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IUpdateChatsService updateChatsService;
        public UpdateChatsTests()
        {
            var testsData = TestsData.Create(nameof(UpdateChatsTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            updateChatsService = testsData.AppServiceProvider.UpdateChatsService;
        }
        [Fact]
        public async Task AddUsersToChat()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var users = fillTestDbHelper.Users.TakeLast(2);
            var resultChat = await updateChatsService.AddUsersToChatAsync(
                users.Select(opt => opt.Id), 
                chat.Id, 
                chat.ChatUsers.FirstOrDefault(opt => opt.UserRole >= UserRole.Admin).UserId);
            Assert.Equal(users.Select(opt => opt.Id), resultChat.Users.Select(opt => opt.UserId));
        }
        [Fact]
        public async Task EditChat()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var expectedChat = new EditChatVm
            {
                Id = chat.Id,
                About = "Edited about",
                Name = "Edited name",
                Photo = "NewPhotoRef"
            };
            var actualChat = await updateChatsService.EditChatAsync(expectedChat, chat.ChatUsers.FirstOrDefault(opt => opt.UserRole >= UserRole.Admin).UserId);
            Assert.True(
                expectedChat.Id == actualChat.Id 
                && expectedChat.Name == actualChat.Name 
                && expectedChat.About == actualChat.About 
                && expectedChat.Photo == actualChat.Photo);
        }
        [Fact]
        public async Task EditChatUsers()
        {
            var chat = fillTestDbHelper.Chats.LastOrDefault();
            var chatUsers = chat.ChatUsers.ToList();
            var exptectedUsers = chatUsers.Select(opt => 
            { 
                if(opt.UserRole <= UserRole.Moderator)
                {
                    return new ChatUserVm
                    {
                        Banned = true,
                        Deleted = true,
                        UserRole = UserRole.Moderator,
                        UserId = opt.UserId,
                        ChatId = opt.ChatId,
                        IsMuted = opt.IsMuted,
                        InviterId = opt.InviterId,
                        Joined = opt.Joined                        
                    };
                }
                else
                {
                    return new ChatUserVm
                    {
                        Banned = opt.Banned,
                        Deleted = opt.Deleted,
                        ChatId = opt.ChatId,
                        UserId = opt.UserId,
                        UserRole = opt.UserRole,
                        IsMuted = opt.IsMuted,
                        InviterId = opt.InviterId,
                        Joined = opt.Joined
                    };
                }
            });
            var actualUsers = await updateChatsService.EditChatUsersAsync(exptectedUsers, chat.Id, chat.ChatUsers.FirstOrDefault(opt => opt.UserRole >= UserRole.Admin).UserId);
            Assert.Equal(
                ObjectSerializer.ObjectToJson(exptectedUsers.OrderBy(opt => opt.UserId)),
                ObjectSerializer.ObjectToJson(actualUsers.OrderBy(opt => opt.UserId)));
        }        
    }
}
