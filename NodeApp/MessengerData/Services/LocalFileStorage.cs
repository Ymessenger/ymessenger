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
using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using ObjectsLibrary.Interfaces;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly IFilesService filesService;
        public LocalFileStorage()
        {
            filesService = AppServiceProvider.Instance.FilesService;
        }

        public string StorageName => "Local";
        private const string DIR_NAME = "LocalFileStorage";

        public async Task<Stream> GetStreamAsync(string objectId)
        {
            if (!File.Exists(Path.Combine(DIR_NAME, objectId)))
            {
                throw new ObjectDoesNotExistsException();
            }

            return File.OpenRead(Path.Combine(DIR_NAME, objectId));
        }

        public async Task<bool> RemoveAsync(string objectId)
        {
            try
            {
                if (!File.Exists(Path.Combine(DIR_NAME, objectId)))
                {
                    throw new ObjectDoesNotExistsException();
                }

                File.Delete(Path.Combine(DIR_NAME, objectId));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<FileInfoVm>> SearchAsync(string prefix)
        {
            var files = Directory.GetFiles(DIR_NAME);
            if (!files.IsNullOrEmpty())
            {
                List<FileInfoVm> resultFiles = new List<FileInfoVm>();
                foreach (var fileName in files) 
                {
                    try
                    {
                        if (fileName.ToLowerInvariant().Contains(prefix.ToLowerInvariant()))
                        {
                            var fileInfo = await filesService.GetFileInfoAsync(fileName);
                            if(fileInfo != null)
                            {
                                resultFiles.Add(FileInfoConverter.GetFileInfoVm(fileInfo, false));
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.WriteLog(ex);
                    }
                }
                return resultFiles;
            }
            return new List<FileInfoVm>();
        }

        public async Task UploadAsync(Stream stream, string destination)
        {
            if (File.Exists(Path.Combine(DIR_NAME, destination)))
            {
                throw new InvalidOperationException($"File with name '{destination}' already exists.");
            }

            using (FileStream fileStream = File.OpenWrite(Path.Combine(DIR_NAME, destination)))
            {
                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
            }
        }
    }
}
