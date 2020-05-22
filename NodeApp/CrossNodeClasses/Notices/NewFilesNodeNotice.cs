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
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.CrossNodeClasses.Notices
{
    [Serializable]
    public class NewFilesNodeNotice : NodeNotice
    {
        public List<ValuePair<FileInfoVm, byte[]>> FilesValuePairs; 
        public long KeyId { get; set; }
        public NewFilesNodeNotice() { }
        public NewFilesNodeNotice(IEnumerable<ValuePair<FileInfoVm, byte[]>> filesValuePairs, long keyId)
        {
            FilesValuePairs = filesValuePairs.ToList();
            KeyId = keyId;
            NoticeCode = Enums.NodeNoticeCode.NewFiles;
        }
        public NewFilesNodeNotice(FileInfoVm file, byte[] privateData, long keyId)
        {
            FilesValuePairs = new List<ValuePair<FileInfoVm, byte[]>> { new ValuePair<FileInfoVm, byte[]>(file, privateData)};
            KeyId = keyId;
            NoticeCode = Enums.NodeNoticeCode.NewFiles;
        }
    }
}
