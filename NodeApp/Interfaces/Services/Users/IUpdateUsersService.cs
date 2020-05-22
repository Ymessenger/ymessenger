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
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;

namespace NodeApp.Interfaces.Services.Users
{
    public interface IUpdateUsersService
    {
        Task<List<long>> AddUsersToBlackListAsync(IEnumerable<long> usersId, long userId);
        Task<UserDto> CreateOrUpdateUserAsync(UserDto user);
        Task<short> CreateVCodeAsync(string targetEmail, long? userId = null);
        Task<short> CreateVCodeAsync(UserPhoneVm userPhone, RequestType requestType, long? userId = null);
        Task<List<long>> DeleteUsersFromBlackListAsync(IEnumerable<long> usersId, long userId);
        Task<UserVm> EditUserAsync(EditUserVm editableUser, long userId);
        Task EditUserNodeAsync(long userId, long? newNodeId);
        Task SetUsersConfirmedAsync(List<long> usersIds);
        Task UpdateUserActivityTimeAsync(ClientConnection clientConnection);
        Task<UserVm> UpdateUserPhoneAsync(long userId, string phone);
        Task<UserVm> UpdateUserEmailAsync(long userId, string email);
        Task<UserVm> SetUserBannedAsync(long userId);
    }
}