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
using ObjectsLibrary.Enums;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class UpdateMessagesTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IUpdateMessagesService updateMessagesService;
        public UpdateMessagesTests()
        {
            var testsData = TestsData.Create(nameof(UpdateMessagesTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            updateMessagesService = testsData.AppServiceProvider.UpdateMessagesService;
        }
        [Fact]
        public async Task EditMessage()
        {
            var message = fillTestDbHelper.Messages.FirstOrDefault();            
            var expectedMessage = MessageConverter.GetMessageDto(message);
            expectedMessage.Text = "Edited message text";
            var actualMessage = await updateMessagesService.EditMessageAsync(expectedMessage, expectedMessage.SenderId.Value);
            Assert.Equal(expectedMessage.Text, actualMessage.Text);
        }
        [Fact]
        public async Task SetDialogMessagesRead()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            var messages = fillTestDbHelper.Messages.Where(opt => opt.DialogId == dialog.Id).ToList();
            var actualMessages = await updateMessagesService.SetMessagesReadAsync(
                messages.Select(opt => opt.GlobalId),
                dialog.Id,
                ConversationType.Dialog,
                dialog.SecondUID);
            Assert.Equal(messages.Select(opt => opt.GlobalId), actualMessages.Select(opt => opt.GlobalId));            
        }
        [Fact]
        public async Task SetChatMessagesRead()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var reader = chat.ChatUsers.FirstOrDefault();
            var messages = fillTestDbHelper.Messages.Where(opt => opt.ChatId == chat.Id && opt.SenderId != reader.UserId).ToList();
            var actualMessages = await updateMessagesService.SetMessagesReadAsync(
                messages.Select(opt => opt.GlobalId), 
                chat.Id, 
                ConversationType.Chat, 
                reader.UserId);
            Assert.Equal(messages.Select(opt => opt.GlobalId), actualMessages.Select(opt => opt.GlobalId));
        }
        [Fact]
        public async Task SetChannelMessagesRead()
        {
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var reader = channel.ChannelUsers.FirstOrDefault();
            var messages = fillTestDbHelper.Messages.Where(opt => opt.ChannelId == channel.ChannelId && opt.SenderId != reader.UserId).ToList();
            var actualMessages = await updateMessagesService.SetMessagesReadAsync(
                messages.Select(opt => opt.GlobalId),
                channel.ChannelId,
                ConversationType.Channel, 
                reader.UserId);
            Assert.Equal(messages.Select(opt => opt.GlobalId), actualMessages.Select(opt => opt.GlobalId));
        }
        [Fact]
        public async Task DialogMessagesRead()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault(opt => opt.SecondUID != opt.FirstUID);
            var messages = fillTestDbHelper.Messages.Where(opt => opt.DialogId == dialog.Id && opt.SenderId != dialog.SecondUID).ToList();
            var actualMessages = await updateMessagesService.DialogMessagesReadAsync(messages.Select(opt => opt.GlobalId), dialog.Id, dialog.SecondUID);
            Assert.Equal(messages.Select(opt => opt.GlobalId), actualMessages.Select(opt => opt.GlobalId));
        }
        [Fact]
        public async Task SetDialogMessagesReadByUsersIds()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault(opt => opt.FirstUID != opt.SecondUID);
            var messages = fillTestDbHelper.Messages.Where(opt => opt.DialogId == dialog.Id && opt.SenderId != dialog.SecondUID).ToList();
            var actualMessages = await updateMessagesService.SetDialogMessagesReadByUsersIdAsync(messages.Select(opt => opt.GlobalId), dialog.FirstUID, dialog.SecondUID);
            Assert.Equal(messages.Select(opt => opt.GlobalId), actualMessages.Select(opt => opt.GlobalId));
        }
    }
}