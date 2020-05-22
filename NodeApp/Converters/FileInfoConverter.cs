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
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.Converters
{
    public static class FileInfoConverter
    {
        public static FileInfoVm GetFileInfoVm(FileInfo fileInfo, bool hideUploader = true)
        {
            return fileInfo == null
                ? null
                : new FileInfoVm
                {
                    FileId = fileInfo.Id,
                    Filename = fileInfo.FileName,
                    Hash = fileInfo.Hash,
                    NodeId = fileInfo.NodeId,
                    NumericId = fileInfo.NumericId,
                    Size = fileInfo.Size,
                    UploaderId = fileInfo.UploaderId,
                    Uploaded = fileInfo.UploadDate,
                    ImageMetadata = fileInfo.ImageMetadata
                };
        }
      
        public static List<FileInfoVm> GetFilesInfoVm(IEnumerable<FileInfo> filesInfo, bool hideUploader = true)
        {
            if (filesInfo == null)
            {
                return null;
            }
            List<FileInfoVm> result = new List<FileInfoVm>();
            filesInfo.ToList().ForEach(file => result.Add(GetFileInfoVm(file, hideUploader)));
            return result;
        }

        public static FileInfoDto GetFileInfoDto(FileInfo fileInfo)
        {
            return new FileInfoDto
            {
                Deleted = fileInfo.Deleted,
                FileName = fileInfo.FileName,
                Hash = fileInfo.Hash,
                Id = fileInfo.Id,
                NodeId = fileInfo.NodeId,
                NumericId = fileInfo.NumericId,
                Size = fileInfo.Size,
                UploadDate = fileInfo.UploadDate,
                UploaderId = fileInfo.UploaderId,
                ImageMetadata = fileInfo.ImageMetadata
            };
        }
       
        public static FileInfo GetFileInfo(FileInfoVm file)
        {
            return new FileInfo
            {
                FileName = file.Filename,
                Id = file.FileId,
                Hash = file.Hash,
                NodeId = file.NodeId.GetValueOrDefault(),
                NumericId = file.NumericId.GetValueOrDefault(),
                Size = file.Size,
                UploadDate = file.Uploaded.GetValueOrDefault(),
                UploaderId = file.UploaderId,
                ImageMetadata = file.ImageMetadata
            };
        }
        public static List<FileInfo> GetFilesInfo(IEnumerable<FileInfoVm> files)
        {
            return files?.Select(GetFileInfo).ToList();
        }

        public static List<FileInfoDto> GetFilesInfoDto(ICollection<FileInfo> filesInfo)
        {
            return filesInfo?.Select(GetFileInfoDto).ToList();
        }
    }
}
