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
using NodeApp.CrossNodeClasses.Responses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using System;
using System.Threading.Tasks;

namespace NodeApp
{
    public class ChatsNodeResponseHandler : ICommunicationHandler
    {
        private readonly ChatsNodeResponse response;
        private readonly ICrossNodeService crossNodeService;
        private readonly NodeConnection nodeConnection;

        public ChatsNodeResponseHandler(NodeResponse response, NodeConnection nodeConnection, ICrossNodeService crossNodeService)
        {
            this.response = (ChatsNodeResponse)response;
            this.crossNodeService = crossNodeService;
            this.nodeConnection = nodeConnection;
        }

        public async Task HandleAsync()
        {
            try
            {
                foreach (var chat in response.Chats)
                {
                    await crossNodeService.NewOrEditChatAsync(chat).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public bool IsObjectValid()
        {
            return nodeConnection.Node != null && !response.Chats.IsNullOrEmpty();
        }
    }
}