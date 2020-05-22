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
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.DataTransferObjects;
using ObjectsLibrary.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class UpdateUsersTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IUpdateUsersService updateUsersService;
        private readonly ILoadUsersService loadUsersService;
        public UpdateUsersTests()
        {
            var testsData = TestsData.Create("updateUsersTests");
            fillTestDbHelper = testsData.FillTestDbHelper;
            updateUsersService = testsData.AppServiceProvider.UpdateUsersService;
            loadUsersService = testsData.AppServiceProvider.LoadUsersService;
        }
        [Fact]
        public async Task EditUser()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault(opt => !opt.Deleted && opt.Confirmed.Value);
            EditUserVm userEdited = new EditUserVm
            {                 
                About = "Edited about",
                Photo = "Edited photo",
                NameFirst = "Edited namefirst",
                NameSecond = "Edited namesecond"                               
            };
            var actualUser = await updateUsersService.EditUserAsync(userEdited, user.Id);
            Assert.True(actualUser.About == userEdited.About
                && actualUser.NameFirst == userEdited.NameFirst
                && actualUser.NameSecond == userEdited.NameSecond
                && actualUser.Photo == userEdited.Photo);            
        }
        [Fact]
        public async Task EditUserNode()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            await updateUsersService.EditUserNodeAsync(user.Id, 2);
            var actualUser = await loadUsersService.GetUserAsync(user.Id);
            Assert.Equal(2, actualUser.NodeId);
        }
        [Fact]
        public async Task AddUsersToBlacklist()
        {
            var user = fillTestDbHelper.Users.LastOrDefault(opt => !opt.Deleted && opt.Confirmed.GetValueOrDefault());
            var expectedUsersIds = fillTestDbHelper.Users.Select(opt => opt.Id).Take(2).ToList();
            var usersIds = await updateUsersService.AddUsersToBlackListAsync(expectedUsersIds, user.Id);
            var actual = await loadUsersService.GetUserInformationAsync(user.Id);
            Assert.True(actual.BlackList.Intersect(usersIds).OrderBy(id => id).SequenceEqual(usersIds.OrderBy(id => id)));
        }
        [Fact]
        public async Task RemoveUsersFromBlacklist()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var expectedUsersIds = fillTestDbHelper.Users.Select(opt => opt.Id).Take(2).ToList();
            var addedUsersIds = await updateUsersService.AddUsersToBlackListAsync(expectedUsersIds, user.Id);
            var removedUsersIds = await updateUsersService.DeleteUsersFromBlackListAsync(addedUsersIds, user.Id);
            var actual = await loadUsersService.GetUserInformationAsync(user.Id);
            Assert.Empty(actual.BlackList);
        }
        [Fact]
        public async Task CreateOrUpdateUser()
        {
            var newUser = new UserDto
            {
                Id = 1488,
                NameFirst = "CRTOUPD",
                Confirmed = false
            };
            var createdUser = await updateUsersService.CreateOrUpdateUserAsync(newUser);
            Assert.True(newUser.Id == createdUser.Id 
                && newUser.NameFirst == createdUser.NameFirst 
                && newUser.Confirmed == createdUser.Confirmed);
            var user = fillTestDbHelper.Users.FirstOrDefault();
            user.NameFirst = "updated_user";
            var updatedUser = await updateUsersService.CreateOrUpdateUserAsync(UserConverter.GetUserDto(user));
            Assert.Equal(user.Id, updatedUser.Id);
            Assert.Equal(user.NameFirst, updatedUser.NameFirst);
        }
        [Fact]
        public async Task SetUsersConfirmed()
        {
            var unconfirmed = fillTestDbHelper.Users.Where(opt => !opt.Confirmed.Value).ToList();
            await updateUsersService.SetUsersConfirmedAsync(unconfirmed.Select(opt => opt.Id).ToList());
        }        
    }
}
