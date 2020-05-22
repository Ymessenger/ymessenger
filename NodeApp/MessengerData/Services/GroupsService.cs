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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using Npgsql;
using ObjectsLibrary.Exceptions;

namespace NodeApp.MessengerData.Services
{
    public class GroupsService : IGroupsService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public GroupsService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task<GroupDto> CreateOrEditGroupAsync(GroupDto groupDto)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    Group result;
                    var group = await context.Groups
                        .Include(opt => opt.ContactGroups)
                        .ThenInclude(opt => opt.Contact)
                        .FirstOrDefaultAsync(opt => opt.UserId == groupDto.UserId && opt.GroupId == groupDto.GroupId)
                        .ConfigureAwait(false);
                    if (group == null)
                    {
                        Group newGroup = new Group
                        {
                            PrivacySettings = groupDto.PrivacySettings,
                            Title = groupDto.Title,
                            UserId = groupDto.UserId,
                            GroupId = groupDto.GroupId
                        };
                        if (groupDto.UsersId != null && groupDto.UsersId.Any())
                        {
                            var contacts = await context.Contacts
                                .Where(opt => opt.UserId == groupDto.UserId && groupDto.UsersId.Contains(opt.ContactUserId))
                                .ToListAsync()
                                .ConfigureAwait(false);
                            HashSet<ContactGroup> contactGroups = contacts
                                .Select(opt => new ContactGroup { ContactId = opt.ContactId })
                                .ToHashSet();
                            newGroup.ContactGroups = contactGroups;
                        }
                        await context.Groups.AddAsync(newGroup).ConfigureAwait(false);
                        result = newGroup;
                    }
                    else
                    {
                        group.PrivacySettings = groupDto.PrivacySettings;
                        group.Title = groupDto.Title;
                        context.Update(group);
                        result = group;
                    }
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return GroupConverter.GetGroupDto(result);
                }
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "23503")
                {
                    throw new ObjectDoesNotExistsException("User is not exists.", ex);
                }
                throw new InternalErrorException("Database error.", ex);
            }
        }
        public async Task<List<GroupDto>> GetUserGroupsAsync(long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var groups = await context.Groups.Where(opt => opt.UserId == userId).ToListAsync().ConfigureAwait(false);
                return GroupConverter.GetGroupsDto(groups);
            }
        }

        public async Task<List<ContactDto>> GetGroupContactsAsync(Guid groupId, long userId, long? navigationUserId = 0)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var contacts = await context.ContactsGroups
                .Include(opt => opt.Contact)
                .OrderBy(opt => opt.Contact.ContactUserId)
                .Where(opt => opt.GroupId == groupId && opt.Contact.ContactUserId > navigationUserId && opt.Contact.UserId == userId)
                .Select(opt => opt.Contact)
                .Include(opt => opt.ContactUser)
                .Include(opt => opt.ContactGroups)
                .ThenInclude(opt => opt.Group)
                .ToListAsync()
                .ConfigureAwait(false);
                return ContactConverter.GetContactsDto(contacts);
            }
        }

        public async Task RemoveUsersFromGroupsAsync(List<long> usersId, Guid groupId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var contactsCondition = PredicateBuilder.New<Contact>();
                contactsCondition = usersId.Aggregate(contactsCondition,
                    (current, value) => current.Or(opt => opt.ContactUserId == value).Expand());
                var contacts = await context.Contacts
                        .Where(opt => opt.UserId == userId)
                        .Where(contactsCondition)
                        .ToListAsync()
                        .ConfigureAwait(false);
                var group = await context.Groups.FirstOrDefaultAsync(opt => opt.GroupId == groupId && opt.UserId == userId).ConfigureAwait(false);
                if (contacts.Count < usersId.Count || group == null)
                    throw new ObjectDoesNotExistsException();
                var contactGroups = await context.ContactsGroups
                    .Where(opt => opt.GroupId == group.GroupId
                        && contacts.Select(p => p.ContactId).Contains(opt.ContactId))
                    .ToListAsync()
                    .ConfigureAwait(false);
                context.ContactsGroups.RemoveRange(contactGroups);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task AddUsersToGroupAsync(List<long> usersId, Guid groupId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var contactsCondition = PredicateBuilder.New<Contact>();
                contactsCondition = usersId.Aggregate(contactsCondition,
                    (current, value) => current.Or(opt => opt.ContactUserId == value).Expand());
                var contacts = await context.Contacts
                    .AsNoTracking()
                    .Where(opt => opt.UserId == userId)
                    .Where(contactsCondition)
                    .ToListAsync()
                    .ConfigureAwait(false);
                var group = await context.Groups
                    .AsNoTracking()
                    .Include(opt => opt.ContactGroups)
                    .FirstOrDefaultAsync(opt => opt.GroupId == groupId && opt.UserId == userId)
                    .ConfigureAwait(false);
                if (contacts.Count < usersId.Count || group == null)
                    throw new ObjectDoesNotExistsException();
                bool anyExisitingContacts = contacts.Any(contact => group.ContactGroups.Any(opt => opt.ContactId == contact.ContactId));
                if (anyExisitingContacts)
                    throw new InvalidOperationException($"Some contacts are already in the group.");
                List<ContactGroup> contactGroups = contacts.Select(opt => new ContactGroup
                {
                    ContactId = opt.ContactId,
                    GroupId = group.GroupId
                }).ToList();
                await context.ContactsGroups.AddRangeAsync(contactGroups).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task RemoveUserGroupsAsync(List<Guid> groupsId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var groupsCondition = PredicateBuilder.New<Group>();
                groupsCondition = groupsId.Aggregate(groupsCondition,
                    (current, value) => current.Or(opt => opt.GroupId == value).Expand());
                var groups = await context.Groups
                    .Where(opt => opt.UserId == userId)
                    .Where(groupsCondition)
                    .ToListAsync()
                    .ConfigureAwait(false);
                if (groups.Count < groupsId.Count)
                    throw new ObjectDoesNotExistsException();
                context.Groups.RemoveRange(groups);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<List<GroupDto>> GetGroupsAsync(List<Guid> groupsId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var groupsCondition = PredicateBuilder.New<Group>();
                groupsCondition = groupsId.Aggregate(groupsCondition,
                    (current, value) => current.Or(opt => opt.GroupId == value).Expand());
                var groups = await context.Groups
                    .Include(opt => opt.ContactGroups)
                    .ThenInclude(opt => opt.Contact)
                    .Where(groupsCondition)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return GroupConverter.GetGroupsDto(groups);
            }
        }
    }
}