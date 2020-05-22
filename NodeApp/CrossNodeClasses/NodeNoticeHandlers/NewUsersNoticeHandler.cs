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
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class NewUsersNoticeHandler : ICommunicationHandler
    {
        private readonly CreateOrEditUsersNodeNotice notice;
        private readonly NodeConnection current;
        private readonly ICrossNodeService crossNodeService;
        public NewUsersNoticeHandler(CommunicationObject @object, NodeConnection current, ICrossNodeService crossNodeService)
        {
            notice = (CreateOrEditUsersNodeNotice)@object;
            this.crossNodeService = crossNodeService;
            this.current = current;
        }
        public async Task HandleAsync()
        {
            foreach (var user in notice.Users)
            {
                await crossNodeService.NewOrEditUserAsync(user, current.Node.Id).ConfigureAwait(false);
            }

            BlockGenerationHelper.Instance.AddSegments(notice.BlockSegments);
        }

        public bool IsObjectValid()
        {
            if (notice.Users == null || !notice.Users.Any() || current.Node == null)
            {
                return false;
            }

            foreach (var user in notice.Users)
            {
                if (user.UserId == 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}