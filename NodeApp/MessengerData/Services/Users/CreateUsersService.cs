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
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.Entities;
using Npgsql;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services.Users
{
    public class CreateUsersService : ICreateUsersService
    {
        private readonly IPoolsService poolsService;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public CreateUsersService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            poolsService = appServiceProvider.PoolsService;
            this.contextFactory = contextFactory;
        }

        public CreateUsersService()
        {
        }

        public const ushort PASSWORD_LENGTH = 128;
        public static byte[] GetSha512Hash(string value)
        {
            using (SHA512 sha = SHA512.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            }
        }
        public async Task<ValuePair<UserVm, string>> CreateNewUserAsync(UserVm user, long nodeId, bool confirmed, string password = null)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                using (var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        User targetUser = new User
                        {
                            Id = await poolsService.GetUserIdAsync().ConfigureAwait(false),
                            About = user.About,
                            Birthday = user.Birthday,
                            City = user.City,
                            Country = user.Country,
                            Language = user.Language,
                            NameFirst = user.NameFirst,
                            NameSecond = user.NameSecond,
                            Photo = user.Photo,
                            Confirmed = confirmed,
                            RegistrationDate = DateTime.UtcNow.ToUnixTime(),
                            NodeId = nodeId,
                            Privacy = user.Privacy?.ToInt32() ?? 0,
                            Tag = RandomExtensions.NextString(10, "QWERTYUIOPASDFGHJKLZXCVBNM1234567890")
                        };
                        if (user.Emails != null)
                        {
                            targetUser.Emails = user.Emails.Select(email => new Emails
                            {
                                EmailAddress = email
                            }).ToList();
                        }
                        if (user.Phones != null)
                        {
                            targetUser.Phones = user.Phones.Select(phone => new Phones
                            {
                                PhoneNumber = phone.FullNumber,
                                Main = phone.IsMain.GetValueOrDefault()
                            }).ToList();
                        }
                        if (password == null)
                        {
                            password = RandomExtensions.NextString(PASSWORD_LENGTH);
                        }

                        targetUser.Sha512Password = GetSha512Hash(password);
                        await context.Users.AddAsync(targetUser).ConfigureAwait(false);
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        transaction.Commit();
                        return new ValuePair<UserVm, string>(UserConverter.GetUserVm(targetUser), password);
                    }
                    catch (DbUpdateException ex)
                    {
                        if (ex.InnerException is PostgresException postgresException)
                        {
                            if (postgresException.ConstraintName == "IX_Emails_EmailAddress")
                            {
                                throw new CreateNewUserException($"Email already exists.");
                            }

                            if (postgresException.ConstraintName == "IX_Phones_PhoneNumber")
                            {
                                throw new CreateNewUserException($"Phone already exists");
                            }
                        }
                        throw new CreateNewUserException("Failed to create user.", ex);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new CreateNewUserException("Failed to create user.", ex);
                    }
                }
            }
        }
    }
}