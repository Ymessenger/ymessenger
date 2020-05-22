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
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Dialogs
{
    public class LoadDialogsService : ILoadDialogsService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public LoadDialogsService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task<List<long>> GetUserDialogsIdAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return await context.Dialogs
                .Where(opt => opt.FirstUID == userId)
                .Select(opt => opt.Id)
                .ToListAsync()
                .ConfigureAwait(false);
            }
        }
        public async Task<long> GetMirrorDialogIdAsync(long dialogId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                Dialog dialog = await context.Dialogs.FirstOrDefaultAsync(opt => opt.Id == dialogId).ConfigureAwait(false);
                if (dialog != null)
                {
                    Dialog secondDialogId = await context.Dialogs.FirstOrDefaultAsync(opt => opt.FirstUID == dialog.SecondUID && opt.SecondUID == dialog.FirstUID).ConfigureAwait(false);
                    return secondDialogId.Id;
                }
                return dialogId;
            }
        }
        public async Task<List<long>> GetDialogsIdByUsersIdPairAsync(long firstUserId, long secondUserId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return await context.Dialogs
                    .Where(dialog => (dialog.FirstUID == firstUserId && dialog.SecondUID == secondUserId)
                    || (dialog.SecondUID == firstUserId && dialog.FirstUID == secondUserId))
                    .OrderByDescending(opt => opt.FirstU)
                    .Select(dialog => dialog.Id)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
        }
        public async Task<List<UserVm>> GetDialogUsersAsync(long dialogId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var dialog = await context.Dialogs
                .Include(opt => opt.FirstU)
                .Include(opt => opt.SecondU)
                .FirstOrDefaultAsync(opt => opt.Id == dialogId)
                .ConfigureAwait(false);
                return UserConverter.GetUsersVm(
                    new List<User>
                    {
                         dialog.FirstU,
                         dialog.SecondU
                    });
            }
        }
        public async Task<DialogDto> GetDialogAsync(long conversationId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var dialog = await context.Dialogs
                .FirstOrDefaultAsync(dial => dial.Id == conversationId).ConfigureAwait(false);
                if (dialog == null)
                {
                    return null;
                }

                return new DialogDto
                {
                    FirstUserId = dialog.FirstUID,
                    SecondUserId = dialog.SecondUID,
                    IsMuted = dialog.IsMuted,
                    Id = dialog.Id
                };
            }
        }
        public async Task<List<long>> GetDlalogNodesAsync(long dialogId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var query = context.Dialogs
                .Include(opt => opt.FirstU)
                .Include(opt => opt.SecondU)
                .Where(opt => opt.Id == dialogId)
                .Select(opt => new List<long> { opt.FirstU.NodeId.Value, opt.SecondU.NodeId.Value });
                return await query.FirstOrDefaultAsync().ConfigureAwait(false);
            }
        }
        public async Task<List<ConversationPreviewVm>> GetUserDialogsPreviewAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var query = from dialog in context.Dialogs
                            join message in context.Messages
                            on
                                new { MessageId = dialog.LastMessageGlobalId.Value, DialogId = dialog.Id }
                            equals
                                new { MessageId = message.GlobalId, DialogId = message.DialogId.Value }
                            into messageTable
                            from message in messageTable.DefaultIfEmpty()
                            join attachment in context.Attachments on message.Id equals attachment.MessageId into attachTable
                            from attachment in attachTable.DefaultIfEmpty()
                            join user in context.Users on dialog.SecondUID equals user.Id
                            where dialog.FirstUID == userId || (dialog.FirstUID == userId && dialog.SecondUID == userId)
                               && user.Deleted == false
                               && message.Deleted == false
                            select new ConversationPreviewVm
                            {
                                ConversationId = dialog.Id,
                                LastMessageSenderId = message.SenderId,
                                LastMessageTime = message.SendingTime,
                                Photo = user.Photo,
                                PreviewText = message.Text,
                                SecondUid = dialog.SecondUID,
                                Title = $"{user.NameFirst} {user.NameSecond}",
                                ConversationType = ConversationType.Dialog,
                                Read = message.Read,
                                AttachmentType = (AttachmentType)attachment.Type,
                                LastMessageId = message.GlobalId,
                                IsMuted = dialog.IsMuted
                            };
                return await query.ToListAsync().ConfigureAwait(false);
            }
        }

        public async Task<List<DialogDto>> GetUsersDialogsAsync(long firstUser, long secondUser)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var dialogs = await context.Dialogs
                    .Where(dialog => (dialog.FirstUID == firstUser && dialog.SecondUID == secondUser) ||
                        (dialog.FirstUID == secondUser && dialog.SecondUID == firstUser))
                    .ToListAsync().ConfigureAwait(false);
                return dialogs?.Select(dialog => new DialogDto
                {
                    FirstUserId = dialog.FirstUID,
                    SecondUserId = dialog.SecondUID,
                    Id = dialog.Id
                }).ToList();
            }
        }
    }
}