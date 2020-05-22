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
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.Interfaces;
using NodeApp.Objects;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class NewNodeKeysNodeNoticeHandler : ICommunicationHandler
    {
        private readonly NewNodeKeysNodeNotice notice;
        private readonly NodeConnection nodeConnection;

        public NewNodeKeysNodeNoticeHandler(NodeNotice notice, NodeConnection nodeConnection)
        {
            this.notice = (NewNodeKeysNodeNotice)notice;
            this.nodeConnection = nodeConnection;
        }

        public async Task HandleAsync()
        {
            nodeConnection.PublicKey = notice.PublicKey;
            nodeConnection.PublicKeyId = notice.KeyId;
            nodeConnection.PublicKeyExpirationTime = notice.ExpirationTime;
            nodeConnection.SignPublicKey = notice.SignPublicKey;
        }

        public bool IsObjectValid()
        {
            return notice.PublicKey != null && notice.PublicKey.Any();
        }
    }
}