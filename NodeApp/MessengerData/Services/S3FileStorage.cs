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
using Amazon.S3;
using Amazon.S3.Model;
using NodeApp.Helpers;
using NodeApp.Objects.SettingsObjects;
using ObjectsLibrary;
using ObjectsLibrary.Interfaces;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class S3FileStorage : IFileStorage
    {
        public string StorageName => "S3";
        private readonly IAmazonS3 _s3Client;
        private readonly S3FileStorageOptions _options;

        public S3FileStorage(S3FileStorageOptions options)
        {
            if (!options.IsValid())
            {
                throw new ArgumentNullException(nameof(options));
            }
            _options = options;
            AmazonS3Config config = new AmazonS3Config
            {
                ServiceURL = options.EndPoint,                
                HttpClientFactory = new AWSHttpClientFactory()
            };
            _s3Client = new AmazonS3Client(options.AccessKey, options.SecretKey, config);
        }
        public async Task<Stream> GetStreamAsync(string objectId)
        {
            var response = await _s3Client.GetObjectAsync(_options.BucketName, objectId).ConfigureAwait(false);
            return response.ResponseStream;
        }
        public async Task<List<string>> GetBucketsList()
        {
            List<string> buckets = new List<string>();
            var response = await _s3Client.ListBucketsAsync().ConfigureAwait(false);
            foreach (var bucket in response.Buckets)
            {
                buckets.Add(bucket.BucketName);
            }
            return buckets;
        }
        public async Task<bool> RemoveAsync(string objectId)
        {
            try
            {
                await _s3Client.DeleteObjectAsync(_options.BucketName, objectId).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return false;
            }
        }

        public async Task<List<FileInfoVm>> SearchAsync(string prefix)
        {
            var objectListReponse = await _s3Client.ListObjectsAsync(_options.BucketName, prefix).ConfigureAwait(false);
            return objectListReponse.S3Objects.Select(obj => new FileInfoVm
            {
                FileId = obj.Key,
                Filename = obj.Key,
                Size = obj.Size,
                Uploaded = obj.LastModified.ToUnixTime()
            }).OrderBy(opt => opt.Uploaded).ToList();
        }

        public async Task UploadAsync(Stream stream, string destination)
        {            
            PutObjectRequest request = new PutObjectRequest
            {
                InputStream = stream,
                BucketName = _options.BucketName,
                Key = destination,
                UseChunkEncoding = false
            };
            await _s3Client.PutObjectAsync(request).ConfigureAwait(false);           
        }
    }    
}