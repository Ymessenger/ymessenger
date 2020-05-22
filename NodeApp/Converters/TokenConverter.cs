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
    public static class TokenConverter
    {
        public static TokenVm GetTokenVm(Token token)
        {
            return token == null
                ? null
                : new TokenVm
                {
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken,
                    UserId = token.UserId,
                    DeviceTokenId = token.DeviceTokenId,
                    AccessTokenExpirationTime = token.AccessTokenExpirationTime,
                    RefreshTokenExpirationTime = token.RefreshTokenExpirationTime,
                    Id = token.Id,
                    AppName = token.AppName,
                    DeviceName = token.DeviceName,
                    OSName = token.OSName
                };
        }

        private static TokenDto GetTokenDto(Token token)
        {
            return new TokenDto
            {
                AccessToken = token.AccessToken,
                AccessTokenExpirationTime = token.AccessTokenExpirationTime,
                DeviceTokenId = token.DeviceTokenId,
                RefreshToken = token.RefreshToken,
                RefreshTokenExpirationTime = token.RefreshTokenExpirationTime,
                UserId = token.UserId,
                AppName = token.AppName,
                DeviceName = token.DeviceName,
                Id = token.Id,
                OSName = token.OSName
            };
        }

        public static List<TokenVm> GetTokensVm(IEnumerable<Token> tokens)
        {
            return tokens?.Select(GetTokenVm).ToList();
        }

        public static List<TokenDto> GetTokensDto(IEnumerable<Token> tokens)
        {
            return tokens?.Select(GetTokenDto).ToList();
        }
    }
}
