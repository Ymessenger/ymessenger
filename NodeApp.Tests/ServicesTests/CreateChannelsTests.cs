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
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class CreateChannelsTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly ICreateChannelsService createChannelsService;
        public CreateChannelsTests()
        {
            var testsData = TestsData.Create(nameof(CreateChannelsTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            createChannelsService = testsData.AppServiceProvider.CreateChannelsService;
        }
        [Fact]
        public async Task CreateChannelWithoutSubs()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var expectedChannel = new ChannelVm
            {
                About = RandomExtensions.NextString(10),
                ChannelName = "Created channel"               
            };
            var actualChannel = await createChannelsService.CreateChannelAsync(expectedChannel, user.Id, null);
            Assert.True(expectedChannel.ChannelName == actualChannel.ChannelName 
                && expectedChannel.About == actualChannel.About
                && actualChannel.ChannelUsers.Count == 1);
        }
        [Fact]
        public async Task CreateChannelWithSubs()
        {
            var users = fillTestDbHelper.Users.Skip(1);
            var creator = fillTestDbHelper.Users.FirstOrDefault();
            var expectedChannel = new ChannelVm
            {
                About = RandomExtensions.NextString(10),
                ChannelName = "Created channel",
                Tag = "TAGTAGTAG",
                NodesId = RandomExtensions.GetRandomInt64Sequence(3, 0).ToList(),
                ChannelUsers = users.Select(user => new ChannelUserVm 
                {
                    UserId = user.Id,
                    ChannelUserRole = ChannelUserRole.Subscriber
                }).ToList()
            };
            var actualChannel = await createChannelsService.CreateChannelAsync(expectedChannel, creator.Id, expectedChannel.ChannelUsers);
            Assert.True(expectedChannel.ChannelUsers.Count() + 1 == actualChannel.ChannelUsers.Count());
        }
        [Fact]
        public async Task CreateOrEditChannel()
        {            
            var editableChannel = fillTestDbHelper.Channels.FirstOrDefault();
            var validUser = editableChannel.ChannelUsers.FirstOrDefault(channelUser => channelUser.ChannelUserRole >= ChannelUserRole.Administrator);
            var invalidUser = editableChannel.ChannelUsers.FirstOrDefault(channelUser => channelUser.ChannelUserRole == ChannelUserRole.Subscriber);
            editableChannel.ChannelName = "Edited channel name";
            var actualChannel = await createChannelsService.CreateOrEditChannelAsync(ChannelConverter.GetChannel(editableChannel), validUser.UserId, null);
            Assert.True(editableChannel.ChannelName == actualChannel.ChannelName);
            await Assert.ThrowsAsync<PermissionDeniedException>(async () => 
                await createChannelsService.CreateOrEditChannelAsync(ChannelConverter.GetChannel(editableChannel), invalidUser.UserId, null));
            var users = fillTestDbHelper.Users.Skip(1);
            var creator = fillTestDbHelper.Users.FirstOrDefault();
            var newExpectedChannel = new ChannelVm
            {
                About = RandomExtensions.NextString(10),
                ChannelName = "Created channel",               
                ChannelUsers = users.Select(user => new ChannelUserVm
                {
                    UserId = user.Id,
                    ChannelUserRole = ChannelUserRole.Subscriber
                }).ToList()
            };
            var newActualChannel = await createChannelsService.CreateOrEditChannelAsync(newExpectedChannel, creator.Id, newExpectedChannel.ChannelUsers);
            Assert.True(newExpectedChannel.ChannelUsers.Count() + 1 == newActualChannel.ChannelUsers.Count());
        }       
    }
}
