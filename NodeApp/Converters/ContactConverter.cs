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
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.Converters
{
    public static class ContactConverter
    {
        public static ContactDto GetContactDto(ContactVm contact, long userId)
        {
            return new ContactDto
            {
                ContactId = contact.ContactId.GetValueOrDefault(),
                ContactUserId = contact.ContactUserId,
                Name = contact.Name,
                UserId = userId
            };
        }
        public static ContactVm GetContactVm(ContactDto contact)
        {
            if (contact == null)
            {
                return null;
            }

            return new ContactVm
            {
                ContactId = contact.ContactId,
                ContactUserId = contact.ContactUserId,
                Name = contact.Name,
                GroupsId = contact.GroupsId,
                ContactUser = contact.ContactUser != null
                    ? AppServiceProvider.Instance.PrivacyService.ApplyPrivacySettings(
                        new List<UserVm>
                        {
                            UserConverter.GetUserVm(contact.ContactUser)
                        },
                        contact.UserId).FirstOrDefault()
                    : null
            };
        }

        public static ContactDto GetContactDto(Contact contact)
        {
            if (contact == null)
            {
                return null;
            }

            return new ContactDto
            {
                ContactId = contact.ContactId,
                ContactUserId = contact.ContactUserId,
                Name = contact.Name,
                UserId = contact.UserId,
                GroupsId = contact.ContactGroups?.Select(opt => opt.GroupId).ToList(),
                ContactUser = UserConverter.GetUserDto(contact.ContactUser)
            };
        }

        public static List<ContactVm> GetContactsVm(List<ContactDto> contactsDto)
        {
            return contactsDto?.Select(GetContactVm).ToList();
        }

        public static List<ContactDto> GetContactsDto(List<Contact> contacts)
        {
            return contacts?.Select(GetContactDto).ToList();
        }
    }
}
