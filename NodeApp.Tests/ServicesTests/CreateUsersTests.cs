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
using NodeApp.MessengerData.Services.Users;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class CreateUsersTests
    {        
        private readonly ICreateUsersService createUsersService;
        public CreateUsersTests()
        {
            var testsData = TestsData.Create(nameof(CreateUsersService));
            createUsersService = testsData.AppServiceProvider.CreateUsersService;
        }
        [Fact]
        public async Task CreateUser()
        {
            string password = RandomExtensions.NextString(10);
            UserVm user = new UserVm
            {
                NameFirst = RandomExtensions.NextString(20),
                NameSecond = RandomExtensions.NextString(20),
                About = RandomExtensions.NextString(50),
                Photo = RandomExtensions.NextString(100),
                Phones = new List<UserPhoneVm>
                {
                    new UserPhoneVm
                    {
                        CountryCode = 7,
                        SubscriberNumber = 9131743886,
                        IsMain = true,
                        UserId = 1000
                    }
                },
                Emails = new List<string>
                {
                    "ilyalex@petrovich.com"
                }                
            };
            var actualUser = await createUsersService.CreateNewUserAsync(user, 1, true, password);
            Assert.Equal(user.NameFirst, actualUser.FirstValue.NameFirst);
            Assert.Equal(user.NameSecond, actualUser.FirstValue.NameSecond);
            Assert.Equal(user.About, actualUser.FirstValue.About);
            Assert.Equal(user.Photo, actualUser.FirstValue.Photo);
            Assert.Equal(user.Phones.FirstOrDefault().FullNumber, actualUser.FirstValue.Phones.FirstOrDefault().FullNumber);
            Assert.Equal(user.Emails, actualUser.FirstValue.Emails);
            Assert.Equal(password, actualUser.SecondValue);
        }
    }
}
