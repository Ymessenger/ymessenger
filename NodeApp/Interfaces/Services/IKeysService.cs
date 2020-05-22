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
using NodeApp.HttpServer.Models;
using NodeApp.MessengerData.DataTransferObjects;
using ObjectsLibrary.ViewModels;

namespace NodeApp.Interfaces
{
    public interface IKeysService
    {
        Task<List<KeyVm>> AddNewUserKeysAsync(IEnumerable<KeyVm> keys, long userId);
        Task<NodeKeysDto> CreateNewNodeKeysAsync(long? nodeId, CreateNewKeysModel.KeyLength keyLength, uint lifetime);
        Task<List<KeyVm>> DeleteUserKeysAsync(IEnumerable<long> keysId, long userId);
        Task<NodeKeysDto> GetActualNodeKeysAsync(long? nodeId);
        Task<NodeKeysDto> GetNodeKeysAsync(long keyId);
        Task<NodeKeysDto> GetNodeKeysAsync(long nodeId, long keyId);
        Task<KeyVm> GetUserKeyAsync(long publicKeyId, long userId, bool isSignKey);
        Task<List<KeyVm>> GetUserPublicKeysAsync(long userId, long? time = 0, bool? direction = true);
        Task<List<KeyVm>> GetUserPublicKeysAsync(long userId, IEnumerable<long> keysId);
        Task<List<NodeKeysDto>> ReencryptNodeKeysAsync(long nodeId, string oldPassword, string newPassword);
        Task<NodeKeysDto> SaveNodePublicKeyAsync(long nodeId, byte[] publicKey, long keyId);
        Task<KeyVm> SetNewSymmetricKeyForChat(KeyVm key, long chatId, long userId);
    }
}