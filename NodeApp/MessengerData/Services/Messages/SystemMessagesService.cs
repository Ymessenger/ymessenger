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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Messages
{
    public class SystemMessagesService : ISystemMessagesService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        private readonly IConversationsService conversationsService;
        private readonly ILoadDialogsService loadDialogsService;
        public SystemMessagesService(IDbContextFactory<MessengerDbContext> contextFactory, IConversationsService conversationsService, ILoadDialogsService loadDialogsService)
        {
            this.contextFactory = contextFactory;
            this.conversationsService = conversationsService;
            this.loadDialogsService = loadDialogsService;
        }
        public async Task<MessageDto> CreateMessageAsync(ConversationType conversationType, long conversationId, SystemMessageInfo systemMessageInfo)
        {
            List<long> nodesIds = await conversationsService.GetConversationNodesIdsAsync(conversationType, conversationId).ConfigureAwait(false);
            using (MessengerDbContext context = contextFactory.Create())
            {
                var messageInfoJson = systemMessageInfo.ToJson();
                AttachmentDto attachmentDto = new AttachmentDto
                {
                    Type = AttachmentType.SystemMessage,
                    Payload = messageInfoJson,
                    Hash = GetHash(messageInfoJson)
                };
                MessageDto messageDto = new MessageDto
                {
                    ConversationId = conversationId,
                    ConversationType = conversationType,
                    Attachments = new List<AttachmentDto>
                    {
                        attachmentDto
                    },
                    SendingTime = DateTime.UtcNow.ToUnixTime(),
                    GlobalId = Guid.NewGuid(),
                    NodesIds = nodesIds
                };
                Message message = MessageConverter.GetMessage(messageDto);
                if(conversationType == ConversationType.Dialog)
                {
                    var mirrorMessage = (MessageDto)messageDto.Clone();
                    var mirrodDialogId = await loadDialogsService.GetMirrorDialogIdAsync(conversationId);
                    mirrorMessage.ConversationId = mirrodDialogId;
                    await context.Messages.AddAsync(MessageConverter.GetMessage(mirrorMessage));
                }
                await context.Messages.AddAsync(message).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return MessageConverter.GetMessageDto(message);
            }
        }
        private byte[] GetHash(string payload)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            }
        }
    }
}
