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
using NodeApp.MessengerData.DataTransferObjects;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class CreateMessagesTests
    {
        private readonly ICreateMessagesService createMessagesService;
        private readonly ILoadMessagesService loadMessagesService;
        private readonly FillTestDbHelper fillTestDbHelper;
        
        public CreateMessagesTests()
        {
            var testsData = TestsData.Create(nameof(CreateMessagesTests));
            createMessagesService = testsData.AppServiceProvider.CreateMessagesService;
            fillTestDbHelper = testsData.FillTestDbHelper;
            loadMessagesService = testsData.AppServiceProvider.LoadMessagesService;
        }
        [Fact]
        public async Task CreateChannelMessage()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var expectedMessage = new MessageDto
            {
                ConversationId = channel.ChannelId,
                ConversationType = ConversationType.Channel,
                GlobalId = Guid.NewGuid(),
                Text = "Create channel message test",
                SendingTime = DateTime.UtcNow.ToUnixTime(),
                SenderId = channel.ChannelUsers.FirstOrDefault(user => user.ChannelUserRole >= ChannelUserRole.Administrator).UserId                
            };
            var actualMessage = await createMessagesService.CreateChannelMessageAsync(expectedMessage);
            Assert.True(expectedMessage.Text == actualMessage.Text 
                && expectedMessage.SenderId == actualMessage.SenderId
                && expectedMessage.ConversationId == actualMessage.ConversationId
                && expectedMessage.ConversationType == actualMessage.ConversationType
                && expectedMessage.GlobalId == actualMessage.GlobalId);
        }
        [Fact]
        public async Task CreateChatMessage()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var expectedMessage = new MessageDto
            {
                ConversationId = chat.Id,
                ConversationType = ConversationType.Channel,
                GlobalId = Guid.NewGuid(),
                Text = "Create channel message test",
                SendingTime = DateTime.UtcNow.ToUnixTime(),
                SenderId = chat.ChatUsers.FirstOrDefault().UserId
            };
            var actualMessage = await createMessagesService.CreateChatMessageAsync(expectedMessage, expectedMessage.SenderId.Value);
            Assert.True(expectedMessage.Text == actualMessage.Text
               && expectedMessage.SenderId == actualMessage.SenderId
               && expectedMessage.ConversationId == actualMessage.ConversationId
               && expectedMessage.ConversationType == actualMessage.ConversationType
               && expectedMessage.GlobalId == actualMessage.GlobalId);
        }
       [Fact]
        public async Task SaveForwardedMessages()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var chatMessage = fillTestDbHelper.Messages.FirstOrDefault(opt => opt.ChatId == chat.Id);
            var expectedMessages = new List<MessageDto>
            {
                new MessageDto
                {
                    ConversationId = chat.Id,
                    ConversationType = ConversationType.Chat,
                    SenderId = chat.ChatUsers.FirstOrDefault().UserId,
                    Text = "Save messages test",
                    GlobalId = Guid.NewGuid(),
                    SendingTime = DateTime.UtcNow.ToUnixTime()
                },
                new MessageDto
                {
                    ConversationId = chat.Id,
                    ConversationType = ConversationType.Chat,
                    SenderId = chat.ChatUsers.LastOrDefault().UserId,
                    Text = "Save messages test",
                    GlobalId = Guid.NewGuid(),
                    SendingTime = DateTime.UtcNow.ToUnixTime()
                },
                MessageConverter.GetMessageDto(chatMessage)
            };
            await createMessagesService.SaveForwardedMessagesAsync(expectedMessages);
            var actualMessages = await loadMessagesService.GetMessagesByIdAsync(expectedMessages.Select(opt => opt.GlobalId), ConversationType.Chat, chat.Id, null);
            Assert.True(expectedMessages.Count() == actualMessages.Count());
            Assert.Equal(
                expectedMessages.OrderBy(opt => opt.SendingTime).Select(opt => opt.GlobalId), 
                actualMessages.OrderBy(opt => opt.SendingTime).Select(opt => opt.GlobalId));            
        }
        [Fact]
        public async Task SaveChatMessages()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();            
            var expectedMessages = new List<MessageDto>
            {
                new MessageDto
                {
                    ConversationId = chat.Id,
                    ConversationType = ConversationType.Chat,
                    SenderId = chat.ChatUsers.FirstOrDefault().UserId,
                    Text = "Save messages test",
                    GlobalId = Guid.NewGuid(),
                    SendingTime = DateTime.UtcNow.ToUnixTime()
                },
                new MessageDto
                {
                    ConversationId = chat.Id,
                    ConversationType = ConversationType.Chat,
                    SenderId = chat.ChatUsers.LastOrDefault().UserId,
                    Text = "Save messages test",
                    GlobalId = Guid.NewGuid(),
                    SendingTime = DateTime.UtcNow.ToUnixTime()
                }
            };
            var actualMessages = await createMessagesService.SaveMessagesAsync(expectedMessages, expectedMessages[0].SenderId.Value);
            Assert.Equal(
                expectedMessages.OrderBy(message => message.SendingTime).Select(message => message.GlobalId),
                actualMessages.OrderBy(message => message.SendingTime).Select(message => message.GlobalId));
        }
        [Fact]
        public async Task SaveDialogMessages()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            var expectedMessages = new List<MessageDto>
            {
                new MessageDto
                {
                    ConversationId = dialog.Id,
                    ConversationType = ConversationType.Dialog,
                    SenderId = dialog.FirstUID,
                    Text = "Save messages test",
                    GlobalId = Guid.NewGuid(),
                    SendingTime = DateTime.UtcNow.ToUnixTime(),
                    ReceiverId = dialog.SecondUID
                },
                new MessageDto
                {
                    ConversationId = dialog.Id,
                    ConversationType = ConversationType.Dialog,
                    SenderId = dialog.FirstUID,
                    Text = "Save messages test",
                    GlobalId = Guid.NewGuid(),
                    SendingTime = DateTime.UtcNow.ToUnixTime(),
                    ReceiverId = dialog.SecondUID
                }
            };
            var actualMessages = await createMessagesService.SaveMessagesAsync(expectedMessages, expectedMessages[0].SenderId.Value);
            Assert.Equal(
                expectedMessages.OrderBy(message => message.SendingTime).Select(message => message.GlobalId),
                actualMessages.OrderBy(message => message.SendingTime).Select(message => message.GlobalId));
        }
        [Fact]
        public async Task SaveChannelMessages()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var channelUser = channel.ChannelUsers.FirstOrDefault(opt => opt.ChannelUserRole >= ChannelUserRole.Administrator);
            var expectedMessages = new List<MessageDto>
            {
                new MessageDto
                {
                    ConversationId = channel.ChannelId,
                    ConversationType = ConversationType.Channel,
                    SenderId = channelUser.UserId,
                    Text = "Save messages test",
                    GlobalId = Guid.NewGuid(),
                    SendingTime = DateTime.UtcNow.ToUnixTime()
                },
                new MessageDto
                {
                    ConversationId = channel.ChannelId,
                    ConversationType = ConversationType.Channel,
                    SenderId = channelUser.UserId,
                    Text = "Save messages test",
                    GlobalId = Guid.NewGuid(),
                    SendingTime = DateTime.UtcNow.ToUnixTime()
                }
            };
            var actualMessages = await createMessagesService.SaveMessagesAsync(expectedMessages, expectedMessages[0].SenderId.Value);
            Assert.Equal(
                expectedMessages.OrderBy(message => message.SendingTime).Select(message => message.GlobalId),
                actualMessages.OrderBy(message => message.SendingTime).Select(message => message.GlobalId));
        }
        [Fact]
        public async Task SaveMixedMessages()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var channelUser = channel.ChannelUsers.FirstOrDefault(opt => opt.ChannelUserRole >= ChannelUserRole.Administrator);
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            var expectedMessages = new List<MessageDto>
            {
                new MessageDto
                {
                    ConversationId = channel.ChannelId,
                    ConversationType = ConversationType.Channel,
                    SenderId = channelUser.UserId,
                    Text = "Save messages test",
                    GlobalId = Guid.NewGuid(),
                    SendingTime = DateTime.UtcNow.AddSeconds(1).ToUnixTime()
                },
                new MessageDto
                {
                    ConversationId = dialog.Id,
                    ConversationType = ConversationType.Dialog,
                    SenderId = dialog.FirstUID,
                    Text = "Save messages test",
                    GlobalId = Guid.NewGuid(),
                    SendingTime = DateTime.UtcNow.AddSeconds(2).ToUnixTime(),
                    ReceiverId = dialog.SecondUID
                },
                new MessageDto
                {
                    ConversationId = chat.Id,
                    ConversationType = ConversationType.Chat,
                    SenderId = chat.ChatUsers.FirstOrDefault().UserId,
                    Text = "Save messages test",
                    GlobalId = Guid.NewGuid(),
                    SendingTime = DateTime.UtcNow.AddSeconds(3).ToUnixTime()
                }
            };
            var actualMessages = await createMessagesService.SaveMessagesAsync(expectedMessages, expectedMessages[1].SenderId.Value);
            Assert.Equal(
                expectedMessages.OrderBy(message => message.SendingTime).Select(message => message.GlobalId),
                actualMessages.OrderBy(message => message.SendingTime).Select(message => message.GlobalId));
        }        
    }
}