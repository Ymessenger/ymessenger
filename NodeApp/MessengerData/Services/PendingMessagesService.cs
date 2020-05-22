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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary;
using ObjectsLibrary.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1000:Possible SQL injection vulnerability.")]
    public class PendingMessagesService : IPendingMessagesService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public PendingMessagesService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task<PendingMessageDto> AddUserPendingMessageAsync(long receiverId, object content, Guid? messageId)
        {
            PendingMessage pendingMessage = new PendingMessage
            {
                Content = ObjectSerializer.ObjectToJson(content),
                ExpiredAt = DateTime.UtcNow.AddDays(1).ToUnixTime(),
                ReceiverId = receiverId,
                MessageId = messageId,
                SentAt = DateTime.UtcNow.ToUnixTime()
            };
            using (MessengerDbContext context = contextFactory.Create())
            {
                await context.PendingMessages.AddAsync(pendingMessage).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
            return new PendingMessageDto
            {
                Content = pendingMessage.Content,
                ExpiredAt = pendingMessage.ExpiredAt,
                Id = pendingMessage.Id,
                ReceiverId = pendingMessage.ReceiverId.Value,
                SentAt = pendingMessage.SentAt
            };
        }
        public async Task<List<PendingMessageDto>> GetUserPendingMessagesAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var pendingMessages = await context.PendingMessages
                .Where(message => message.ReceiverId == userId)
                .OrderBy(message => message.SentAt)
                .ToListAsync()
                .ConfigureAwait(false);
                return pendingMessages.Select(message => new PendingMessageDto
                {
                    Content = message.Content,
                    ExpiredAt = message.ExpiredAt,
                    Id = message.Id,
                    ReceiverId = message.ReceiverId.Value,
                    SentAt = message.SentAt
                }).ToList();
            }
        }
        public async Task<PendingMessageDto> AddNodePendingMessageAsync(long nodeId, object content, TimeSpan lifetime)
        {
            if (nodeId == NodeSettings.Configs?.Node?.Id)
                return null;
            PendingMessage pendingMessage = new PendingMessage
            {
                Content = Convert.ToBase64String(ObjectSerializer.ObjectToByteArray(content)),
                ExpiredAt = DateTime.UtcNow.AddSeconds(lifetime.TotalSeconds).ToUnixTime(),
                NodeId = nodeId,
                SentAt = DateTime.UtcNow.ToUnixTime()
            };
            using (MessengerDbContext context = contextFactory.Create())
            {
                if (!await context.Nodes.AnyAsync(opt => opt.Id == nodeId).ConfigureAwait(false))
                    return null;
                await context.PendingMessages.AddAsync(pendingMessage).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
            return new PendingMessageDto
            {
                Content = pendingMessage.Content,
                ExpiredAt = pendingMessage.ExpiredAt,
                Id = pendingMessage.Id,
                NodeId = pendingMessage.NodeId,
                SentAt = pendingMessage.SentAt
            };
        }
        public async Task<List<PendingMessageDto>> GetNodePendingMessagesAsync(long nodeId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var pendingMessages = await context.PendingMessages
                    .Where(message => message.NodeId == nodeId)
                    .OrderBy(message => message.SentAt)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return pendingMessages.Select(message => new PendingMessageDto
                {
                    Content = message.Content,
                    ExpiredAt = message.ExpiredAt,
                    Id = message.Id,
                    NodeId = message.NodeId.Value,
                    SentAt = message.SentAt
                }).ToList();
            }
        }
        public async Task RemovePendingMessagesAsync(IEnumerable<int> pendingMessagesIds)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                if (pendingMessagesIds == null || !pendingMessagesIds.Any())
                    return;
                RawSqlString sql = $@"DELETE FROM ""{nameof(context.PendingMessages)}"" 
                                      WHERE ""{nameof(PendingMessage.Id)}"" IN ({pendingMessagesIds.AsString(",")});";
                await context.Database.ExecuteSqlCommandAsync(sql).ConfigureAwait(false);
            }
        }
        public async Task RemoveExpiredAsync()
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var sql = $@"DELETE FROM ""{nameof(context.PendingMessages)}"" 
                             WHERE ""{nameof(PendingMessage.ExpiredAt)}"">={DateTime.UtcNow.ToUnixTime()} ";
                await context.Database.ExecuteSqlCommandAsync(sql).ConfigureAwait(false);
            }
        }
        public async Task<bool> RemovePendingMessageByMessagesIds(IEnumerable<Guid> messagesIds)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                RawSqlString sql = $@"DELETE FROM ""{nameof(context.PendingMessages)}"" 
                                      WHERE ""{nameof(PendingMessage.MessageId)}"" IN ({messagesIds.AsString(",")});";
                return await context.Database.ExecuteSqlCommandAsync(sql).ConfigureAwait(false) > 0;
            }
        }
    }
}