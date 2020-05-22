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
using NodeApp.Blockchain;
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class EditUsersNoticeHandler : ICommunicationHandler
    {
        private readonly CreateOrEditUsersNodeNotice notice;
        private readonly ICrossNodeService crossNodeService;
        private readonly NodeConnection nodeConnection;
        public EditUsersNoticeHandler(CommunicationObject @object, NodeConnection nodeConnection, ICrossNodeService crossNodeService)
        {
            notice = (CreateOrEditUsersNodeNotice)@object;
            this.nodeConnection = nodeConnection;
            this.crossNodeService = crossNodeService;
        }
        public async Task HandleAsync()
        {
            foreach (var user in notice.Users)
            {
                await crossNodeService.NewOrEditUserAsync(user, nodeConnection.Node.Id).ConfigureAwait(false);
            }

            BlockGenerationHelper.Instance.AddSegments(notice.BlockSegments);
        }

        public bool IsObjectValid()
        {
            if (notice.Users.IsNullOrEmpty() || nodeConnection.Node == null)
            {
                return false;
            }

            foreach (ShortUser item in notice.Users)
            {
                if (item.UserId == 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}