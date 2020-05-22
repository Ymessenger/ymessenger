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
using ObjectsLibrary.ViewModels;

namespace NodeApp.MessengerData.Services
{
    public interface ITokensService
    {
        Task<TokenVm> CheckTokenAsync(TokenVm targetToken, long nodeId);
        Task<TokenVm> CreateTokenPairByUserIdAsync(long userId, bool generateRefresh = true, int? tokenLifetimeSeconds = 259200);
        Task<ValuePair<TokenVm, string>> EmailPasswordCreateTokenPairAsync(string targetEmail, string password, string deviceTokenId);
        Task<TokenVm> EmailVCodeCreateTokenPairAsync(string email, short vCode, string deviceTokenId);
        Task<List<TokenVm>> GetAllUsersTokensAsync(IEnumerable<long> usersId, bool requireDeviceToken = true);
        Task<ValuePair<TokenVm, string>> PhonePasswordCreateTokenPairAsync(string phone, string password, string deviceTokenId);
        Task<TokenVm> PhoneVCodeCreateTokenPairAsync(string phone, short vCode, string deviceTokenId);
        Task<TokenVm> RefreshTokenPairAsync(long userId, string refreshToken);
        Task<List<TokenVm>> RemoveTokensAsync(long userId, string accessToken, List<long> tokensIds);
        Task SetDeviceTokenIdNullAsync(string deviceTokenId);
        Task<TokenVm> UpdateTokenDataAsync(string osName, string deviceName, string appName, string ipAddress, TokenVm tokenVm);
        Task<ValuePair<TokenVm, string>> UserIdPasswordCreateTokenPairAsync(long userId, string password, string deviceTokenId);
        Task<TokenVm> UserIdVCodeCreateTokenPairAsync(long userId, short vCode, string deviceTokenId);
    }
}