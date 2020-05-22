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
using ObjectsLibrary.Enums;
using System;

namespace NodeApp.MessengerData.DataTransferObjects
{
    [Serializable]
    public class KeyDto
    {
        public long? UserId { get; set; }
        public long? ChatId { get; set; }
        public long KeyId { get; set; }
        public byte[] KeyData { get; set; }
        public long? ExpirationTimeSeconds { get; set; }
        public long GenerationTimeSeconds { get; set; }
        public byte Version { get; set; }
        public KeyType Type { get; set; }
    }
}
