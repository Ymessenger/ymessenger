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
using NodeApp.Interfaces.Services;
using ObjectsLibrary.ViewModels;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.Tests.Mocks
{
    public class MockPrivacyService : IPrivacyService
    {
        public ChatVm ApplyPrivacySettings(ChatVm chat, BitArray mask)
        {
            return chat;
        }

        public List<ChatUserVm> ApplyPrivacySettings(IEnumerable<ChatUserVm> chatUsers)
        {
            return chatUsers?.ToList();
        }

        public List<UserVm> ApplyPrivacySettings(IEnumerable<UserVm> users, long? userId = null)
        {
            return users?.ToList();
        }

        public List<UserVm> ApplyPrivacySettings(IEnumerable<UserVm> users, List<string> phones, long? userId = null)
        {
            return users?.ToList();
        }

        public List<UserVm> ApplyPrivacySettings(IEnumerable<UserVm> users, string searchQuery, long? userId = null)
        {
            return users?.ToList();
        }

        public UserVm ApplyPrivacySettings(UserVm user, BitArray mask, long? userId = null)
        {
            return user;
        }

        public async Task<List<UserVm>> ApplyPrivacySettingsAsync(IEnumerable<UserVm> users, long? userId = null)
        {
            return users?.ToList();
        }

        public List<UserVm> FilterUsersDataByFieldsNames(IEnumerable<string> fieldsNames, IEnumerable<UserVm> users)
        {
            return users?.ToList();
        }
    }
}
