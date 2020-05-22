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
using NodeApp.Interfaces.Services.Messages;
using ObjectsLibrary.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class LoadMessagesTests
    {
        private FillTestDbHelper fillTestDbHelper;
        private ILoadMessagesService loadMessagesService;

        public LoadMessagesTests()
        {
            var testsData = TestsData.Create("LoadMessagesTests");
            fillTestDbHelper = testsData.FillTestDbHelper;
            loadMessagesService = testsData.AppServiceProvider.LoadMessagesService;
        }

        [Fact]
        public async Task GetChannelMessagesById()
        {
            var expectedChannelMessage = fillTestDbHelper.Messages.FirstOrDefault(message => message.ChannelId != null);
            var channelMessages = await loadMessagesService.GetMessagesByIdAsync(
                new List<Guid> { expectedChannelMessage.GlobalId },
                ConversationType.Channel,
                expectedChannelMessage.ChannelId.Value,
                null);
            Assert.Equal(expectedChannelMessage.GlobalId, channelMessages.FirstOrDefault().GlobalId);
        }
        [Fact]
        public async Task GetDialogMessagesById()
        {
            var expectedDialogMessage = fillTestDbHelper.Messages.FirstOrDefault(message => message.DialogId != null);
            var dialogMessages = await loadMessagesService.GetMessagesByIdAsync(
                new List<Guid> { expectedDialogMessage.GlobalId },
                ConversationType.Dialog,
                expectedDialogMessage.DialogId.Value,
                null);
            Assert.Equal(expectedDialogMessage.GlobalId, dialogMessages.FirstOrDefault().GlobalId);
        }
        [Fact]
        public async Task GetChatMessagesById()
        {
            var expectedChatMessage = fillTestDbHelper.Messages.FirstOrDefault(message => message.ChatId != null);
            var chatMessages = await loadMessagesService.GetMessagesByIdAsync(
               new List<Guid> { expectedChatMessage.GlobalId },
               ConversationType.Chat,
               expectedChatMessage.ChatId.Value,
               null);
            Assert.Equal(expectedChatMessage.GlobalId, chatMessages.FirstOrDefault().GlobalId);
        }
        [Fact]
        public async Task GetMessagesByIdExceptions()
        {
            var expectedDialogMessage = fillTestDbHelper.Messages.FirstOrDefault(message => message.DialogId != null);
            await Assert.ThrowsAsync<GetMessagesException>(async () => await loadMessagesService.GetMessagesByIdAsync(
                new List<Guid> { expectedDialogMessage.GlobalId },
                ConversationType.Dialog,
                expectedDialogMessage.DialogId.Value,
                fillTestDbHelper.Users.FirstOrDefault(user => user.Id != expectedDialogMessage.SenderId && user.Id != expectedDialogMessage.ReceiverId).Id));
            Assert.Empty(await loadMessagesService.GetMessagesByIdAsync(
                new List<Guid> { Guid.NewGuid() },
                ConversationType.Chat,
                expectedDialogMessage.DialogId.Value,
                null));
        }
        [Fact]
        public async Task GetChatMessages()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var expectedMessages = fillTestDbHelper.Messages
                .OrderByDescending(message => message.SendingTime)
                .Where(message => message.ChatId == chat.Id)
                .ToList();
            var actualMessages = await loadMessagesService.GetMessagesAsync(chat.Id, ConversationType.Chat, true, null, null, 100);
            Assert.Equal(expectedMessages.FirstOrDefault().GlobalId, actualMessages.FirstOrDefault().GlobalId);
        }
        [Fact]
        public async Task GetDialogMessages()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            var expectedMessages = fillTestDbHelper.Messages
                .OrderByDescending(message => message.SendingTime)
                .Where(message => message.DialogId == dialog.Id)
                .ToList();
            var actualMessages = await loadMessagesService.GetMessagesAsync(dialog.Id, ConversationType.Dialog, true, null, null, 100);
            Assert.Equal(expectedMessages.FirstOrDefault().GlobalId, actualMessages.FirstOrDefault().GlobalId);
        }
        [Fact]
        public async Task GetChannelMessages()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var expectedMessages = fillTestDbHelper.Messages
                .OrderByDescending(message => message.SendingTime)
                .Where(message => message.ChannelId == channel.ChannelId)
                .ToList();
            var actualMessages = await loadMessagesService.GetMessagesAsync(channel.ChannelId, ConversationType.Channel, true, null, null, 100);
            Assert.Equal(expectedMessages.FirstOrDefault().GlobalId, actualMessages.FirstOrDefault().GlobalId);
        }
        [Fact]
        public async Task IsChannelMessageExists()
        {
            var message = fillTestDbHelper.Messages.FirstOrDefault(opt => opt.ChannelId != null);
            Assert.True(await loadMessagesService.IsChannelMessageExistsAsync(message.GlobalId, message.ChannelId.Value));
        }
        [Fact]
        public async Task IsDialogMessagesExists()
        {
            var message = fillTestDbHelper.Messages.FirstOrDefault(opt => opt.DialogId != null);
            Assert.True(await loadMessagesService.IsDialogMessageExistsAsync(message.GlobalId, message.SenderId.Value, message.ReceiverId.Value));
        }
        [Fact]
        public async Task IsChatMessagesExists()
        {
            var message = fillTestDbHelper.Messages.FirstOrDefault(opt => opt.ChatId != null);
            Assert.True(await loadMessagesService.IsChatMessageExistsAsync(message.GlobalId, message.ChatId.Value));
        }
        [Fact]
        public async Task IsReplyMessageExists()
        {
            var message = fillTestDbHelper.Messages.FirstOrDefault(opt => opt.Replyto != null);
            var messageDto = MessageConverter.GetMessageDto(message);
            var messageVm = MessageConverter.GetMessageVm(messageDto, null);
            Assert.True(await loadMessagesService.IsReplyMessageExistsAsync(messageVm));
        }
        [Fact]
        public async Task GetDialogUnreadedCount()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            var messages = fillTestDbHelper.Messages.Where(opt => opt.DialogId == dialog.Id && opt.SenderId != dialog.FirstUID);
            var unreadedCount = await loadMessagesService.GetDialogUnreadedMessagesCountAsync(new List<long> { dialog.Id }, dialog.FirstUID);
            Assert.Equal(messages.Count(), unreadedCount.FirstOrDefault().Value);
        }
        [Fact]
        public async Task GetChatUnreadedCount()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();            
            var chatUser = chat.ChatUsers.ElementAt(1);
            var messages = fillTestDbHelper.Messages
                .OrderBy(opt => opt.SendingTime)
                .Where(opt => opt.ChatId == chat.Id && opt.SenderId !=  chatUser.UserId)
                .ToList();
            var lastReaded = messages.FirstOrDefault(opt => opt.GlobalId == chatUser.LastReadedGlobalMessageId);
            messages = messages.Where(opt => opt.SendingTime > lastReaded.SendingTime).ToList();
            var unreadedCount = await loadMessagesService.GetChatUnreadedMessagesCountAsync(new List<long> { chat.Id }, chatUser.UserId);
            Assert.Equal(messages.Count(), unreadedCount.FirstOrDefault().Value);
        }
        [Fact]
        public async Task GetChannelUnreadedCount()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();            
            var channelUser = channel.ChannelUsers.FirstOrDefault();
            var messages = fillTestDbHelper.Messages
                .OrderBy(opt => opt.SendingTime)
                .Where(opt => opt.ChannelId == channel.ChannelId && opt.SenderId != channelUser.UserId)
                .ToList();
            var lastReaded = messages.FirstOrDefault(opt => opt.GlobalId == channelUser.LastReadedGlobalMessageId);
            messages = messages.Where(opt => opt.SendingTime > lastReaded.SendingTime).ToList();
            var unreadedCount = await loadMessagesService.GetChannelsUnreadedMessagesCountAsync(new List<long> { channel.ChannelId }, channelUser.UserId);
            Assert.Equal(messages.Count(), unreadedCount.FirstOrDefault().Value);
        }
        [Fact]
        public async Task GetLastValidChatMessage()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var expectedMessage = fillTestDbHelper.Messages
                .OrderByDescending(opt => opt.SendingTime)
                .FirstOrDefault(opt => !opt.Deleted && opt.ChatId == chat.Id);
            var actualMessage = await loadMessagesService.GetLastValidChatMessageAsync(chat.Id);
            Assert.Equal(expectedMessage.GlobalId, actualMessage.GlobalId);
        }
        [Fact]
        public async Task GetLastValidChannelMessage()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var expectedMessage = fillTestDbHelper.Messages
                .OrderByDescending(opt => opt.SendingTime)
                .FirstOrDefault(opt => !opt.Deleted && opt.ChannelId == channel.ChannelId);
            var actualMessage = await loadMessagesService.GetLastValidChannelMessageAsync(channel.ChannelId);
            Assert.Equal(expectedMessage.GlobalId, actualMessage.GlobalId);
        }
        [Fact]
        public async Task GetLastValidDialogMessage()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            var expectedMessage = fillTestDbHelper.Messages
                .OrderByDescending(opt => opt.SendingTime)
                .FirstOrDefault(opt => !opt.Deleted && opt.DialogId == dialog.Id);
            var actualMessage = await loadMessagesService.GetLastValidDialogMessageAsync(dialog.Id);
            Assert.Equal(expectedMessage.GlobalId, actualMessage.GlobalId);
        }
        [Fact]
        public async Task CanUserGetDialogMessage()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            var user = fillTestDbHelper.Users.FirstOrDefault(opt => opt.Id == dialog.FirstUID);
            var badUser = fillTestDbHelper.Users.FirstOrDefault(opt => opt.Id != dialog.FirstUID && opt.Id != dialog.SecondUID);
            Assert.True(await loadMessagesService.CanUserGetMessageAsync(ConversationType.Dialog, dialog.Id, user.Id));
            Assert.False(await loadMessagesService.CanUserGetMessageAsync(ConversationType.Dialog, dialog.Id, badUser.Id));
        }
        [Fact]
        public async Task CanUserGetChatMessage()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            Assert.True(await loadMessagesService.CanUserGetMessageAsync(ConversationType.Chat, chat.Id, chat.ChatUsers.FirstOrDefault().UserId));
            Assert.False(await loadMessagesService.CanUserGetMessageAsync(ConversationType.Chat, chat.Id, long.MaxValue));
        }
        [Fact]
        public async Task CanUserGetChannelMessage()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            Assert.True(await loadMessagesService.CanUserGetMessageAsync(ConversationType.Channel, channel.ChannelId, channel.ChannelUsers.FirstOrDefault().UserId));
            Assert.False(await loadMessagesService.CanUserGetMessageAsync(ConversationType.Channel, channel.ChannelId, long.MaxValue));
        }
    }
}