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

namespace NodeApp.Interfaces
{
    public interface INoticeService
    {
        void SendEncryptedKeysNoticeAsync(byte[] encryptedData, byte[] encryptedSymmetricKey, ClientConnection responseConnection, ClientConnection requestorConnection, byte[] publicKey);
        Task SendNeedLoginNoticeAsync(ClientConnection client);
        void SendNewSessionNoticeAsync(ClientConnection connection);
        void SendPendingMessagesAsync(List<PendingMessageDto> pendingMessages, long receiverId);
        void SendUserNodeChangedNoticeAsync(long userId, long newNodeId);
    }
}