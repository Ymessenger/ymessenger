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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.MessengerData.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class GroupsTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IGroupsService groupsService;
        private readonly IContactsService contactsService;
        public GroupsTests()
        {
            TestsData testsData = TestsData.Create(nameof(GroupsTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            groupsService = testsData.AppServiceProvider.GroupsService;
            contactsService = testsData.AppServiceProvider.ContactsService;
        }
        [Fact]
        public async Task CreateOrEditGroup()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var exptectedGroup = new GroupDto
            {
                Title = "Group",
                PrivacySettings = int.MaxValue,
                UserId = user.Id,
                UsersId = user.Contacts.Select(opt => opt.UserId).Distinct().ToList()                
            };
            var actualGroup = await groupsService.CreateOrEditGroupAsync(exptectedGroup);
            Assert.True(exptectedGroup.PrivacySettings == actualGroup.PrivacySettings 
                && exptectedGroup.Title == actualGroup.Title
                && exptectedGroup.UserId == actualGroup.UserId
                && exptectedGroup.UsersId.SequenceEqual(actualGroup.UsersId));
        }
        [Fact]
        public async Task GetUserGroups()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var expectedGroups = GroupConverter.GetGroupsDto(fillTestDbHelper.Groups.Where(opt => opt.UserId == user.Id).ToList());
            var actualGroups = await groupsService.GetUserGroupsAsync(user.Id);
            Assert.Equal(expectedGroups.Select(opt => opt.GroupId).OrderBy(opt => opt), actualGroups.Select(opt => opt.GroupId).OrderBy(opt => opt));
        }
        [Fact]
        public async Task GetGroupContacts()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault(opt => opt.Groups.Any());
            var group = fillTestDbHelper.Groups.FirstOrDefault(opt => opt.UserId == user.Id);
            var expectedContacts = ContactConverter.GetContactsDto(group.ContactGroups.Select(opt => opt.Contact).ToList());
            var actualContacts = await groupsService.GetGroupContactsAsync(group.GroupId, user.Id);
            Assert.Equal(expectedContacts.Select(opt => opt.ContactId), actualContacts.Select(opt => opt.ContactId));
        }   
        [Fact]
        public async Task RemoveUserGroups()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var groups = fillTestDbHelper.Groups.Where(opt => opt.UserId == user.Id);
            await groupsService.RemoveUserGroupsAsync(groups.Select(opt => opt.GroupId).ToList(), user.Id);
            var actualGroups = fillTestDbHelper.Groups.Where(opt => opt.UserId == user.Id);
            Assert.True(actualGroups.IsNullOrEmpty());
        }
        [Fact]
        public async Task RemoveUsersFromGroups()
        {
            var user = fillTestDbHelper.Users.Skip(1).FirstOrDefault();
            var group = GroupConverter.GetGroupDto(user.Groups.FirstOrDefault());
            await groupsService.RemoveUsersFromGroupsAsync(group.UsersId, group.GroupId, user.Id);
            var actualGroup = GroupConverter.GetGroupDto(fillTestDbHelper.Groups.FirstOrDefault(opt => opt.GroupId == group.GroupId));
            Assert.True(actualGroup.UsersId.IsNullOrEmpty());
        }
        [Fact]
        public async Task AddUsersToGroup()
        {
            var group = fillTestDbHelper.Groups.FirstOrDefault();
            var contact = await contactsService.CreateOrEditContactAsync(new ContactDto
            {
                ContactUserId = -1,
                UserId = group.UserId
            });
            await groupsService.AddUsersToGroupAsync(new List<long> { contact.ContactUserId }, group.GroupId, group.UserId);
        }        
    }
}