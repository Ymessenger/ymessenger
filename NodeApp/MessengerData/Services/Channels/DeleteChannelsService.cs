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
using NodeApp.Interfaces.Services.Channels;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Enums;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Channels
{
    public class DeleteChannelsService : IDeleteChannelsService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public DeleteChannelsService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task DeleteChannelAsync(long channelId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var channelUser = await context.ChannelUsers
                .Include(opt => opt.Channel)
                .FirstOrDefaultAsync(opt => opt.ChannelId == channelId
                    && opt.UserId == userId
                    && opt.ChannelUserRole == ChannelUserRole.Creator)
                .ConfigureAwait(false);
                if (channelUser != null)
                {
                    context.Remove(channelUser.Channel);
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
