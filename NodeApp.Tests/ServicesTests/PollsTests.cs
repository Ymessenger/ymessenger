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
using NodeApp.MessengerData.DataTransferObjects;
using ObjectsLibrary.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class PollsTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IPollsService pollsService;
        public PollsTests()
        {
            TestsData testsData = TestsData.Create(nameof(PollsTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            pollsService = testsData.AppServiceProvider.PollsService;
        }
        [Fact]
        public async Task SavePoll()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var chatUser = chat.ChatUsers.FirstOrDefault();
            var expectedPoll = new PollDto 
            { 
                ConversationId = chat.Id,
                ConversationType = ConversationType.Chat,
                Title = "Poll",
                CreatorId = chatUser.UserId,
                Options = new List<PollOptionDto>
                {
                    new PollOptionDto
                    {
                        Description = "desc",
                        OptionId = 1
                    },
                    new PollOptionDto
                    {
                        Description = "desc 2",
                        OptionId = 2
                    }
                }
            };
            var actualPoll = await pollsService.SavePollAsync(expectedPoll);
            Assert.True(actualPoll.Title == expectedPoll.Title 
                && expectedPoll.ConversationId == actualPoll.ConversationId
                && expectedPoll.ConversationType == actualPoll.ConversationType
                && expectedPoll.CreatorId == actualPoll.CreatorId);
        }
        [Fact]
        public async Task GetPoll()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var chatUser = chat.ChatUsers.FirstOrDefault();
            var poll = new PollDto
            {
                ConversationId = chat.Id,
                ConversationType = ConversationType.Chat,
                Title = "Poll",
                CreatorId = chatUser.UserId,
                Options = new List<PollOptionDto>
                {
                    new PollOptionDto
                    {
                        Description = "desc",
                        OptionId = 1
                    },
                    new PollOptionDto
                    {
                        Description = "desc 2",
                        OptionId = 2
                    }
                }
            };
            var expectedPoll = await pollsService.SavePollAsync(poll);
            var actualPoll = await pollsService.GetPollAsync(poll.PollId, poll.ConversationId, poll.ConversationType);
            Assert.True(expectedPoll.ConversationId == actualPoll.ConversationId
                && expectedPoll.ConversationType == actualPoll.ConversationType
                && expectedPoll.Title == actualPoll.Title
                && expectedPoll.CreatorId == actualPoll.CreatorId);
        }
        [Fact]
        public async Task VotePoll()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var chatUser = chat.ChatUsers.LastOrDefault();
            var poll = new PollDto
            {
                ConversationId = chat.Id,
                ConversationType = ConversationType.Chat,
                Title = "Poll",
                CreatorId = chatUser.UserId,
                Options = new List<PollOptionDto>
                {
                    new PollOptionDto
                    {
                        Description = "desc",
                        OptionId = 1
                    },
                    new PollOptionDto
                    {
                        Description = "desc 2",
                        OptionId = 2
                    }
                }
            };
            poll = await pollsService.SavePollAsync(poll);
            var votedPoll = await pollsService.VotePollAsync(poll.PollId, poll.ConversationId, poll.ConversationType, new List<byte> { 1 }, chatUser.UserId);
            Assert.NotNull(votedPoll.Options.FirstOrDefault(opt => opt.OptionId == 1).Votes.FirstOrDefault(opt => opt.UserId == chatUser.UserId));
        }
        [Fact]
        public async Task GetPollVotedUsers()
        {
            var chat = fillTestDbHelper.Chats.FirstOrDefault();
            var chatUser = chat.ChatUsers.FirstOrDefault();
            var poll = new PollDto
            {
                ConversationId = chat.Id,
                ConversationType = ConversationType.Chat,
                Title = "Poll",
                CreatorId = chatUser.UserId,
                Options = new List<PollOptionDto>
                {
                    new PollOptionDto
                    {
                        Description = "desc",
                        OptionId = 1
                    },
                    new PollOptionDto
                    {
                        Description = "desc 2",
                        OptionId = 2
                    }
                }
            };
            poll = await pollsService.SavePollAsync(poll);
            poll = await pollsService.VotePollAsync(poll.PollId, poll.ConversationId, poll.ConversationType, new List<byte> { 1 }, chatUser.UserId);
            var votedUsers = await pollsService.GetPollVotedUsersAsync(poll.PollId, poll.ConversationId, poll.ConversationType, 1, chatUser.UserId);
            Assert.True(votedUsers.Count == 1 && votedUsers.Any(opt => opt.FirstValue.Id == chatUser.UserId));           
        }
    }
}