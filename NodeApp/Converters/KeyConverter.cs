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
    public static class KeyConverter
    {
        public static Key GetKey(KeyVm key)
        {
            return key == null
                ? null
                : new Key
                {
                    ChatId = key.ChatId,
                    UserId = key.UserId,
                    KeyData = key.Data,
                    KeyId = key.KeyId,
                    Version = key.Version,
                    GenerationTimeSeconds = key.GenerationTime,
                    ExpirationTimeSeconds = key.GenerationTime + key.Lifetime.GetValueOrDefault(),
                    Type = key.Type
                };
        }

        private static KeyDto GetKeyDto(Key key)
        {
            return new KeyDto
            {
                ChatId = key.ChatId,
                ExpirationTimeSeconds = key.ExpirationTimeSeconds,
                GenerationTimeSeconds = key.GenerationTimeSeconds,
                KeyData = key.KeyData,
                KeyId = key.KeyId,
                UserId = key.UserId,
                Version = key.Version,
                Type = key.Type
            };
        }

        public static KeyVm GetKeyVm(Key key)
        {
            return key == null
                ? null
                : new KeyVm
                {
                    Data = key.KeyData,
                    GenerationTime = key.GenerationTimeSeconds,
                    KeyId = key.KeyId,
                    Lifetime = key.ExpirationTimeSeconds - key.GenerationTimeSeconds,
                    Version = key.Version,
                    ChatId = key.ChatId,
                    UserId = key.UserId,
                    Type = key.Type
                };
        }

        public static List<KeyDto> GetKeysDto(IEnumerable<Key> keys)
        {
            return keys?.Select(GetKeyDto).ToList();
        }

        public static List<Key> GetKeys(IEnumerable<KeyVm> keys)
        {
            return keys?.Select(GetKey).ToList();
        }

        public static List<KeyVm> GetKeysVm(IEnumerable<Key> keys)
        {
            return keys?.Select(GetKeyVm).ToList();
        }
    }
}
