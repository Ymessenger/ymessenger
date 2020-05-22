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
using NodeApp.Interfaces;
using NodeApp.MessengerData.DataTransferObjects;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class ContactsTests 
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IContactsService contactsService;
        public ContactsTests()
        {
            TestsData testsData = TestsData.Create(nameof(ContactsTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            contactsService = testsData.AppServiceProvider.ContactsService;
        }
        [Fact]
        public async Task CreateOrEditContact()
        {
            var firstUser = fillTestDbHelper.Users.FirstOrDefault();
            var secondUser = fillTestDbHelper.Users.LastOrDefault();
            var exprectedContact = new ContactDto
            {
                UserId = firstUser.Id,
                ContactUserId = secondUser.Id,
                Name = "Contact name"                
            };
            var actualContact = await contactsService.CreateOrEditContactAsync(exprectedContact);
            Assert.True(exprectedContact.ContactUserId == actualContact.ContactUserId 
                && exprectedContact.UserId == actualContact.UserId
                && exprectedContact.Name == actualContact.Name);
            var editedExpectedContact = actualContact;
            editedExpectedContact.Name = "Edited name";
            var editedActualContact = await contactsService.CreateOrEditContactAsync(editedExpectedContact);
            Assert.True(editedExpectedContact.Name == editedActualContact.Name);
        }
        [Fact]
        public async Task GetUserContacts()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var actualContacts = await contactsService.GetUserContactsAsync(user.Id);
            Assert.True(user.Contacts.Count == actualContacts.Count);
            long navUserId = actualContacts.Min(opt => opt.ContactUserId);
            var filteredContacts = await contactsService.GetUserContactsAsync(user.Id, navUserId);
            Assert.True(!filteredContacts.Any(opt => opt.ContactUserId == navUserId));
            var filteredLimitedContacts = await contactsService.GetUserContactsAsync(user.Id, navUserId, 1);
            Assert.True(!filteredLimitedContacts.Any(opt => opt.ContactUserId == navUserId) && filteredLimitedContacts.Count == 1);
        }
        [Fact]
        public async Task GetUsersContacts()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var otherUsers = fillTestDbHelper.Users.Skip(1).ToList();
            var contacts = await contactsService.GetUsersContactsAsync(user.Id, otherUsers.Select(opt => opt.Id).ToList());
            Assert.True(contacts.All(opt => opt.ContactUserId == user.Id));
        }
        [Fact]
        public async Task RemoveContacts()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var removingContactsIds = user.Contacts.Take(2).Select(opt => opt.ContactId).ToList();
            await contactsService.RemoveContactsAsync(removingContactsIds, user.Id);
            Assert.True(fillTestDbHelper.Contacts.All(opt => !removingContactsIds.Contains(opt.ContactId)));
        }
    }
}
