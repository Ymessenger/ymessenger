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
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.Converters
{
    public static class PollConverter
    {
        public static PollDto GetPollDto(Poll poll)
        {
            if (poll == null)
            {
                return null;
            }

            return new PollDto
            {
                PollId = poll.PollId,
                Title = poll.Title,
                ConversationId = poll.ConvertsationId,
                ConversationType = poll.ConversationType,
                MultipleSelection = poll.MultipleSelection,
                ResultsVisibility = poll.ResultsVisibility,
                CreatorId = poll.CreatorId,
                Options = poll.Options.Select(opt => new PollOptionDto
                {
                    OptionId = opt.OptionId,
                    Description = opt.Description,
                    Votes = opt.PollOptionVotes?.Select(vote => new PollVoteDto { UserId = vote.UserId, Sign = vote.Sign }).ToList()
                }).ToList(),
                SignRequired = poll.SignRequired
            };
        }

        public static Poll GetPoll(PollDto poll)
        {
            if (poll == null)
            {
                return null;
            }

            var result = new Poll
            {
                PollId = poll.PollId,
                Title = poll.Title,
                ConvertsationId = poll.ConversationId,
                ConversationType = poll.ConversationType,
                MultipleSelection = poll.MultipleSelection,
                ResultsVisibility = poll.ResultsVisibility,
                CreatorId = poll.CreatorId,
                SignRequired = poll.SignRequired
            };
            var pollOptions = new List<PollOption>();
            foreach (var option in poll.Options)
            {
                pollOptions.Add(new PollOption
                {
                    OptionId = option.OptionId,
                    Description = option.Description,
                    PollOptionVotes = option.Votes?.Select(vote => new PollOptionVote
                    {
                        ConversationId = poll.ConversationId,
                        ConversationType = poll.ConversationType,
                        OptionId = option.OptionId,
                        PollId = poll.PollId,
                        UserId = vote.UserId,
                        Sign = vote.Sign,
                        SignKeyId = vote.SignKeyId
                    }).ToList()
                });
            }
            result.Options = pollOptions;
            return result;
        }

        public static PollVm GetPollVm(PollDto poll, long userId)
        {
            if (poll == null)
            {
                return null;
            }

            PollVm result = new PollVm
            {
                ConversationType = poll.ConversationType,
                ConversationId = poll.ConversationId,
                MultipleSelection = poll.MultipleSelection,
                PollId = poll.PollId,
                ResultsVisibility = poll.ResultsVisibility,
                Title = poll.Title,
                SignRequired = poll.SignRequired
            };
            if (poll.CreatorId == userId || poll.ResultsVisibility)
            {
                result.PollOptions = GetPollOptionsVm(poll.Options, userId, true);
            }
            else if (poll.CreatorId != userId && !poll.ResultsVisibility)
            {
                result.PollOptions = GetPollOptionsVm(poll.Options, userId, false);
            }
            result.Voted = result.PollOptions?.Any(opt => opt.Voted.GetValueOrDefault());
            return result;
        }

        public static List<PollOptionVm> GetPollOptionsVm(List<PollOptionDto> pollOptions, long userId, bool mapVotedUsers)
        {
            var result = new List<PollOptionVm>();
            foreach (var option in pollOptions)
            {
                PollOptionVm optionVm = new PollOptionVm
                {
                    Description = option.Description,
                    OptionId = option.OptionId
                };
                if (mapVotedUsers)
                {
                    optionVm.VotedUsersCount = option.Votes.Count;
                }
                optionVm.Voted = option.Votes.Any(vote => vote.UserId == userId);
                result.Add(optionVm);
            }
            return result;
        }
        public static PollDto GetPollDto(PollVm poll, long userId)
        {
            if (poll == null)
            {
                return null;
            }

            return new PollDto
            {
                ConversationType = poll.ConversationType.GetValueOrDefault(),
                ConversationId = poll.ConversationId.GetValueOrDefault(),
                CreatorId = userId,
                MultipleSelection = poll.MultipleSelection.GetValueOrDefault(false),
                ResultsVisibility = poll.ResultsVisibility.GetValueOrDefault(true),
                PollId = poll.PollId.GetValueOrDefault(),
                Title = poll.Title,
                Options = poll.PollOptions.Select(opt => new PollOptionDto
                {
                    Description = opt.Description,
                    OptionId = opt.OptionId
                }).ToList(),
                SignRequired = poll.SignRequired.GetValueOrDefault()
            };
        }
        public static async Task<PollVm> InitPollConversationAsync(PollVm poll, MessageVm message)
        {
            poll.ConversationType = message.ConversationType;
            if (message.ConversationType == ConversationType.Dialog)
            {
                poll.ConversationId = (await AppServiceProvider.Instance.LoadDialogsService.GetDialogsIdByUsersIdPairAsync(
                    message.SenderId.GetValueOrDefault(),
                    message.ReceiverId.GetValueOrDefault()).ConfigureAwait(false)).FirstOrDefault();
            }
            else
            {
                poll.ConversationId = message.ConversationId;
            }
            return poll;
        }
    }
}