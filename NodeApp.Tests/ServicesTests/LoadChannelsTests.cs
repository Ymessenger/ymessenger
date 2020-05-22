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
using NodeApp.Interfaces.Services.Channels;
using ObjectsLibrary.Enums;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class LoadChannelsTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly ILoadChannelsService loadChannelsService;
        public LoadChannelsTests()
        {
            var testsData = TestsData.Create(nameof(LoadChannelsTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            loadChannelsService = testsData.AppServiceProvider.LoadChannelsService;
        }
        [Fact]
        public async Task GetChannelById()
        {
            var expectedChannel = fillTestDbHelper.Channels.FirstOrDefault();
            var actualChannel = await loadChannelsService.GetChannelByIdAsync(expectedChannel.ChannelId);
            Assert.Equal(expectedChannel.ChannelId, actualChannel.ChannelId);
        }
        [Fact]
        public async Task GetChannelNodesIds()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var channelNodesId = channel.NodesId?.ToList();            
            var actualNodesId = await loadChannelsService.GetChannelNodesIdAsync(channel.ChannelId);
            channelNodesId.Sort();
            actualNodesId.Sort();
            Assert.Equal(channelNodesId, actualNodesId);
        }
        [Fact]
        public async Task GetChannels()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var expectedChannels = fillTestDbHelper.Channels;
            var actualChannels = await loadChannelsService.GetChannelsAsync(expectedChannels.Select(opt => opt.ChannelId), user.Id);
            Assert.Equal(expectedChannels.Select(opt => opt.ChannelId), actualChannels.Select(opt => opt.ChannelId.Value));
        }
        [Fact]
        public async Task GetChannelAdministration()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var expectedAdmins = channel.ChannelUsers.Where(opt => opt.ChannelUserRole >= ChannelUserRole.Administrator);
            var actualAdmins = await loadChannelsService.GetAdministrationChannelUsersAsync(channel.ChannelId);
            expectedAdmins = expectedAdmins.OrderBy(opt => opt.UserId);
            Assert.Equal(expectedAdmins.Select(opt => opt.UserId), actualAdmins.Select(opt => opt.UserId));
        }
        [Fact] 
        public async Task GetChannelUsers()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var expectedChannelUsers = channel.ChannelUsers.OrderBy(channelUser => channelUser.UserId);
            var actualChannelUsers = await loadChannelsService.GetChannelUsersAsync(channel.ChannelId, null, null);
            actualChannelUsers = actualChannelUsers.OrderBy(channelUser => channelUser.UserId).ToList();
            Assert.Equal(channel.ChannelUsers.Select(opt => opt.UserId), expectedChannelUsers.Select(opt => opt.UserId));
        }
        [Fact]
        public async Task GetChannelUsersWithParams()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var channelSubscriber = channel.ChannelUsers.FirstOrDefault(opt => opt.ChannelUserRole == ChannelUserRole.Subscriber);
            var channelAdmin = channel.ChannelUsers.FirstOrDefault(opt => opt.ChannelUserRole >= ChannelUserRole.Administrator);
            await Assert.ThrowsAsync<PermissionDeniedException>(async() => await loadChannelsService.GetChannelUsersAsync(channel.ChannelId, null, channelSubscriber.UserId));
            var expectedChannelUsers = channel.ChannelUsers.OrderBy(channelUser => channelUser.UserId);
            var actualChannelUsers = await loadChannelsService.GetChannelUsersAsync(channel.ChannelId, null, channelAdmin.UserId);
            actualChannelUsers = actualChannelUsers.OrderBy(channelUser => channelUser.UserId).ToList();
            Assert.Equal(channel.ChannelUsers.Select(opt => opt.UserId), expectedChannelUsers.Select(opt => opt.UserId));
        }
        [Fact]
        public async Task GetUserChannels()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var expectedChannels = fillTestDbHelper.Channels
                .Where(channel => channel.ChannelUsers.Any(opt => opt.UserId == user.Id && !opt.Banned && !opt.Deleted));
            var actualChannels = await loadChannelsService.GetUserChannelsAsync(user.Id);
            expectedChannels = expectedChannels.OrderBy(opt => opt.ChannelId);
            actualChannels = actualChannels.OrderBy(opt => opt.ChannelId).ToList();
            Assert.Equal(expectedChannels.Select(opt => opt.ChannelId), actualChannels.Select(opt => opt.ChannelId));            
        }
        [Fact]
        public async Task GetUserChannelsIds()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var expectedChannelsIds = fillTestDbHelper.Channels
                .Where(channel => channel.ChannelUsers.Any(opt => opt.UserId == user.Id && !opt.Banned && !opt.Deleted))
                .OrderBy(opt => opt.ChannelId)
                .Select(opt => opt.ChannelId);            
            var actualChannelsIds = await loadChannelsService.GetUserChannelsIdAsync(user.Id);
            actualChannelsIds.Sort();
            Assert.Equal(expectedChannelsIds, actualChannelsIds);
        }        
        [Fact]
        public async Task GetChannelUsersIds()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var expectedUsersIds = channel.ChannelUsers.OrderBy(opt => opt.UserId).Select(opt => opt.UserId);
            var actualUsersIds = await loadChannelsService.GetChannelUsersIdAsync(channel.ChannelId);
            actualUsersIds.Sort();
            Assert.Equal(expectedUsersIds, actualUsersIds);
        }
    }
}
