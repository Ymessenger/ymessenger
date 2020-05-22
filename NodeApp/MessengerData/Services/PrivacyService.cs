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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.MessengerData.DataTransferObjects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;

namespace NodeApp.MessengerData.Services
{
    public class PrivacyService : IPrivacyService
    {
        private readonly IGroupsService groupsService;
        private readonly IContactsService contactsService;

        public PrivacyService(IAppServiceProvider appServiceProvider)
        {
            groupsService = appServiceProvider.GroupsService;
            contactsService = appServiceProvider.ContactsService;
        }
        public ChatVm ApplyPrivacySettings(ChatVm chat, BitArray mask)
        {
            ChatVm result = new ChatVm
            {
                Users = new List<ChatUserVm>(),
                Id = chat.Id,
                Type = chat.Type
            };
            if (mask[0])
            {
                result.Name = chat.Name;
                result.Tag = chat.Tag;
            }
            if (mask[1])
            {
                result.Photo = chat.Photo;
                result.About = chat.About;
            }
            if (mask[2])
            {
                result.Public = chat.Public;
            }
            if (mask[3])
            {
                result.Security = chat.Security;
            }
            if (chat.Users != null)
            {
                if (mask[4])
                {
                    result.Users.AddRange(chat.Users.Where(chatUser => chatUser.UserRole == UserRole.User).ToList());
                }
                if (mask[5])
                {
                    result.Users.AddRange(chat.Users.Where(chatUser => chatUser.UserRole == UserRole.Admin).ToList());
                }
                if (mask[6])
                {
                    result.Users.AddRange(chat.Users.Where(chatUser => chatUser.UserRole == UserRole.Moderator).ToList());
                }
                if (mask[7])
                {
                    result.Users.AddRange(chat.Users.Where(chatUser => chatUser.Banned == true).ToList());
                }
            }
            return result;
        }

        public List<UserVm> ApplyPrivacySettings(IEnumerable<UserVm> users, List<string> phones, long? userId = null)
        {
            if (phones == null)
                throw new ArgumentNullException(nameof(phones));
            if (users == null)
                return null;
            ConcurrentBag<UserVm> result = new ConcurrentBag<UserVm>();
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                var filterUsersDataTask = Task.Run(async () =>
                {
                    List<ContactDto> contacts = await contactsService.GetUsersContactsAsync(
                    userId.GetValueOrDefault(),
                    users.Select(opt => opt.Id.GetValueOrDefault()).ToList())
                    .ConfigureAwait(false);
                    List<Guid> groupsId = new List<Guid>();
                    foreach (var contact in contacts)
                        if (contact.GroupsId?.Any() ?? false)
                            groupsId.AddRange(contact.GroupsId);
                    List<GroupDto> groups = await groupsService.GetGroupsAsync(groupsId).ConfigureAwait(false);
                    foreach (var user in users)
                    {
                        var userGroups = groups.Where(opt => opt.UserId == user.Id && opt.UsersId.Contains(userId.GetValueOrDefault()));
                        var contact = contacts.FirstOrDefault(opt => opt.UserId == user.Id && opt.ContactUserId == userId);
                        int resultPrivacy = user.Privacy.ToInt32();
                        if (userGroups != null && userGroups.Any())
                        {
                            var privacyValues = userGroups.Select(opt => opt.PrivacySettings.GetValueOrDefault());
                            foreach (var value in privacyValues)
                                resultPrivacy |= value;
                        }
                        else if (contact != null)
                            resultPrivacy = (user.ContactsPrivacy?.ToInt32()).GetValueOrDefault() | user.Privacy.ToInt32();
                        var bitMask = new BitArray(BitConverter.GetBytes(resultPrivacy));
                        if ((user.Phones?.Any(opt => phones.Contains(opt.FullNumber)) ?? false) && !bitMask[15])
                            continue;
                        result.Add(ApplyPrivacySettings(user, bitMask, userId));
                    }
                }, cancellationTokenSource.Token);
                filterUsersDataTask.Wait();
            }
            return result.ToList();
        }
        public List<UserVm> ApplyPrivacySettings(IEnumerable<UserVm> users, string searchQuery, long? userId = null)
        {
            if (users == null)
                return null;
            string lowerQuery = searchQuery.ToLowerInvariant();
            ConcurrentBag<UserVm> result = new ConcurrentBag<UserVm>();
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                var filterUsersDataTask = Task.Run(async () =>
                {
                    List<ContactDto> contacts = await contactsService.GetUsersContactsAsync(
                    userId.GetValueOrDefault(),
                    users.Select(opt => opt.Id.GetValueOrDefault()).ToList())
                    .ConfigureAwait(false);
                    List<Guid> groupsId = new List<Guid>();
                    foreach (var contact in contacts)
                    {
                        if (contact.GroupsId?.Any() ?? false)
                        {
                            groupsId.AddRange(contact.GroupsId);
                        }
                    }
                    List<GroupDto> groups = await groupsService.GetGroupsAsync(groupsId).ConfigureAwait(false);
                    foreach (var user in users)
                    {
                        var userGroups = groups.Where(opt => opt.UserId == user.Id && opt.UsersId.Contains(userId.GetValueOrDefault()));
                        var contact = contacts.FirstOrDefault(opt => opt.UserId == user.Id && opt.ContactUserId == userId);
                        int resultPrivacy = user.Privacy.ToInt32();
                        if (userGroups != null && userGroups.Any())
                        {
                            var privacyValues = userGroups.Select(opt => opt.PrivacySettings.GetValueOrDefault());
                            foreach (var value in privacyValues)
                            {
                                resultPrivacy |= value;
                            }
                        }
                        else if (contact != null)
                        {
                            resultPrivacy = (user.ContactsPrivacy?.ToInt32()).GetValueOrDefault() | user.Privacy.ToInt32();
                        }
                        var lowerNameFirst = user.NameFirst?.ToLowerInvariant();
                        var lowerNameSecond = user.NameSecond?.ToLowerInvariant();
                        var queryContainsNameFirst = lowerNameFirst?.Contains(lowerQuery);
                        var queryContainsNameSecond = lowerNameSecond?.Contains(lowerQuery);
                        if (!string.IsNullOrWhiteSpace(lowerNameFirst))
                        {
                            queryContainsNameFirst = queryContainsNameFirst.GetValueOrDefault() || lowerQuery.Contains(lowerNameFirst);
                        }
                        if (!string.IsNullOrWhiteSpace(lowerNameSecond))
                        {
                            queryContainsNameSecond = queryContainsNameSecond.GetValueOrDefault() || lowerQuery.Contains(lowerNameSecond);
                        }
                        var bitMask = new BitArray(BitConverter.GetBytes(resultPrivacy));
                        if ((queryContainsNameFirst.GetValueOrDefault() && !bitMask[1])
                        || (queryContainsNameSecond.GetValueOrDefault() && !bitMask[1])
                        || ((user.Phones?.Any(opt => opt.FullNumber.ToLowerInvariant() == lowerQuery) ?? false) && !bitMask[15])
                        || ((user.Emails?.Any(opt => opt.ToLowerInvariant() == lowerQuery) ?? false) && !bitMask[17])
                        || (user.Tag?.ToLowerInvariant() == lowerQuery && !bitMask[1]))
                        {
                            continue;
                        }
                        result.Add(ApplyPrivacySettings(user, bitMask, userId));
                    }
                }, cancellationTokenSource.Token);
                filterUsersDataTask.Wait();
            }
            return result.ToList();
        }
        public UserVm ApplyPrivacySettings(UserVm user, BitArray mask, long? userId = null)
        {
            if (userId == user.Id)
                return user;
            UserVm resultUser = new UserVm
            {
                Id = user.Id,
                Contact = user.Contact,
                Groups = user.Groups
            };
            if (mask[1])
            {
                resultUser.NameFirst = user.NameFirst;
                resultUser.NameSecond = user.NameSecond;
                resultUser.Tag = user.Tag;
            }
            if (mask[2])
            {
                resultUser.About = user.About;
                resultUser.Photo = user.Photo;
            }
            if (mask[3])
            {
                resultUser.Country = user.Country;
            }
            if (mask[4])
            {
                resultUser.City = user.City;
            }
            if (mask[5])
            {
                resultUser.Birthday = user.Birthday;
            }
            if (mask[6])
            {
                resultUser.Language = user.Language;
            }
            if (mask[13])
            {
                resultUser.NodeId = user.NodeId;
            }
            if (mask[14])
            {
                resultUser.Online = user.Online;
            }
            if (mask[15])
            {
                resultUser.Phones = new List<UserPhoneVm>(user.Phones);
            }
            if (mask[16])
            {
                if (user.Phones?.Count > 1)
                {
                    if (!resultUser.Phones?.Any() ?? false)
                    {
                        resultUser.Phones = new List<UserPhoneVm>(user.Phones.Skip(1).ToList());
                    }
                    else
                    {
                        resultUser.Phones.AddRange(user.Phones.Skip(1));
                    }
                }
            }
            if (mask[17])
            {
                resultUser.Emails = new List<string> { user.Emails?.FirstOrDefault() };
            }
            if (mask[18])
            {
                if (user.Emails?.Count > 1)
                {
                    if (resultUser.Emails == null || !resultUser.Emails.Any())
                    {
                        resultUser.Emails = user.Emails.Skip(1).ToList(); ;
                    }
                    else
                    {
                        resultUser.Emails.AddRange(user.Emails.Skip(1));
                    }
                }
            }
            if (mask[19])
            {
                resultUser.BlackList = user.BlackList;
            }
            return resultUser;
        }

        public List<UserVm> ApplyPrivacySettings(IEnumerable<UserVm> users, long? userId = null)
        {
            return ApplyPrivacyAsync(users, userId).Result;
        }
        public async Task<List<UserVm>> ApplyPrivacySettingsAsync(IEnumerable<UserVm> users, long? userId = null)
        {
            return await ApplyPrivacyAsync(users, userId).ConfigureAwait(false);
        }
        private async Task<List<UserVm>> ApplyPrivacyAsync(IEnumerable<UserVm> users, long? userId = null)
        {
            if (users == null || !users.Any())
                return new List<UserVm>();
            List<UserVm> result = new List<UserVm>();
            List<ContactDto> contacts = await contactsService.GetUsersContactsAsync(
                userId.GetValueOrDefault(),
                users.Select(opt => opt.Id.GetValueOrDefault())?.ToList())
                .ConfigureAwait(false);
            List<Guid> groupsId = new List<Guid>();
            foreach (var contact in contacts)
            {
                if (contact.GroupsId?.Any() ?? false)
                {
                    groupsId.AddRange(contact.GroupsId);
                }
            }
            List<GroupDto> groups = await groupsService.GetGroupsAsync(groupsId).ConfigureAwait(false);
            foreach (var user in users)
            {
                var userGroups = groups.Where(opt => opt.UserId == user.Id && opt.UsersId.Contains(userId.GetValueOrDefault()));
                var contact = contacts.FirstOrDefault(opt => opt.UserId == user.Id && opt.ContactUserId == userId);
                int resultPrivacy = (user.Privacy?.ToInt32()).GetValueOrDefault();
                if (userGroups != null && userGroups.Any())
                {
                    var privacyValues = userGroups.Select(opt => opt.PrivacySettings.GetValueOrDefault());
                    foreach (var value in privacyValues)
                    {
                        resultPrivacy |= value;
                    }
                }
                if (contact != null)
                {
                    resultPrivacy = resultPrivacy | (user.ContactsPrivacy?.ToInt32()).GetValueOrDefault() | (user.Privacy?.ToInt32()).GetValueOrDefault();
                }
                resultPrivacy |= (user.Privacy?.ToInt32()).GetValueOrDefault();
                result.Add(ApplyPrivacySettings(user, new BitArray(BitConverter.GetBytes(resultPrivacy)), userId));
            }
            return result;
        }
        public List<ChatUserVm> ApplyPrivacySettings(IEnumerable<ChatUserVm> chatUsers)
        {
            List<ChatUserVm> result = new List<ChatUserVm>(chatUsers);
            foreach (var chatUser in result)
            {
                if (chatUser.UserInfo != null)
                {
                    chatUser.UserInfo = ApplyPrivacySettings(chatUser.UserInfo, chatUser.UserInfo.Privacy);
                }
            }
            return result;
        }

        public List<UserVm> FilterUsersDataByFieldsNames(IEnumerable<string> fieldsNames, IEnumerable<UserVm> users)
        {
            List<UserVm> usersVm = new List<UserVm>();
            foreach (UserVm user in users)
            {
                UserVm newUser = new UserVm();
                foreach (string field in fieldsNames)
                {
                    PropertyInfo property = user.GetType().GetProperty(field);
                    if (property != null)
                    {
                        object value = property.GetValue(user);
                        newUser.GetType().GetProperty(field).SetValue(newUser, value);
                    }
                }
                usersVm.Add(newUser);
            }
            return usersVm;
        }
    }
}