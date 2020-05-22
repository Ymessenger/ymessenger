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
using Microsoft.EntityFrameworkCore;
using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class PollsService : IPollsService
    {
        private readonly IConversationsService conversationsService;
        private readonly IConnectionsService connectionsService;
        private readonly INodeRequestSender nodeRequestSender;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public PollsService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            conversationsService = appServiceProvider.ConversationsService;
            this.contextFactory = contextFactory;
            nodeRequestSender = appServiceProvider.NodeRequestSender;
            connectionsService = appServiceProvider.ConnectionsService;
        }
        public async Task<PollDto> GetPollAsync(Guid pollId, long conversationId, ConversationType conversationType)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var poll = await context.Polls
                .Include(opt => opt.Options)
                .ThenInclude(opt => opt.PollOptionVotes)
                .FirstOrDefaultAsync(opt =>
                    opt.PollId == pollId
                    && opt.ConvertsationId == conversationId
                    && opt.ConversationType == conversationType)
                .ConfigureAwait(false);
                if (poll == null)
                {
                    var nodeId = await conversationsService.GetConversationNodeIdAsync(conversationType, conversationId).ConfigureAwait(false);
                    var connection = connectionsService.GetNodeConnection(nodeId);
                    if (connection != null)
                    {
                        var loadedPoll = await nodeRequestSender.GetPollInformationAsync(conversationId, conversationType, pollId, connection).ConfigureAwait(false);
                        loadedPoll = await SavePollAsync(loadedPoll).ConfigureAwait(false);
                        return loadedPoll;
                    }
                }
                return PollConverter.GetPollDto(poll);
            }
        }
        public async Task<PollDto> VotePollAsync(Guid pollId, long conversationId, ConversationType conversationType, List<PollVoteVm> options, long votingUserId)
        {
            if (!await conversationsService.IsUserInConversationAsync(conversationType, conversationId, votingUserId).ConfigureAwait(false))
            {
                throw new PermissionDeniedException("User is not in conversation.");
            }
            using (MessengerDbContext context = contextFactory.Create())
            {
                var poll = await context.Polls
                .Include(opt => opt.Options)
                .ThenInclude(opt => opt.PollOptionVotes)
                .FirstOrDefaultAsync(opt =>
                    opt.PollId == pollId
                    && opt.ConvertsationId == conversationId
                    && opt.ConversationType == conversationType)
                .ConfigureAwait(false);
                if (poll == null)
                {
                    throw new ObjectDoesNotExistsException($"Poll not found (Poll Id: {pollId}, {conversationType.ToString()}Id: {conversationId}).");
                }
                if (options == null || !options.Any())
                {
                    foreach (var option in poll.Options)
                    {
                        option.PollOptionVotes = option.PollOptionVotes.Where(vote => vote.UserId != votingUserId).ToList();
                    }
                }
                List<PollOptionVote> newVotes = new List<PollOptionVote>();
                List<PollOptionVote> removedVotes = new List<PollOptionVote>();
                foreach (var voteOption in options)
                {
                    var option = poll.Options.FirstOrDefault(opt => opt.OptionId == voteOption.OptionId);
                    if (option != null && voteOption.Sign != null && voteOption.Sign.Data != null)
                    {
                        if (!option.PollOptionVotes.Any(vote => vote.UserId == votingUserId))
                        {
                            if (poll.SignRequired)
                            {
                                if (voteOption.Sign == null || voteOption.Sign.Data == null || !voteOption.Sign.Data.Any())
                                    throw new InvalidSignException($"Vote №{voteOption.OptionId} is not signed.");
                                var signKey = await context.Keys
                                    .FirstOrDefaultAsync(key => key.KeyId == voteOption.Sign.KeyId && key.Type == KeyType.SignAsymKey)
                                    .ConfigureAwait(false);
                                if (signKey == null)
                                {
                                    throw new ObjectDoesNotExistsException($"Key not found (Key Id: {voteOption.Sign.KeyId}.");
                                }                               
                            }
                            newVotes.Add(new PollOptionVote
                            {
                                ConversationId = conversationId,
                                ConversationType = conversationType,
                                OptionId = voteOption.OptionId,
                                UserId = votingUserId,
                                PollId = pollId,
                                Sign = voteOption.Sign?.Data,
                                SignKeyId = voteOption.Sign?.KeyId
                            });
                        }
                        else
                        {
                            removedVotes.Add(option.PollOptionVotes
                                .FirstOrDefault(opt => opt.UserId == votingUserId && opt.OptionId == voteOption.OptionId));
                        }
                    }
                    else
                    {
                        if (!option.PollOptionVotes.Any(vote => vote.UserId == votingUserId))
                        {
                            newVotes.Add(new PollOptionVote
                            {
                                ConversationId = conversationId,
                                ConversationType = conversationType,
                                OptionId = voteOption.OptionId,
                                UserId = votingUserId,
                                PollId = pollId
                            });
                        }
                        else
                        {
                            removedVotes.Add(option.PollOptionVotes
                               .FirstOrDefault(opt => opt.UserId == votingUserId && opt.OptionId == voteOption.OptionId));
                        }
                    }
                    foreach (var vote in newVotes)
                    {
                        option.PollOptionVotes.Add(vote);
                    }
                    foreach (var vote in removedVotes)
                    {
                        option.PollOptionVotes.Remove(vote);
                    }
                }
                if (!poll.MultipleSelection && poll.Options.Count(opt => opt.PollOptionVotes.Any(vote => vote.UserId == votingUserId)) > 1)
                {
                    throw new InvalidOperationException("Multiple voting is not allowed in current poll.");
                }
                context.Polls.Update(poll);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return PollConverter.GetPollDto(poll);
            }
        }
        public async Task<PollDto> VotePollAsync(Guid pollId, long conversationId, ConversationType conversationType, List<byte> optionsId, long votingUserId)
        {
            if (!await conversationsService.IsUserInConversationAsync(conversationType, conversationId, votingUserId).ConfigureAwait(false))
            {
                throw new PermissionDeniedException("User is not in conversation.");
            }
            using (MessengerDbContext context = contextFactory.Create())
            {
                var poll = await context.Polls
                .Include(opt => opt.Options)
                .ThenInclude(opt => opt.PollOptionVotes)
                .FirstOrDefaultAsync(opt =>
                    opt.PollId == pollId
                    && opt.ConvertsationId == conversationId
                    && opt.ConversationType == conversationType
                    && !opt.SignRequired)
                .ConfigureAwait(false);
                if (poll == null)
                {
                    throw new ObjectDoesNotExistsException($"Poll not found (Poll Id: {pollId}, {conversationType.ToString()}Id: {conversationId}).");
                }
                if (optionsId == null || !optionsId.Any())
                {
                    foreach (var option in poll.Options)
                    {
                        option.PollOptionVotes = option.PollOptionVotes.Where(vote => vote.UserId != votingUserId).ToList();
                    }
                }
                List<PollOptionVote> newVotes = new List<PollOptionVote>();
                List<PollOptionVote> removedVotes = new List<PollOptionVote>();
                foreach (var optionId in optionsId)
                {
                    var option = poll.Options.FirstOrDefault(opt => opt.OptionId == optionId);
                    if (option != null)
                    {
                        if (!option.PollOptionVotes.Any(vote => vote.UserId == votingUserId))
                        {
                            newVotes.Add(new PollOptionVote
                            {
                                ConversationId = conversationId,
                                ConversationType = conversationType,
                                OptionId = optionId,
                                UserId = votingUserId,
                                PollId = pollId
                            });
                        }
                        else
                        {
                            removedVotes.Add(option.PollOptionVotes
                                .FirstOrDefault(opt => opt.UserId == votingUserId && opt.OptionId == optionId));
                        }
                    }
                    foreach (var vote in newVotes)
                    {
                        option.PollOptionVotes.Add(vote);
                    }
                    foreach (var vote in removedVotes)
                    {
                        option.PollOptionVotes.Remove(vote);
                    }
                }
                if (!poll.MultipleSelection && poll.Options.Count(opt => opt.PollOptionVotes.Any(vote => vote.UserId == votingUserId)) > 1)
                {
                    throw new InvalidOperationException("Multiple voting is not allowed in current poll.");
                }
                context.Polls.Update(poll);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return PollConverter.GetPollDto(poll);
            }
        }

        public async Task<PollDto> SavePollAsync(PollDto pollDto)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                Poll poll = PollConverter.GetPoll(pollDto);
                bool isPollExists = await context.Polls.AnyAsync(opt => opt.PollId == poll.PollId
                    && opt.ConvertsationId == poll.ConvertsationId && opt.ConversationType == poll.ConversationType)
                    .ConfigureAwait(false);
                if (!isPollExists)
                {
                    await context.Polls.AddAsync(poll).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
                return PollConverter.GetPollDto(poll);
            }
        }

        public async Task<List<ValuePair<UserDto, byte[]>>> GetPollVotedUsersAsync(Guid pollId, long conversationId, ConversationType conversationType, byte optionId, long? requestorId, int limit = 30, long navigationUserId = 0)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var poll = await context.Polls.FirstOrDefaultAsync(opt =>
                    opt.PollId == pollId
                    && opt.ConversationType == conversationType
                    && opt.ConvertsationId == conversationId)
                    .ConfigureAwait(false);
                if (requestorId.GetValueOrDefault() != poll.CreatorId && !poll.ResultsVisibility
                    && !await conversationsService.IsUserInConversationAsync(conversationType, conversationId, requestorId.GetValueOrDefault()).ConfigureAwait(false))
                {
                    throw new PermissionDeniedException();
                }
                List<PollOptionVote> votes = await context.PollsOptionsVotes                    
                    .Include(opt => opt.User)
                        .Include(opt => opt.User.Phones)
                        .Include(opt => opt.User.Emails)
                        .Include(opt => opt.User.BlackList)
                    .Where(opt => opt.ConversationId == conversationId
                        && opt.ConversationType == conversationType
                        && opt.PollId == pollId
                        && opt.OptionId == optionId
                        && opt.UserId > navigationUserId)
                    .OrderBy(opt => opt.UserId)
                    .Take(limit)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return votes.Select(vote => new ValuePair<UserDto, byte[]>(UserConverter.GetUserDto(vote.User), vote.Sign)).ToList();
            }
        }
    }
}