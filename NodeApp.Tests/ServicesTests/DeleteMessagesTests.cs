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
using NodeApp.Interfaces.Services.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class DeleteMessagesTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IDeleteMessagesService deleteMessagesService;
        public DeleteMessagesTests()
        {
            var testsData = TestsData.Create(nameof(DeleteMessagesTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            deleteMessagesService = testsData.AppServiceProvider.DeleteMessagesService;
        }
        [Fact]
        public async Task DeleteMessagesInfo()
        {
            var message = fillTestDbHelper.Messages.FirstOrDefault();
            var expectedMessage = MessageConverter.GetMessageVm(MessageConverter.GetMessageDto(message), null);            
            var actualMessage = (await deleteMessagesService.DeleteMessagesInfoAsync(
                expectedMessage.ConversationId.Value, 
                expectedMessage.ConversationType, 
                new List<Guid> { expectedMessage.GlobalId.Value },
                expectedMessage.SenderId.Value)).FirstOrDefault();
            Assert.Equal(expectedMessage.GlobalId, actualMessage.GlobalId);
        }
        [Fact]
        public async Task DeleteForwardedMessages()
        {
            var messages = fillTestDbHelper.Messages.Take(5).ToList();
            var expectedMessages = MessageConverter.GetMessagesVm(MessageConverter.GetMessagesDto(messages), null);
            var actualMessages = await deleteMessagesService.DeleteForwardedDialogMessagesAsync(expectedMessages);
            Assert.Equal(expectedMessages.Select(opt => opt.GlobalId.Value), actualMessages.Select(opt => opt.GlobalId));
        }
    }
}
