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
using System.Collections.Generic;
using System.Threading.Tasks;
using NodeApp.MessengerData.DataTransferObjects;
using ObjectsLibrary.ViewModels;

namespace NodeApp.Interfaces.Services.Users
{
    public interface ILoadUsersService
    {
        Task<List<UserVm>> FindUsersByStringQueryAsync(string stringQuery, long? navigationUserId = 0, bool? direction = true);
        Task<UserDto> GetAllUserDataAsync(long userId);
        Task<UserVm> GetUserAsync(long userId);
        Task<UserVm> GetUserInformationAsync(long userId);
        Task<List<UserVm>> GetUsersAsync(SearchUserVm templateUser = null, byte limit = 100, long navigationId = 0, bool confirmed = true);
        Task<List<UserVm>> GetUsersByIdAsync(IEnumerable<long> usersId, long? requestorId = null);
        Task<List<SessionVm>> GetUserSessionsAsync(long userId);
        Task<bool> IsUserBlacklisted(long targetUserId, IEnumerable<long> usersId);
        Task<bool> IsUserValidAsync(long userId);
        Task<List<UserVm>> FindUsersByPhonesAsync(List<string> phones);
        Task<bool> IsPhoneExistsAsync(string phone);
        Task<bool> IsEmailExistsAsync(string email);
    }
}