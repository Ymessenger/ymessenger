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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class LoadUsersTests
    {
        private FillTestDbHelper fillTestDbHelper;
        private ILoadUsersService loadUsersService;        
        public LoadUsersTests()
        {
            var testsData = TestsData.Create(nameof(LoadUsersTests));
            loadUsersService = testsData.AppServiceProvider.LoadUsersService;
            fillTestDbHelper = testsData.FillTestDbHelper;
        }
        [Fact]
        public async Task GetUser()
        {
            var expectedUser = fillTestDbHelper.Users.FirstOrDefault();
            var actualUser = await loadUsersService.GetUserAsync(expectedUser.Id);
            Assert.Equal(expectedUser.Tag, actualUser.Tag);
        }
        [Fact]
        public async Task GetUsersById()
        {
            List<long> usersIds = fillTestDbHelper.Users.Select(opt => opt.Id).Take(3).ToList();
            var expectedUsers = fillTestDbHelper.Users.Where(opt => usersIds.Contains(opt.Id)).ToList();
            var actualUsers = await loadUsersService.GetUsersByIdAsync(usersIds);
            Assert.True(expectedUsers.Count == actualUsers.Count && expectedUsers.All(opt => actualUsers.Any(p => opt.Id == p.Id)));
        }
        [Fact]
        public async Task GetUserInformation()
        {
            var expectedUser = fillTestDbHelper.Users.FirstOrDefault();
            var actualUser = await loadUsersService.GetUserInformationAsync(expectedUser.Id);
            Assert.True(expectedUser.Id == actualUser.Id
                && (expectedUser.Phones != null ? expectedUser.Phones.All(phone => actualUser.Phones.Any(p => p.FullNumber == phone.PhoneNumber)) : true)
                && (expectedUser.Emails != null ? expectedUser.Emails.All(email => actualUser.Emails.Any(p => p == email.EmailAddress) ): true));
        }
        [Fact]
        public async Task IsUserValid()
        {
            var validUser = fillTestDbHelper.Users.FirstOrDefault(user => !user.Deleted && user.Confirmed.Value);
            var invalidUser = fillTestDbHelper.Users.FirstOrDefault(user => !user.Confirmed.Value || user.Deleted);
            Assert.True(await loadUsersService.IsUserValidAsync(validUser.Id));
            Assert.False(await loadUsersService.IsUserValidAsync(invalidUser.Id));
        }
        [Fact]
        public async Task IsUserBlacklisted()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault(opt => opt.BlackList.Any());
            Assert.True(await loadUsersService.IsUserBlacklisted(user.Id, user.BlackList.Select(opt => opt.BadUid)));
            Assert.False(await loadUsersService.IsUserBlacklisted(user.Id, new List<long> { 10 }));
        }        
    }
}

