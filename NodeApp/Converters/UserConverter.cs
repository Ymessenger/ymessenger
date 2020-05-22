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
using NodeApp.Extensions;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.Converters
{
    public static class UserConverter
    {
        public static UserVm GetUserVm(User user, long? userId = null)
        {
            if (user == null)
            {
                return null;
            }
            var resultUser = new UserVm()
            {
                About = user.About,
                Birthday = user.Birthday,
                City = user.City,
                NameFirst = user.NameFirst,
                NameSecond = user.NameSecond,
                Online = user.Online.GetValueOrDefault(),
                Language = user.Language,
                Country = user.Country,
                Photo = user.Photo,
                Id = user.Id,
                RegistrationDate = user.RegistrationDate.GetValueOrDefault(),
                NodeId = user.NodeId,
                Emails = user.Emails?.Select(opt => opt.EmailAddress).ToList(),
                Phones = user.Phones?.Select(opt => new UserPhoneVm
                {
                    FullNumber = opt.PhoneNumber,
                    UserId = user.Id,
                    IsMain = opt.Main
                }).ToList(),
                Tag = user.Tag,
                Security = user.Security,
                BlackList = user.BlackList?.Select(opt => opt.BadUid).ToList(),
                Visible = new List<BitArray> { new BitArray(BitConverter.GetBytes(user.Privacy)) },
                Keys = KeyConverter.GetKeysVm(user.UserPublicKeys),
                ContactsPrivacy = user.ContactsPrivacy != null
                    ? new BitArray(BitConverter.GetBytes(user.ContactsPrivacy.Value))
                    : null,
                Privacy = new BitArray(BitConverter.GetBytes(user.Privacy)),
                Confirmed = user.Confirmed.GetValueOrDefault(),
                Banned = user.Banned,
                SyncContacts = user.SyncContacts
                
            };
            if (user.UserContacts != null && userId != null && user.UserContacts.Any())
            {
                var contactDto = ContactConverter.GetContactDto(user.UserContacts.FirstOrDefault());
                resultUser.Contact = ContactConverter.GetContactVm(contactDto);
            }
            if (userId != null && user.UserContacts != null && user.UserContacts.Any())
            {
                var groups = GroupConverter.GetGroupsDto(user.UserContacts.FirstOrDefault()?.ContactGroups?.Select(opt => opt.Group)?.ToList());
                resultUser.Groups = GroupConverter.GetGroupsVm(groups);
            }
            return resultUser;
        }

        public static List<UserVm> GetUsersVm(List<UserDto> usersDto, long? userId)
        {
            var result = usersDto.Select(GetUserVm).ToList();
            return AppServiceProvider.Instance.PrivacyService.ApplyPrivacySettings(result, userId);
        }

        public static UserVm GetUserVm(UserDto user)
        {
            if (user == null)
            {
                return null;
            }

            return new UserVm
            {
                Id = user.Id,
                About = user.About,
                Birthday = user.Birthday,
                BlackList = user.Blacklist,
                City = user.City,
                Country = user.Country,
                Emails = user.Emails,
                Keys = user.Keys?.Select(key => new KeyVm
                {
                    ChatId = key.ChatId,
                    Data = key.KeyData,
                    GenerationTime = key.GenerationTimeSeconds,
                    KeyId = key.KeyId,
                    Lifetime = key.ExpirationTimeSeconds - key.GenerationTimeSeconds,
                    UserId = key.UserId,
                    Version = key.Version
                }).ToList(),
                Language = user.Language,
                NameFirst = user.NameFirst,
                NameSecond = user.NameSecond,
                NodeId = user.NodeId,
                Online = user.Online,
                Phones = user.Phones?.Select(phone => new UserPhoneVm
                {
                    FullNumber = phone
                }).ToList(),
                Photo = user.Photo,
                RegistrationDate = user.RegistrationDate,
                Security = user.Security,
                Tag = user.Tag,
                Visible = new List<BitArray> { new BitArray(BitConverter.GetBytes(user.Privacy)) },
                ContactsPrivacy = user.ContactsPrivacy != null
                    ? new BitArray(BitConverter.GetBytes(user.ContactsPrivacy.Value))
                    : null,
                Privacy = new BitArray(BitConverter.GetBytes(user.Privacy)),
                Confirmed = user.Confirmed,
                Banned = user.Banned,
                SyncContacts = user.SyncContacts
            };
        }

        public static User GetUser(UserDto user)
        {
            if (user == null)
            {
                return null;
            }

            User editableUser = new User
            {
                Id = user.Id,
                About = user.About,
                Birthday = user.Birthday,
                City = user.City,
                Country = user.Country,
                Photo = user.Photo,
                Tag = user.Tag,
                Online = user.Online,
                Language = user.Language,
                NameFirst = user.NameFirst,
                NameSecond = user.NameSecond,
                NodeId = user.NodeId,
                Privacy = user.Privacy,
                Security = user.Security?.ToArray(),
                RegistrationDate = user.RegistrationDate,
                Tokens = user.Tokens?.Select(token => new Token
                {
                    AccessToken = token.AccessToken,
                    AccessTokenExpirationTime = token.AccessTokenExpirationTime,
                    DeviceTokenId = token.DeviceTokenId,
                    RefreshToken = token.RefreshToken,
                    RefreshTokenExpirationTime = token.RefreshTokenExpirationTime,
                    UserId = token.UserId
                }).ToList(),
                BlackList = user.Blacklist?.Select(blockedId => new BadUser
                {
                    Uid = user.Id,
                    BadUid = blockedId
                }).ToList(),
                Emails = user.Emails?.Select(email => new Emails
                {
                    EmailAddress = email,
                    UserId = user.Id
                }).ToList(),
                Phones = user.Phones?.Select(phone => new Phones
                {
                    PhoneNumber = phone,
                    UserId = user.Id
                }).ToList(),
                UserPublicKeys = user.Keys?.Select(key => new Key
                {
                    KeyId = key.KeyId,
                    KeyData = key.KeyData,
                    UserId = user.Id,
                    GenerationTimeSeconds = key.GenerationTimeSeconds,
                    ExpirationTimeSeconds = key.ExpirationTimeSeconds,
                    Version = key.Version,
                    ChatId = key.ChatId
                }).ToList(),
                ContactsPrivacy = user.ContactsPrivacy,
                Banned = user.Banned,
                SyncContacts = user.SyncContacts
            };
            return editableUser;
        }

        public static UserDto GetUserDto(User user, IEnumerable<ChatDto> userChats = null, IEnumerable<ChannelDto> userChannels = null)
        {
            if (user == null)
            {
                return null;
            }

            return new UserDto
            {
                Id = user.Id,
                About = user.About,
                Birthday = user.Birthday,
                NameFirst = user.NameFirst,
                Blacklist = user.BlackList?.Select(opt => opt.BadUid).ToList(),
                Channels = userChannels?.ToList(),
                Chats = userChats?.ToList(),
                Deleted = user.Deleted,
                City = user.City,
                Country = user.Country,
                Dialogs = user.DialogsFirstU?.Select(dialog => new DialogDto
                {
                    FirstUserId = dialog.FirstUID,
                    SecondUserId = dialog.SecondUID,
                    Messages = MessageConverter.GetMessagesDto(dialog.Messages)
                }).ToList(),
                Emails = user.Emails?.Select(opt => opt.EmailAddress).ToList(),
                Language = user.Language,
                NameSecond = user.NameSecond,
                NodeId = user.NodeId,
                Online = user.Online,
                Phones = user.Phones?.Select(opt => opt.PhoneNumber).ToList(),
                Photo = user.Photo,
                RegistrationDate = user.RegistrationDate,
                Security = user.Security,
                Tag = user.Tag,
                Tokens = TokenConverter.GetTokensDto(user.Tokens),
                Privacy = user.Privacy,
                FilesInfo = FileInfoConverter.GetFilesInfoDto(user.FilesInfo),
                Keys = KeyConverter.GetKeysDto(user.UserPublicKeys),
                ContactsPrivacy = user.ContactsPrivacy,
                Confirmed = user.Confirmed.GetValueOrDefault(),
                PasswordHash = user.Sha512Password,
                Banned = user.Banned,
                SyncContacts = user.SyncContacts,
                Contacts = user.Contacts?.Select(contact => new ContactDto
                {
                    ContactId = contact.ContactId,
                    ContactUserId = contact.ContactUserId,
                    Name = contact.Name,
                    UserId = contact.UserId
                }).ToList(),
                ContactGroups = user.Groups?.Select(group => new GroupDto
                {
                    GroupId = group.GroupId,
                    PrivacySettings = group.PrivacySettings,
                    Title = group.Title,
                    UserId = group.UserId,
                    UsersId = group.ContactGroups?.Select(contactGroup => contactGroup.Contact?.ContactUserId ?? 0).Distinct().ToList()
                }).ToList(),
                Favorites = user.Favorites?.Select(favorite => new UserFavoritesDto
                {
                    ChannelId = favorite.ChannelId,
                    ChatId = favorite.ChatId,
                    UserId = favorite.UserId,
                    ContactId = favorite.ContactId,
                    SerialNumber = favorite.SerialNumber
                }).ToList()
            };
        }

        public static UserDto GetUserDto(UserVm user)
        {
            if (user == null)
            {
                return null;
            }

            return new UserDto
            {
                Id = user.Id.GetValueOrDefault(),
                About = user.About,
                Birthday = user.Birthday,
                Blacklist = user.BlackList,
                City = user.City,
                Privacy = (user.Privacy?.ToInt32() ?? user.Visible?.FirstOrDefault()?.ToInt32()).GetValueOrDefault(),
                Country = user.Country,
                Language = user.Language,
                Emails = user.Emails,
                Phones = user.Phones?.Select(opt => opt.FullNumber).ToList(),
                NameFirst = user.NameFirst,
                NameSecond = user.NameSecond,
                Photo = user.Photo,
                Tag = user.Tag,
                NodeId = user.NodeId.GetValueOrDefault(),
                RegistrationDate = user.RegistrationDate,
                ContactsPrivacy = user.ContactsPrivacy?.ToInt32(),
                Security = user.Security?.ToArray(),
                Online = user.Online,
                Banned = user.Banned,
                SyncContacts = user.SyncContacts
            };
        }

        public static User GetUser(User editableUser, UserDto user)
        {
            if (editableUser == null || user == null)
            {
                return null;
            }

            editableUser.About = user.About;
            editableUser.Birthday = user.Birthday;
            editableUser.City = user.City;
            editableUser.Country = user.Country;
            editableUser.Photo = user.Photo;
            editableUser.Tag = user.Tag;
            editableUser.Online = user.Online;
            editableUser.Language = user.Language;
            editableUser.NameFirst = user.NameFirst;
            editableUser.NameSecond = user.NameSecond;
            editableUser.NodeId = user.NodeId;
            editableUser.Privacy = user.Privacy;
            editableUser.Security = user.Security?.ToArray();
            editableUser.RegistrationDate = user.RegistrationDate;
            editableUser.Tokens = user.Tokens?.Select(token => new Token
            {
                AccessToken = token.AccessToken,
                AccessTokenExpirationTime = token.AccessTokenExpirationTime,
                DeviceTokenId = token.DeviceTokenId,
                RefreshToken = token.RefreshToken,
                RefreshTokenExpirationTime = token.RefreshTokenExpirationTime,
                UserId = token.UserId
            }).ToList();
            editableUser.BlackList = user.Blacklist?.Select(blockedId => new BadUser
            {
                Uid = user.Id,
                BadUid = blockedId
            }).ToList();
            editableUser.Emails = user.Emails?.Select(email => new Emails
            {
                EmailAddress = email,
                UserId = user.Id
            }).ToList();
            editableUser.Phones = user.Phones?.Select(phone => new Phones
            {
                PhoneNumber = phone,
                UserId = user.Id
            }).ToList();
            editableUser.UserPublicKeys = user.Keys?.Select(key => new Key
            {
                KeyId = key.KeyId,
                KeyData = key.KeyData,
                UserId = user.Id,
                GenerationTimeSeconds = key.GenerationTimeSeconds,
                ExpirationTimeSeconds = key.ExpirationTimeSeconds,
                Version = key.Version,
                ChatId = key.ChatId
            }).ToList();
            editableUser.ContactsPrivacy = user.ContactsPrivacy;
            editableUser.SyncContacts = user.SyncContacts;
            return editableUser;
        }

        public static List<UserDto> GetUsersDto(List<User> users)
        {
            return users?.Select(user => GetUserDto(user, null, null)).ToList();
        }

        public static User GetUser(User editableUser, EditUserVm editUser)
        {
            if (editUser == null || editableUser == null)
            {
                return editableUser;
            }

            editableUser.About = editUser.About ?? editableUser.About;
            editableUser.Birthday = editUser.Birthday ?? editableUser.Birthday;
            editableUser.City = editUser.City ?? editableUser.City;
            editableUser.Country = editUser.Country ?? editableUser.Country;
            editableUser.NameFirst = editUser.NameFirst ?? editableUser.NameFirst;
            editableUser.NameSecond = editUser.NameSecond ?? editableUser.NameSecond;
            editableUser.Security = editUser.Security?.ToArray() ?? editableUser.Security;
            editableUser.Privacy = editUser.Privacy?.ToInt32() ?? editUser.Visible?.FirstOrDefault()?.ToInt32() ?? editableUser.Privacy;
            editableUser.ContactsPrivacy = editUser.ContactsPrivacy?.ToInt32();
            editableUser.Photo = editUser.Photo ?? editableUser.Photo;
            editableUser.Language = editUser.Language ?? editableUser.Language;
            editableUser.SyncContacts = editUser.SyncContacts ?? editableUser.SyncContacts;
            return editableUser;
        }

        public static List<UserVm> GetUsersVm(IEnumerable<User> users, long? userId = null)
        {
            List<UserVm> result = new List<UserVm>();
            foreach (var user in users)
            {
                result.Add(GetUserVm(user, userId));
            }
            return result;
        }
    }
}
