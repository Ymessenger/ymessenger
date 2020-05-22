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
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.ViewModels;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class BlockSegmentsNoticeHandler : ICommunicationHandler
    {
        private readonly BlockSegmentsNotice notice;
        private readonly NodeConnection current;

        public BlockSegmentsNoticeHandler(NodeNotice notice, NodeConnection current)
        {
            this.notice = (BlockSegmentsNotice)notice;
            this.current = current;
        }

        public async Task HandleAsync()
        {
            BlockGenerationHelper.Instance.AddSegments(notice.BlockSegments);
        }

        public bool IsObjectValid()
        {
            if (current.Node == null)
            {
                return false;
            }
            foreach (BlockSegmentVm segment in notice.BlockSegments)
            {
                if (segment.PrivateData == null && segment.PublicData == null)
                {
                    return false;
                }

                if (segment.SegmentHeader == null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}