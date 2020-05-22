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
using LinqKit;
using Microsoft.EntityFrameworkCore;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Users
{
    public class DeleteUsersService : IDeleteUsersService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public DeleteUsersService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task DeleteUserInformationAsync(long userId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    User user = await context.Users
                        .Include(opt => opt.Emails)
                        .Include(opt => opt.Phones)
                        .Include(opt => opt.Tokens)
                        .FirstOrDefaultAsync(opt => opt.Id == userId)
                        .ConfigureAwait(false);
                    user.Phones = null;
                    user.Emails = null;
                    user.About = null;
                    user.Birthday = null;
                    user.City = null;
                    user.Country = null;
                    user.Sha512Password = null;
                    user.Photo = null;
                    user.NameSecond = null;
                    user.NameFirst = "unknown";
                    user.Tokens = null;
                    context.Update(user);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async Task DeleteUserAsync(long userId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    User user = await context.Users
                        .Include(opt => opt.Phones)
                        .Include(opt => opt.Emails)
                        .Include(opt => opt.Tokens)
                        .FirstOrDefaultAsync(opt => opt.Id == userId).ConfigureAwait(false);
                    if (user != null)
                    {
                        user.Deleted = true;
                        user.Tokens = null;
                        if (NodeSettings.Configs.Node.PermanentlyDeleting)
                        {
                            user.NameFirst = null;
                            user.NameSecond = null;
                            user.About = null;
                            user.Tag = null;
                            user.Sha512Password = null;
                            user.Language = null;
                            user.Country = null;
                            user.City = null;
                            user.Birthday = null;
                            user.Emails = null;
                            user.Phones = null;
                            user.SearchVector = null;
                            user.Photo = null;
                        }
                        await context.SaveChangesAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        throw new UserNotFoundException($"User with Id:{userId} not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DeleteUserException(ex);
            }
        }

        public async Task DeleteUsersAsync(IEnumerable<long> usersId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var usersCondition = PredicateBuilder.New<User>();
                usersCondition = usersId.Aggregate(usersCondition,
                    (current, value) => current.Or(user => user.Id == value).Expand());
                List<User> users = await context.Users.Where(usersCondition).ToListAsync().ConfigureAwait(false);
                context.RemoveRange(users);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}