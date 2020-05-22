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
using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using Npgsql;
using ObjectsLibrary.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class ContactsService : IContactsService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public ContactsService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task<ContactDto> CreateOrEditContactAsync(ContactDto contactDto)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    Contact result;
                    var contact = await context.Contacts
                        .Include(opt => opt.ContactGroups)
                        .FirstOrDefaultAsync(opt => opt.UserId == contactDto.UserId && opt.ContactUserId == contactDto.ContactUserId)
                        .ConfigureAwait(false);
                    if (contact == null)
                    {
                        Contact newContact = new Contact
                        {
                            Name = contactDto.Name,
                            UserId = contactDto.UserId,
                            ContactUserId = contactDto.ContactUserId,
                            ContactId = contactDto.ContactId
                        };
                        await context.Contacts.AddAsync(newContact).ConfigureAwait(false);
                        result = newContact;
                    }
                    else
                    {
                        contact.Name = contactDto.Name;
                        context.Update(contact);
                        result = contact;
                    }
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return ContactConverter.GetContactDto(result);
                }
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "23503")
                {
                    throw new ObjectDoesNotExistsException("User not found.", ex);
                }
                throw new InternalErrorException("Database error.", ex);
            }
        }

        public async Task<List<ContactDto>> GetUserContactsAsync(long userId, long navigationUserId = 0, byte? limit = null)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var query = context.Contacts
                .OrderBy(opt => opt.ContactUserId)
                .Include(opt => opt.ContactUser)
                .Include(opt => opt.ContactGroups)
                .Where(opt => opt.UserId == userId && opt.ContactUserId > navigationUserId);
                if (limit != null)
                {
                    query = query.Take(limit.Value);
                }

                var contacts = await query.ToListAsync().ConfigureAwait(false);
                return ContactConverter.GetContactsDto(contacts);
            }
        }

        public async Task RemoveContactsAsync(List<Guid> contactsId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var contactsCondition = PredicateBuilder.New<Contact>();
                contactsCondition = contactsId.Aggregate(contactsCondition,
                    (current, value) => current.Or(opt => opt.ContactId == value).Expand());
                var contacts = await context.Contacts
                    .Where(opt => opt.UserId == userId)
                    .Where(contactsCondition)
                    .ToListAsync()
                    .ConfigureAwait(false);
                if (!contacts.Any() || contacts.Count < contactsId.Count)
                {
                    throw new ObjectDoesNotExistsException();
                }

                context.Contacts.RemoveRange(contacts);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<List<ContactDto>> GetUsersContactsAsync(long userId, List<long> usersId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var contactsCondition = PredicateBuilder.New<Contact>();
                contactsCondition = usersId.Aggregate(contactsCondition,
                    (current, value) => current.Or(opt => opt.UserId == value).Expand());
                var contacts = await context.Contacts
                    .Include(opt => opt.ContactGroups)
                    .Where(opt => opt.ContactUserId == userId)
                    .Where(contactsCondition)
                    .ToListAsync().ConfigureAwait(false);
                return ContactConverter.GetContactsDto(contacts);
            }
        }
    }
}