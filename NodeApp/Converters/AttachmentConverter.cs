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
using NodeApp.MessengerData.Entities;
using NodeApp.Objects;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.Converters
{
    public static class AttachmentConverter
    {
        private static readonly IAttachmentsService attachmentsService = AppServiceProvider.Instance.AttachmentsService;
        private static AttachmentVm GetAttachmentVm(AttachmentDto attachment, long? requestorId)
        {
            return attachment == null
                ? null
                : new AttachmentVm
                {
                    Hash = attachment.Hash,
                    Type = attachment.Type,
                    Payload = PayloadStringToObject(attachment.Payload, attachment.Type, requestorId).Result
                };
        }

        private static async Task<object> PayloadStringToObject(string attachmentPayload, AttachmentType attachmentType, long? requestorId)
        {
            if (attachmentPayload == null)
            {
                return null;
            }

            switch (attachmentType)
            {
                case AttachmentType.Audio:
                case AttachmentType.File:
                case AttachmentType.Picture:
                case AttachmentType.Video:
                case AttachmentType.VoiceMessage:
                case AttachmentType.VideoMessage:
                    {
                        if (IsPayloadJsonObject(attachmentPayload, out FileInfoVm fileInfo))
                        {
                            return fileInfo;
                        }
                        return await attachmentsService.LoadFileInfoAsync(attachmentPayload).ConfigureAwait(false);
                    }

                case AttachmentType.EncryptedMessage:
                    {
                        return ObjectSerializer.JsonToObject<EncryptedMessage>(attachmentPayload);
                    }

                case AttachmentType.ForwardedMessages:
                    {
                        if (IsPayloadJsonObject(attachmentPayload, out ForwardedMessagesInfo messInfo))
                        {
                            ForwardedMessagesInfo messagesInfo =
                                ObjectSerializer.JsonToObject<ForwardedMessagesInfo>(attachmentPayload);
                            return await attachmentsService.LoadForwardedMessagesAsync(messagesInfo, requestorId).ConfigureAwait(false);
                        }
                        return ObjectSerializer.JsonToObject<List<MessageVm>>(attachmentPayload);
                    }

                case AttachmentType.KeyMessage:
                    {
                        return ObjectSerializer.JsonToObject<KeyExchangeMessageVm>(attachmentPayload);
                    }

                case AttachmentType.Poll:
                    {
                        try
                        {
                            PollAttachmentInformation pollInfo =
                                ObjectSerializer.JsonToObject<PollAttachmentInformation>(attachmentPayload);
                            PollDto pollDto = await AppServiceProvider.Instance.PollsService.GetPollAsync(pollInfo.PollId, pollInfo.ConversationId, pollInfo.ConversationType).ConfigureAwait(false);
                            if (requestorId != null)
                            {
                                return PollConverter.GetPollVm(pollDto, requestorId.Value);
                            }
                            else
                            {
                                return PollConverter.GetPollVm(pollDto, pollDto.CreatorId);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog(ex);
                            return null;
                        }
                    }
                case AttachmentType.SystemMessage:
                    {
                        try
                        {
                            return ObjectSerializer.JsonToObject<SystemMessageInfo>(attachmentPayload);
                        }
                        catch(Exception ex)
                        {
                            Logger.WriteLog(ex);
                            return null;
                        }
                    }                    

                default:
                    return attachmentPayload;
            }
        }


        public static AttachmentDto GetAttachment(AttachmentVm attachment)
        {
            if (attachment.Payload == null)
            {
                return null;
            }

            object payload = attachment.Payload;
            AttachmentDto attachmentDto = new AttachmentDto
            {
                Type = attachment.Type,
                Hash = attachment.Hash ?? Array.Empty<byte>()
            };
            if (payload is string strPayload)
            {
                attachmentDto.Payload = strPayload;
            }
            else if (payload is PollVm pollPayload)
            {
                attachmentDto.Payload = ObjectSerializer.ObjectToJson(new PollAttachmentInformation
                {
                    ConversationId = pollPayload.ConversationId.GetValueOrDefault(),
                    ConversationType = pollPayload.ConversationType.GetValueOrDefault(),
                    PollId = pollPayload.PollId.GetValueOrDefault()
                });
            }
            else
            {
                attachmentDto.Payload = ObjectSerializer.ObjectToJson(attachment.Payload);
            }
            return attachmentDto;
        }

        private static AttachmentDto GetAttachmentDto(Attachment attachment)
        {
            return attachment == null
                ? null
                : new AttachmentDto
                {
                    Hash = GetAttachmentHash(attachment).Result,
                    Type = (AttachmentType)attachment.Type,
                    Payload = attachment.Payload
                };
        }

        private static Attachment GetAttachment(AttachmentDto attachment, long messageId) => new Attachment
        {
            Hash = attachment.Hash,
            Payload = attachment.Payload,
            Type = (short)attachment.Type,
            MessageId = messageId
        };

        public static List<Attachment> GetAttachments(IEnumerable<AttachmentDto> attachments, long? messageId)
        {
            return attachments?.Select(opt => GetAttachment(opt, messageId.GetValueOrDefault())).ToList();
        }

        public static List<AttachmentDto> GetAttachmentsDto(IEnumerable<Attachment> attachments)
        {
            return attachments?.Select(GetAttachmentDto).ToList();
        }

        public static List<AttachmentDto> GetAttachments(IEnumerable<AttachmentVm> attachments)
        {
            return attachments?.Select(attach => GetAttachment(attach)).ToList();
        }

        public static List<AttachmentVm> GetAttachmentsVm(IEnumerable<AttachmentDto> attachments, long? requestorId)
        {
            return attachments?.Select(attach => GetAttachmentVm(attach, requestorId)).ToList();
        }
        private static bool IsPayloadJsonObject<T>(string payload, out T deserializedObject)
        {
            try
            {
                deserializedObject = ObjectSerializer.JsonToObject<T>(payload);
                return true;
            }
            catch
            {
                deserializedObject = default;
                return false;
            }
        }
        private static async Task<byte[]> GetAttachmentHash(Attachment attachment)
        {
            switch ((AttachmentType)attachment.Type)
            {
                case AttachmentType.Audio:
                case AttachmentType.File:
                case AttachmentType.Picture:
                case AttachmentType.Video:
                case AttachmentType.VoiceMessage:
                case AttachmentType.VideoMessage:
                    {
                        var fileInfo = await AppServiceProvider.Instance.FilesService.GetFileInfoAsync(attachment.Payload).ConfigureAwait(false);
                        if (fileInfo != null)
                        {
                            return fileInfo.Hash;
                        }
                    }
                    break;
                case AttachmentType.EncryptedMessage:
                    return Array.Empty<byte>();
                case AttachmentType.ForwardedMessages:
                    return Array.Empty<byte>();                
            }
            return Array.Empty<byte>();
        }
    }
}