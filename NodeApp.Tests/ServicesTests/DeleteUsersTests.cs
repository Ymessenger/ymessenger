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
using NodeApp.Interfaces.Services.Users;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class DeleteUsersTests
    {
        private readonly IDeleteUsersService deleteUsersService;
        private readonly ILoadUsersService loadUsersService;
        private readonly FillTestDbHelper fillTestDbHelper;
        public DeleteUsersTests()
        {
            var testsData = TestsData.Create(nameof(DeleteUsersTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            deleteUsersService = testsData.AppServiceProvider.DeleteUsersService;
            loadUsersService = testsData.AppServiceProvider.LoadUsersService;
        }
        [Fact]
        public async Task DeleteUser()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            await deleteUsersService.DeleteUserAsync(user.Id);
            Assert.Null(await loadUsersService.GetUserAsync(user.Id));
        }
        [Fact]
        public async Task DeleteUserInformation()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            await deleteUsersService.DeleteUserInformationAsync(user.Id);
            var actualUser = await loadUsersService.GetUserAsync(user.Id);
            Assert.True(actualUser.NameSecond == null
                && actualUser.Photo == null
                && (actualUser.Phones == null || !actualUser.Phones.Any())
                && (actualUser.Emails == null || !actualUser.Emails.Any())
                && actualUser.About == null
                && actualUser.City == null
                && actualUser.Country == null
                && actualUser.Language == null);            
        }
        [Fact]
        public async Task DeleteUsers()
        {
            var usersIds = fillTestDbHelper.Users.Select(opt => opt.Id).Take(3);
            await deleteUsersService.DeleteUsersAsync(usersIds);
            var actualUsers = await loadUsersService.GetUsersByIdAsync(usersIds);
            Assert.Empty(actualUsers);
        }
    }
}
