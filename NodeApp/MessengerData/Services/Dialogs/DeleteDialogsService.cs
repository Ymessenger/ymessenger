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
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.MessengerData.Entities;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Dialogs
{
    public class DeleteDialogsService : IDeleteDialogsService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public DeleteDialogsService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task DeleteDialogAsync(long dialogId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var dialog = await context.Dialogs.FirstOrDefaultAsync(opt => opt.Id == dialogId && opt.FirstUID == userId).ConfigureAwait(false);
                var mirroDialog = await context.Dialogs.FirstOrDefaultAsync(opt => opt.FirstUID == dialog.SecondUID && opt.SecondUID == dialog.FirstUID).ConfigureAwait(false);
                context.Dialogs.RemoveRange(dialog, mirroDialog);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}