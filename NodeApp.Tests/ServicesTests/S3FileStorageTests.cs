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
using NodeApp.MessengerData.Services;
using NodeApp.Objects.SettingsObjects;
using ObjectsLibrary;
using ObjectsLibrary.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests.ServicesTests
{
    public class S3FileStorageTests
    {      
  
        S3FileStorageOptions options = new S3FileStorageOptions
        {
            AccessKey = "RN_W8gh6zDoaFwImzIyM",
            BucketName = "ekovizor-ymess",
            EndPoint = "https://s3.yandexcloud.net",
            Region = "ru-central1",
            SecretKey = "3MhIJmgLaR7zrEs-5IPLbh7cWtE-ruLpG5DUlmR58"
        };
        [Fact]
        public async Task UploadTest()
        {
            try
            {
                string objectId = RandomExtensions.NextString(64);
                IFileStorage fileStorage = new S3FileStorage(options);
                using (MemoryStream memoryStream = new MemoryStream(RandomExtensions.NextBytes(1024)))
                {
                    await fileStorage.UploadAsync(memoryStream, objectId);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        [Fact]
        public async Task GetBuckets()
        {
            S3FileStorage fileStorage = new S3FileStorage(options);
            await fileStorage.GetBucketsList().ConfigureAwait(false);
        }
        [Fact]
        public async Task GeoObjectTest()
        {
            S3FileStorage fileStorage = new S3FileStorage(options);
            var stream = await fileStorage.GetStreamAsync("EOzfMJj0RkL3xI4E0rIKsa5bQ8HYe9wy488hHzlmlvrwl0r3dmQqBTrgaN9Rv2mC").ConfigureAwait(false);
            Assert.True(stream.Position == 0 && stream.CanRead);
        }
        [Fact]
        public async Task SearchTest()
        {
            S3FileStorage fileStorage = new S3FileStorage(options);
            var filesInfo = await fileStorage.SearchAsync("EOzfMJj0RkL3xI4E0rIKsa5bQ8HYe9wy488hHzlmlvrwl0r3dmQqBTrgaN9Rv2mC").ConfigureAwait(false);
            Assert.NotEmpty(filesInfo);
        }
        [Fact]
        public async Task RemoveTest()
        {
            S3FileStorage fileStorage = new S3FileStorage(options);
            string objectId = RandomExtensions.NextString(64);
            using (MemoryStream memoryStream = new MemoryStream(RandomExtensions.NextBytes(1024)))
            {
                await fileStorage.UploadAsync(memoryStream, objectId);
            }
            Assert.True(await fileStorage.RemoveAsync(objectId));
        }      
    }
}
 
