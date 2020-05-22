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
    public static class GroupConverter
    {
        public static GroupDto GetGroupDto(GroupVm group, long userId)
        {
            return new GroupDto
            {
                GroupId = group.GroupId.GetValueOrDefault(),
                PrivacySettings = group.PrivacySettings?.ToInt32(),
                Title = group.Title,
                UserId = userId,
                UsersId = group.UsersId
            };
        }

        public static GroupVm GetGroupVm(GroupDto group)
        {
            BitArray privacy = group.PrivacySettings != null
                ? new BitArray(BitConverter.GetBytes(group.PrivacySettings.Value))
                : null;
            return new GroupVm
            {
                GroupId = group.GroupId,
                PrivacySettings = privacy,
                Title = group.Title,
                UsersId = group.UsersId
            };
        }

        public static List<GroupVm> GetGroupsVm(List<GroupDto> groups)
        {
            return groups.Select(GetGroupVm).ToList();
        }

        public static GroupDto GetGroupDto(Group group)
        {
            return new GroupDto
            {
                GroupId = group.GroupId,
                PrivacySettings = group.PrivacySettings,
                Title = group.Title,
                UserId = group.UserId,
                UsersId = group.ContactGroups?.Select(opt => opt.Contact?.ContactUserId ?? 0).ToList()
            };
        }

        public static List<GroupDto> GetGroupsDto(List<Group> groups)
        {
            return groups.Select(GetGroupDto).ToList();
        }
    }
}
