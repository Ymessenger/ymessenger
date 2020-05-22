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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Chats
{
    public class DeleteChatsService : IDeleteChatsService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public DeleteChatsService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task DeleteChatAsync(long chatId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                IQueryable<Chat> query = from chat in context.Chats
                                         join chatUser in context.ChatUsers on chat.Id equals chatUser.ChatId
                                         where chatUser.ChatId == chatId
                                             && chatUser.UserId == userId
                                             && chatUser.UserRole == UserRole.Creator
                                         select chat;
                Chat targetChat = await query.FirstOrDefaultAsync().ConfigureAwait(false);
                if (targetChat != null)
                {
                    targetChat.Deleted = true;
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
                else
                {
                    throw new PermissionDeniedException();
                }
            }
        }
    }
}
