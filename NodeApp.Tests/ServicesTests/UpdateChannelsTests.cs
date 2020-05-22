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
using NodeApp.Interfaces.Services.Channels;
using ObjectsLibrary.Enums;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class UpdateChannelsTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IUpdateChannelsService updateChannelsService;
        public UpdateChannelsTests()
        {
            var testsData = TestsData.Create(nameof(UpdateChannelsTests));
            fillTestDbHelper = testsData.FillTestDbHelper;            
            updateChannelsService = testsData.AppServiceProvider.UpdateChannelsService;
        }
        [Fact]
        public async Task EditChannel()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var expectedChannel = ChannelConverter.GetChannelVm(ChannelConverter.GetChannelDto(channel));
            expectedChannel.ChannelName = "Edited channel name";
            expectedChannel.About = "Edited channel about";
            expectedChannel.Tag = "EDITEDTAG";
            var actualChannel = await updateChannelsService.EditChannelAsync(expectedChannel, channel.ChannelUsers.FirstOrDefault(opt => opt.ChannelUserRole >= ChannelUserRole.Administrator).UserId);
            Assert.True( expectedChannel.ChannelName == actualChannel.ChannelName && expectedChannel.About == actualChannel.About && expectedChannel.Tag != actualChannel.Tag);
        }        
    }
}
