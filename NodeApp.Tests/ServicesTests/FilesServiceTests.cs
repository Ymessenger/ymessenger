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
using NodeApp.Extensions;
using NodeApp.Interfaces;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class FilesServiceTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IFilesService filesService;
        public FilesServiceTests()
        {
            TestsData testsData = TestsData.Create(nameof(FilesServiceTests));
            filesService = testsData.AppServiceProvider.FilesService;
            fillTestDbHelper = testsData.FillTestDbHelper;           
        }
        [Fact]
        public async Task SaveFileInfo()
        {
            var uploader = fillTestDbHelper.Users.FirstOrDefault();
            var expected = new FileInfoVm
            {              
                Filename = "Filename",
                Hash = RandomExtensions.NextBytes(26),
                NumericId = 0,
                UploaderId = uploader.Id,
                Uploaded = DateTime.UtcNow.ToUnixTime(),
                Size = 1024,
                NodeId = 1                
            };
            var actual = await filesService.SaveFileAsync(expected, uploader.Id, "ssylca");
            Assert.True(expected.Hash.SequenceEqual(actual.Hash)
                && expected.Filename == actual.Filename
                && expected.Uploaded == actual.Uploaded
                && expected.UploaderId == actual.UploaderId
                && expected.NumericId == actual.NumericId);
        }
        [Fact]
        public async Task GetFileInfo()
        {
            var uploader = fillTestDbHelper.Users.FirstOrDefault();
            var expected = new FileInfoVm
            {
                Filename = "Filename",
                Hash = RandomExtensions.NextBytes(26),
                NumericId = 0,
                UploaderId = uploader.Id,
                Uploaded = DateTime.UtcNow.ToUnixTime(),
                Size = 1024,
                NodeId = 1
            };
            expected = await filesService.SaveFileAsync(expected, uploader.Id, "ssylca");
            var actual = FileInfoConverter.GetFileInfoVm(await filesService.GetFileInfoAsync(expected.FileId));
            Assert.Equal(expected.ToJson(), actual.ToJson());
        }
        [Fact]
        public async Task GetFilesInfo()
        {
            var uploader = fillTestDbHelper.Users.FirstOrDefault();
            var files = fillTestDbHelper.Files.Where(opt => opt.UploaderId == uploader.Id).ToList();
            var actualFiles = await filesService.GetFilesInfoAsync(0L.ToDateTime(), uploader.Id);
            Assert.Equal(files.Count, actualFiles.Count);              
        }
        [Fact]
        public async Task GetFilesInfoByIds()
        {
            var uploader = fillTestDbHelper.Users.FirstOrDefault();
            var files = fillTestDbHelper.Files.Where(opt => opt.UploaderId == uploader.Id).ToList();
            var actualFiles = await filesService.GetFilesInfoAsync(files.Select(opt => opt.Id).ToList());
            Assert.Equal(files.Select(opt => opt.Id), actualFiles.Select(opt => opt.FileId));
        }
        [Fact]
        public async Task DeleteFiles()
        {
            var uploader = fillTestDbHelper.Users.Skip(1).FirstOrDefault();
            var fileinfo = new FileInfoVm
            {
                Filename = "Filename",
                Hash = RandomExtensions.NextBytes(26),
                NumericId = 0,
                UploaderId = uploader.Id,
                Uploaded = DateTime.UtcNow.ToUnixTime(),
                Size = 1024,
                NodeId = 1
            };
            fileinfo = await filesService.SaveFileAsync(fileinfo, uploader.Id, "ssylca");
            var expected = FileInfoConverter.GetFileInfoVm(await filesService.GetFileInfoAsync(fileinfo.FileId));
            await filesService.DeleteFilesAsync(new List<string> { expected.FileId }, uploader.Id);
            Assert.Null(fillTestDbHelper.Files.FirstOrDefault(opt => opt.Id == expected.FileId && !opt.Deleted));
        }
        [Fact]
        public async Task UpdateFileInformation() 
        {
            var file = fillTestDbHelper.Files.FirstOrDefault();
            var newFileUrl = RandomExtensions.NextString(10);
            await filesService.UpdateFileInformationAsync(newFileUrl, file.Id);
            Assert.True(fillTestDbHelper.Files.FirstOrDefault(opt => opt.Id == file.Id).Url == newFileUrl);
        }
    }
}
