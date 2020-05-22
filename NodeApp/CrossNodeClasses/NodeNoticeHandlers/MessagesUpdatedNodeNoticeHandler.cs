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
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Messages;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class MessagesUpdatedNodeNoticeHandler : ICommunicationHandler
    {
        private readonly MessagesUpdatedNodeNotice notice;
        private readonly NodeConnection nodeConnection;
        private readonly IConversationsNoticeService conversationsNoticeService;
        private readonly IUpdateMessagesService updateMessagesService;
        private readonly IAttachmentsService attachmentsService;

        public MessagesUpdatedNodeNoticeHandler(NodeNotice notice,
                                                NodeConnection nodeConnection,
                                                IConversationsNoticeService conversationsNoticeService,
                                                IUpdateMessagesService updateMessagesService,
                                                IAttachmentsService attachmentsService)
        {
            this.notice = (MessagesUpdatedNodeNotice)notice;
            this.nodeConnection = nodeConnection;
            this.conversationsNoticeService = conversationsNoticeService;
            this.updateMessagesService = updateMessagesService;
            this.attachmentsService = attachmentsService;
        }

        public async Task HandleAsync()
        {
            foreach (var message in notice.Messages)
            {
                try
                {
                    if (message.Attachments != null && message.Attachments.Any())
                    {
                        bool isValid = await attachmentsService.CheckEditedMessageAttachmentsAsync(MessageConverter.GetMessageVm(message, null), notice.EditorUserId).ConfigureAwait(false);
                        if (!isValid)
                        {
                            return;
                        }
                    }
                    MessageDto edited = await updateMessagesService.EditMessageAsync(message, message.SenderId.GetValueOrDefault()).ConfigureAwait(false);
                    conversationsNoticeService.SendMessagesUpdatedNoticeAsync(
                        message.ConversationId,
                        message.ConversationType,
                        new List<MessageDto> { edited },
                        message.SenderId.GetValueOrDefault(),
                        false,
                        null);
                    UsersConversationsCacheService.Instance.MessageEditedUpdateConversations(edited);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex);
                }
            }
        }

        public bool IsObjectValid()
        {
            return nodeConnection.Node != null && notice.Messages != null && notice.Messages.Any();
        }
    }
}