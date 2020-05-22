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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.ViewModels;

namespace NodeApp.Interfaces
{
    public interface IFilesService
    {
        Task<List<long>> DeleteFilesAsync(IEnumerable<long> filesId);
        Task<List<long>> DeleteFilesAsync(IEnumerable<string> filesId, long userId);
        Task<FileInfo> GetFileInfoAsync(string fileId);
        Task<List<FileInfoVm>> GetFilesInfoAsync(DateTime navigationTime, long? uploaderId = null, int limit = 100);
        Task<List<FileInfoVm>> GetFilesInfoAsync(List<string> filesId, int limit = 100);
        Task<FileInfoVm> SaveFileAsync(FileInfoVm fileInfo, long userId, string url = null);
        Task<FileInfoVm> SaveFileAsync(long? userId, string filename, string url, long fileSize, byte[] fileHash, string storageType, ImageMetadata imageMetadata = null);
        Task<List<FileInfoVm>> CreateFilesInformationAsync(List<FileInfoVm> filesInfo);
        Task<FileInfoVm> UpdateFileInformationAsync(string fileName, string fileId);
    }
}