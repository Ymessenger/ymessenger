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
using NodeApp.CrossNodeClasses.Enums;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.CrossNodeClasses.Notices
{
    [Serializable]
    public class CreateOrEditUsersNodeNotice : NodeNotice
    {
        public List<ShortUser> Users { get; set; }
        public List<BlockSegmentVm> BlockSegments { get; set; }
        public CreateOrEditUsersNodeNotice() { }
        public CreateOrEditUsersNodeNotice(NodeNoticeCode noticeCode, IEnumerable<ShortUser> users, IEnumerable<BlockSegmentVm> blockSegments)
        {
            NoticeCode = noticeCode;
            BlockSegments = blockSegments?.ToList();
            Users = users?.ToList();            
        }

        public CreateOrEditUsersNodeNotice(NodeNoticeCode noticeCode, ShortUser user, BlockSegmentVm blockSegment)
        {
            NoticeCode = noticeCode;
            Users = new List<ShortUser> { user };
            BlockSegments = new List<BlockSegmentVm> { blockSegment };
        }
    }
}
