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
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System;
using System.ComponentModel.DataAnnotations;

namespace NodeApp.MessengerData.Entities
{
    public partial class FileInfo
    {
        public FileInfo()
        {           
        }

        public FileInfo(FileInfoVm fileInfo, string url, long uploaderId)
        {           
            Id = fileInfo.FileId ?? RandomExtensions.NextString(64);
            FileName = fileInfo.Filename;
            Url = url;
            UploaderId = fileInfo.UploaderId.GetValueOrDefault();
            UploadDate = fileInfo.Uploaded.GetValueOrDefault(DateTime.UtcNow.ToUnixTime());
            NodeId = fileInfo.NodeId.GetValueOrDefault();
            Size = fileInfo.Size;
            Hash = fileInfo.Hash;
            NumericId = fileInfo.NumericId.GetValueOrDefault();
            ImageMetadata = fileInfo.ImageMetadata;
        }        

        [MaxLength(256)]
        public string Id { get; set; }
        public long NumericId { get; set; }
        public long? UploaderId { get; set; }
        public long NodeId { get; set; }
        public byte[] Hash { get; set; }
        [MaxLength(300)]
        public string Url { get; set; }
        public long UploadDate { get; set; }
        [MaxLength(100)]
        public string FileName { get; set; }
        public long? Size { get; set; }
        public bool Deleted { get; set; }
        [MaxLength(50)]
        public string Storage { get; set; }           
        public ImageMetadata ImageMetadata { get; set; }
        

        public User Uploader { get; set; }
        public Node Node { get; set; }
       
    }
}
