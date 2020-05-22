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
using ObjectsLibrary.Enums;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class ConversationsTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IConversationsService conversationsService;        
        public ConversationsTests()
        {
            TestsData testsData = TestsData.Create(nameof(ConversationsTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            conversationsService = testsData.AppServiceProvider.ConversationsService;            
        }
        [Fact]
        public async Task GetConversationsNodesIds()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var chatNodesIds = await conversationsService.GetConversationNodesIdsAsync(ConversationType.Chat, chat.Id);
            Assert.Equal(chat.NodesId, chatNodesIds);
        }
        [Fact]
        public async Task IsUserInConversation()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();            
            Assert.True(await conversationsService.IsUserInConversationAsync(ConversationType.Chat, chat.Id, chat.ChatUsers.FirstOrDefault().UserId));
            Assert.False(await conversationsService.IsUserInConversationAsync(ConversationType.Chat, chat.Id, long.MaxValue));
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            Assert.True(await conversationsService.IsUserInConversationAsync(ConversationType.Channel, channel.ChannelId, channel.ChannelUsers.FirstOrDefault().UserId));
            Assert.False(await conversationsService.IsUserInConversationAsync(ConversationType.Channel, channel.ChannelId, long.MaxValue));
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            Assert.True(await conversationsService.IsUserInConversationAsync(ConversationType.Dialog,  dialog.Id, dialog.FirstUID));
            Assert.False(await conversationsService.IsUserInConversationAsync(ConversationType.Dialog, dialog.Id, long.MaxValue));
        }
        [Fact]
        public async Task MuteConversation()
        {
            var expectedChat = fillTestDbHelper.Chats.FirstOrDefault();
            await conversationsService.MuteConversationAsync(ConversationType.Chat, expectedChat.Id, expectedChat.ChatUsers.FirstOrDefault().UserId);
            var actualChat = fillTestDbHelper.Chats.FirstOrDefault(opt => opt.Id == expectedChat.Id);
            Assert.True(actualChat.ChatUsers.FirstOrDefault().IsMuted);
            var expectedChannel = fillTestDbHelper.Channels.FirstOrDefault();
            await conversationsService.MuteConversationAsync(ConversationType.Channel, expectedChannel.ChannelId, expectedChannel.ChannelUsers.FirstOrDefault().UserId);
            var actualChannel = fillTestDbHelper.Channels.FirstOrDefault(opt => opt.ChannelId == expectedChannel.ChannelId);
            Assert.True(actualChannel.ChannelUsers.FirstOrDefault().IsMuted);
            var expectedDialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            await conversationsService.MuteConversationAsync(ConversationType.Dialog, expectedDialog.Id, expectedDialog.FirstUID);
            var actualDialog = fillTestDbHelper.Dialogs.FirstOrDefault(opt => opt.Id == expectedDialog.Id);
            Assert.True(actualDialog.IsMuted);
        }
        [Fact]
        public async Task GetConversationNodeId()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var chatCreatorId = chat.ChatUsers.FirstOrDefault(opt => opt.UserRole == UserRole.Creator).UserId;
            var chatCreator = fillTestDbHelper.Users.FirstOrDefault(opt => opt.Id == chatCreatorId);
            var actualChatNodeId = await conversationsService.GetConversationNodeIdAsync(ConversationType.Chat, chat.Id);
            Assert.Equal(chatCreator.NodeId, actualChatNodeId);
            var channel = fillTestDbHelper.Channels.FirstOrDefault();
            var channelCreatorId = channel.ChannelUsers.FirstOrDefault(opt => opt.ChannelUserRole == ChannelUserRole.Creator).UserId;
            var channelCreator = fillTestDbHelper.Users.FirstOrDefault(opt => opt.Id == channelCreatorId);
            var actualChannelNodeId = await conversationsService.GetConversationNodeIdAsync(ConversationType.Channel, channel.ChannelId);
            Assert.Equal(channelCreator.NodeId, actualChannelNodeId);
        }       
    }
}
