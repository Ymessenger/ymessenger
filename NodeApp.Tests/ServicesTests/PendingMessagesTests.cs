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
using NodeApp.Interfaces;
using ObjectsLibrary.Converters;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class PendingMessagesTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IPendingMessagesService pendingMessagesService;
        public PendingMessagesTests()
        {
            TestsData testsData = TestsData.Create(nameof(PendingMessagesTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            pendingMessagesService = testsData.AppServiceProvider.PendingMessagesService;
        }
        [Fact]
        public async Task AddNodePendingMessage()
        {
            var content = Guid.NewGuid();
            var pendingMessage = await pendingMessagesService.AddNodePendingMessageAsync(2, content, TimeSpan.FromSeconds(10));
            Assert.True(pendingMessage.NodeId == 2 && Convert.FromBase64String(pendingMessage.Content).SequenceEqual(ObjectSerializer.ObjectToByteArray(content)));
        }
        [Fact]
        public async Task AddUserPendingMessages()
        {
            var content = Guid.NewGuid();
            var pendingMessage = await pendingMessagesService.AddUserPendingMessageAsync(-1, content, content);
            Assert.True(pendingMessage.ReceiverId == -1 && pendingMessage.Content == ObjectSerializer.ObjectToJson(content.ToString()));
        }
        [Fact]
        public async Task GetNodePendingMessages()
        {
            var content = Guid.NewGuid();
            var expectedPendingMessage = await pendingMessagesService.AddNodePendingMessageAsync(2, content, TimeSpan.FromSeconds(10));
            var actualPendingMessages = await pendingMessagesService.GetNodePendingMessagesAsync(2);
            Assert.Contains(actualPendingMessages, message => message.Content == expectedPendingMessage.Content);
        }
        [Fact]
        public async Task GetUserPendingMessages()
        {
            var content = Guid.NewGuid();
            var user = fillTestDbHelper.Users.Skip(1).FirstOrDefault();
            var expectedPendingMessage = await pendingMessagesService.AddUserPendingMessageAsync(user.Id, content, content);
            var actualPendingMessage = (await pendingMessagesService.GetUserPendingMessagesAsync(user.Id)).FirstOrDefault();
            Assert.True(expectedPendingMessage.Content == actualPendingMessage.Content);
        }        
    }
}
