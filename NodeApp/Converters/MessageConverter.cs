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
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.Converters
{
    public static class MessageConverter
    {
        public static MessageVm GetMessageVm(MessageDto message, long? requestorId)
        {
            if (message == null)
            {
                return null;
            }

            MessageVm result = new MessageVm
            {
                SendingTime = message.SendingTime,
                SenderId = message.SenderId,
                Text = message.Text,
                Read = message.Read,
                ReplyTo = message.Replyto,
                GlobalId = message.GlobalId,
                UpdatedAt = message.UpdatedAt,
                ReceiverId = message.ReceiverId,
                Lifetime = message.Lifetime,
                NodesId = message.NodesIds
            };
            result.ConversationId = message.ConversationId;
            result.ConversationType = message.ConversationType;
            if (message.Attachments != null)
            {
                result.Attachments = AttachmentConverter.GetAttachmentsVm(message.Attachments, requestorId)?.ToList();
            }
            return result;
        }

        public static List<MessageVm> GetMessagesVm(IEnumerable<MessageDto> messages, long? requestorId)
        {
            if (messages == null)
            {
                return new List<MessageVm>();
            }

            List<MessageVm> result = new List<MessageVm>();
            foreach (var message in messages)
            {
                result.Add(GetMessageVm(message, requestorId));
            }
            return result;
        }
        public static MessageDto GetMessage(MessageVm message)
        {
            MessageDto result = new MessageDto
            {
                SendingTime = message.SendingTime.GetValueOrDefault(),
                SenderId = message.SenderId ?? 0,
                Text = message.Text,
                Read = message.Read ?? false,
                GlobalId = message.GlobalId ?? Guid.Empty,
                Replyto = message.ReplyTo,
                ReceiverId = message.ReceiverId,
                Lifetime = message.Lifetime,
                NodesIds = message.NodesId?.ToList()
            };
            result.ConversationId = message.ConversationId.GetValueOrDefault();
            result.ConversationType = message.ConversationType;
            if (message.Attachments != null)
            {
                result.Attachments = AttachmentConverter.GetAttachments(message.Attachments)?.ToList();
            }
            return result;
        }

        public static Message GetMessage(MessageDto message)
        {
            Message result = new Message
            {
                Attachments = AttachmentConverter.GetAttachments(message.Attachments, null)?.ToList(),
                GlobalId = message.GlobalId,
                SenderId = message.SenderId,
                Deleted = message.Deleted,
                ReceiverId = message.ReceiverId,
                Read = message.Read,
                Text = message.Text,
                Replyto = message.Replyto,
                SendingTime = message.SendingTime,
                UpdatedAt = message.UpdatedAt,
                NodesIds = message.NodesIds?.ToArray()
            };
            if (message.Lifetime != null)
            {
                result.ExpiredAt = message.SendingTime + message.Lifetime;
            }
            switch (message.ConversationType)
            {
                case ConversationType.Chat:
                    result.ChatId = message.ConversationId;
                    break;
                case ConversationType.Channel:
                    result.ChannelId = message.ConversationId;
                    break;
                case ConversationType.Dialog:
                    result.DialogId = message.ConversationId;
                    break;
                case ConversationType.Unknown:
                    {
                        result.ChannelId = null;
                        result.DialogId = null;
                        result.ChatId = null;
                    }
                    break;
            }
            return result;
        }

        public static List<Message> GetMessages(IEnumerable<MessageDto> messages)
        {
            return messages?.Select(GetMessage).ToList();
        }

        public static MessageDto GetMessageDto(Message message)
        {
            MessageDto messageDto = new MessageDto
            {
                Attachments = AttachmentConverter.GetAttachmentsDto(message.Attachments),
                GlobalId = message.GlobalId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Read = message.Read,
                Replyto = message.Replyto,
                SendingTime = message.SendingTime,
                Text = message.Text,
                Deleted = message.Deleted,
                UpdatedAt = message.UpdatedAt,
                Lifetime = message.ExpiredAt - message.SendingTime,
                NodesIds = message.NodesIds?.ToList()
            };
            if (message.ChatId != null)
            {
                messageDto.ConversationId = message.ChatId.GetValueOrDefault();
                messageDto.ConversationType = ConversationType.Chat;
            }
            if (message.DialogId != null)
            {
                messageDto.ConversationId = message.DialogId.GetValueOrDefault();
                messageDto.ConversationType = ConversationType.Dialog;
            }
            if (message.ChannelId != null)
            {
                messageDto.ConversationId = message.ChannelId.GetValueOrDefault();
                messageDto.ConversationType = ConversationType.Channel;
            }
            return messageDto;
        }

        public static MessageDto GetMessageDto(MessageVm message)
        {
            MessageDto result = new MessageDto
            {
                SendingTime = message.SendingTime.GetValueOrDefault(),
                SenderId = message.SenderId,
                Text = message.Text,
                Read = message.Read ?? false,
                GlobalId = message.GlobalId ?? Guid.Empty,
                Replyto = message.ReplyTo,
                ReceiverId = message.ReceiverId,
                Lifetime = message.Lifetime,
                NodesIds = message.NodesId?.ToList()
            };
            result.ConversationId = message.ConversationId.GetValueOrDefault();
            result.ConversationType = message.ConversationType;
            if (message.Attachments != null)
            {
                result.Attachments = AttachmentConverter.GetAttachments(message.Attachments)?.ToList();
            }
            return result;
        }

        public static MessageDto GetMessageDto(EditedMessage editedMessage, Message message)
        {
            MessageDto result = new MessageDto
            {
                SendingTime = message.SendingTime,
                Text = editedMessage.Text,
                Attachments = editedMessage.Attachments?.Select(attach => new AttachmentDto
                {
                    Hash = attach.Hash,
                    Payload = attach.Payload,
                    Type = attach.AttachmentType
                }).ToList(),
                SenderId = message.SenderId,
                Replyto = message.Replyto,
                ReceiverId = message.ReceiverId,
                Read = message.Read,
                GlobalId = message.GlobalId,
                Deleted = message.Deleted,
                UpdatedAt = message.UpdatedAt
            };
            if (message.ChatId != null)
            {
                result.ConversationType = ConversationType.Chat;
                result.ConversationId = message.ChatId.Value;
            }
            if (message.ChannelId != null)
            {
                result.ConversationType = ConversationType.Channel;
                result.ConversationId = message.ChannelId.Value;
            }
            if (message.DialogId != null)
            {
                result.ConversationType = ConversationType.Dialog;
                result.ConversationId = message.DialogId.Value;
            }
            return result;
        }

        public static List<MessageDto> GetMessagesDto(IEnumerable<Message> messages)
        {
            return messages?.Select(GetMessageDto).ToList();
        }

        public static List<MessageDto> GetMessagesDto(List<MessageVm> messages)
        {
            return messages?.Select(GetMessageDto).ToList();
        }

        public static List<MessageDto> GetMessagesDto(List<EditedMessage> editedMessages, Message message)
        {
            return editedMessages?.Select(opt => GetMessageDto(opt, message)).ToList();
        }
    }
}
