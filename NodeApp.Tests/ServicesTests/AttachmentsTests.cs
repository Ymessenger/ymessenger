using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.MessengerData.DataTransferObjects;
using ObjectsLibrary;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class AttachmentsTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IAttachmentsService attachmentsService;
        public AttachmentsTests()
        {
            TestsData testsData = TestsData.Create(nameof(AttachmentsTests));
            attachmentsService = testsData.AppServiceProvider.AttachmentsService;
            fillTestDbHelper = testsData.FillTestDbHelper;
        }
        [Fact]
        public async Task CheckEditedMessageFileAttachment()
        {
            var file = fillTestDbHelper.Files.FirstOrDefault();
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var message = new MessageVm
            {
                Attachments = new List<AttachmentVm>
                {
                    new AttachmentVm
                    {
                        Type = AttachmentType.File,
                        Payload = file.Id
                    }
                }
            };
            Assert.True(await attachmentsService.CheckEditedMessageAttachmentsAsync(message, user.Id));
        }
        [Fact]
        public async Task CheckEditedMessageForwardedAttachment()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var forwardedMessage = MessageConverter.GetMessageDto(fillTestDbHelper.Messages.FirstOrDefault(opt => opt.DialogId == null));
            var message = new MessageVm
            {
                Attachments = new List<AttachmentVm>
                {
                    new AttachmentVm
                    {
                        Type = AttachmentType.ForwardedMessages,
                        Payload = new ForwardedMessagesInfo(new List<Guid>{ forwardedMessage.GlobalId }, forwardedMessage.ConversationId, forwardedMessage.ConversationType)
                    }
                }
            };
            Assert.True(await attachmentsService.CheckEditedMessageAttachmentsAsync(message, user.Id));
        }
        [Fact]
        public async Task CheckEditedMessagePollAttachment()
        {
            var existingMessage = MessageConverter.GetMessageDto(fillTestDbHelper.Messages.FirstOrDefault(opt => opt.DialogId == null));
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var poll = new PollVm
            {
                PollOptions = new List<PollOptionVm>
                {
                    new PollOptionVm
                    {
                        Description = "desc",
                        OptionId = 1
                    },
                    new PollOptionVm
                    {
                        Description = "desc 2",
                        OptionId = 2
                    }
                }
            };
            var message = new MessageVm
            {
                ConversationId = existingMessage.ConversationId,
                ConversationType = existingMessage.ConversationType,
                Attachments = new List<AttachmentVm>
                {
                    new AttachmentVm
                    {
                        Payload = poll,
                        Type = AttachmentType.Poll
                    }
                }
            };
            Assert.True(await attachmentsService.CheckEditedMessageAttachmentsAsync(message, user.Id));
        }
        [Fact]
        public async Task LoadFileInfo()
        {
            var file = fillTestDbHelper.Files.FirstOrDefault();
            Assert.NotNull(await attachmentsService.LoadFileInfoAsync(file.Id));
        }
        [Fact]
        public async Task LoadForwardedMessages()
        {
            var message = fillTestDbHelper.Messages.FirstOrDefault();
            var messageDto = MessageConverter.GetMessageDto(message);
            var actualMessage = (await attachmentsService.LoadForwardedMessagesAsync(
                new ForwardedMessagesInfo(new List<Guid> { messageDto.GlobalId }, messageDto.ConversationId, messageDto.ConversationType),
                messageDto.SenderId)).FirstOrDefault();
            Assert.True(messageDto.GlobalId == actualMessage.GlobalId);
        }
        [Fact]
        public async Task SaveMessageAttachments()
        {
            var message = fillTestDbHelper.Messages.FirstOrDefault();
            var messageDto = MessageConverter.GetMessageDto(message);
            List<AttachmentDto> attachments = new List<AttachmentDto>
            {
                new AttachmentDto
                {
                    Payload = ObjectSerializer.ObjectToJson(
                        new ForwardedMessagesInfo(new List<Guid>{ messageDto.GlobalId }, messageDto.ConversationId, messageDto.ConversationType)),
                    Type = AttachmentType.ForwardedMessages
                }
            };
            var actualAttachment = await attachmentsService.SaveMessageAttachmentsAsync(attachments, new List<long> { message.Id });
            Assert.Equal(attachments, actualAttachment);
        }
        [Fact]
        public async Task ThrowIfAttachmentsInvalid()
        {           
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var secondUser = fillTestDbHelper.Users.LastOrDefault();
            var file = fillTestDbHelper.Files.FirstOrDefault();
            var message = MessageConverter.GetMessageDto(fillTestDbHelper.Messages.FirstOrDefault());
            var messageVm = new MessageVm
            {
                SenderId = user.Id,
                Attachments = new List<AttachmentVm>
                {
                    new AttachmentVm
                    {
                        Payload = file.Id,
                        Type = AttachmentType.File
                    },
                    new AttachmentVm
                    {
                        Payload = new ForwardedMessagesInfo(new List<Guid>{ message.GlobalId }, message.ConversationId, message.ConversationType),
                        Type = AttachmentType.ForwardedMessages
                    },
                    new AttachmentVm
                    {
                        Payload = new PollVm
                        {
                            ConversationId = message.ConversationId,
                            ConversationType = message.ConversationType,
                            PollOptions = new List<PollOptionVm>
                            {
                                new PollOptionVm
                                {
                                    Description = "Desc",
                                    OptionId = 1
                                }
                            },
                            Title = "Title"
                        },
                        Type = AttachmentType.Poll
                    }
                }
            };
            await attachmentsService.ThrowIfAttachmentsInvalidAsync(messageVm, false);
            var invalidKeyAttachment = new MessageVm
            {
                SenderId = user.Id,
                ReceiverId = secondUser.Id,
                Attachments = new List<AttachmentVm>
                {
                    new AttachmentVm
                    {
                        Type = AttachmentType.KeyMessage,
                        Payload = new object()
                    }
                }
            };
            var invalidFileMessage = new MessageVm
            {
                SenderId = user.Id,
                Attachments = new List<AttachmentVm>
               {
                   new AttachmentVm
                   {
                        Payload = "Non existing file ID",
                        Type = AttachmentType.File,
                   }
               }
            };
            var invalidForwardedMessage = new MessageVm
            {
                SenderId = user.Id,
                Attachments = new List<AttachmentVm>
                {
                    new AttachmentVm
                    {
                        Payload = RandomExtensions.NextString(3),
                        Type = AttachmentType.ForwardedMessages
                    }
                }
            };
            var invalidPollMessage = new MessageVm
            {
                SenderId = user.Id,
                Attachments = new List<AttachmentVm>
                {
                    new AttachmentVm
                    {
                        Payload = new PollVm
                        {
                            Title = "t"
                        }
                    }
                }
            };
            await Assert.ThrowsAnyAsync<InvalidAttachmentsException>(async () => await attachmentsService.ThrowIfAttachmentsInvalidAsync(invalidFileMessage, false));
            await Assert.ThrowsAnyAsync<InvalidAttachmentsException>(async () => await attachmentsService.ThrowIfAttachmentsInvalidAsync(invalidForwardedMessage, false));
            await Assert.ThrowsAnyAsync<InvalidAttachmentsException>(async () => await attachmentsService.ThrowIfAttachmentsInvalidAsync(invalidPollMessage, false));
            await Assert.ThrowsAnyAsync<InvalidAttachmentsException>(async () => await attachmentsService.ThrowIfAttachmentsInvalidAsync(invalidKeyAttachment, false));
        }        
    }
}
