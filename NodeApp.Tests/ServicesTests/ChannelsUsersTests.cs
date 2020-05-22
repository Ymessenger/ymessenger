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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces.Services.Channels;
using ObjectsLibrary.Enums;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class ChannelsUsersTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly ICreateChannelsService createChannelsService;
        private readonly IUpdateChannelsService updateChannelsService;

        public ChannelsUsersTests()
        {
            var testsData = TestsData.Create(nameof(ChannelsUsersTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            createChannelsService = testsData.AppServiceProvider.CreateChannelsService;
            updateChannelsService = testsData.AppServiceProvider.UpdateChannelsService;
        }
        [Fact]
        public async Task CreateOrEditChannelUsers()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var expectedChannelUsers = ChannelConverter.GetChannelUsers(channel.ChannelUsers);
            var validUser = channel.ChannelUsers.FirstOrDefault(opt => opt.ChannelUserRole >= ChannelUserRole.Administrator);
            var invalidUser = channel.ChannelUsers.FirstOrDefault(opt => opt.ChannelUserRole == ChannelUserRole.Subscriber);
            foreach (var channelUser in expectedChannelUsers)
            {
                if (channelUser.UserId != validUser.UserId)
                    channelUser.Deleted = true;
            }
            var actualChannelUsers = await createChannelsService.CreateOrEditChannelUsersAsync(expectedChannelUsers, validUser.UserId);
            Assert.True(actualChannelUsers.All(opt => (opt.Deleted == true && opt.UserId != validUser.UserId) || opt.UserId == validUser.UserId));
            await Assert.ThrowsAsync<PermissionDeniedException>(async () =>
                await createChannelsService.CreateOrEditChannelUsersAsync(expectedChannelUsers, invalidUser.UserId));
        }
        [Fact]
        public async Task AddUsersToChannel()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var users = fillTestDbHelper.Users.Where(opt => opt.Id != channel.ChannelUsers.FirstOrDefault(p => p.ChannelUserRole == ChannelUserRole.Creator).UserId).ToList();
            var addedUsers = await updateChannelsService.AddUsersToChannelAsync(
                users.Select(opt => opt.Id).ToList(), 
                channel.ChannelId, 
                channel.ChannelUsers.FirstOrDefault(opt => opt.ChannelUserRole >= ChannelUserRole.Administrator).UserId);
            Assert.Equal(users.Count, addedUsers.Count);
        }        
    }
}
