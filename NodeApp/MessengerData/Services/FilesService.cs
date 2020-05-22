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
using LinqKit;
using Microsoft.EntityFrameworkCore;
using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class FilesService : IFilesService
    {
        private readonly IPoolsService poolsService;
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public FilesService(IAppServiceProvider appServiceProvider, IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.poolsService = appServiceProvider.PoolsService;
            this.contextFactory = contextFactory;
        }        
        public async Task<FileInfo> GetFileInfoAsync(string fileId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var fileInfo = await context.FilesInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(file => file.Id == fileId && file.Deleted == false)
                .ConfigureAwait(false);
                return fileInfo;
            }
        }        
        public async Task<FileInfoVm> SaveFileAsync(long? userId, string filename, string url, long fileSize, byte[] fileHash, string storageType, ImageMetadata imageMetadata = null)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                FileInfo newFile = new FileInfo
                {
                    Id = RandomExtensions.NextString(64),
                    FileName = filename,
                    Url = url,
                    UploaderId = userId,
                    UploadDate = DateTime.UtcNow.ToUnixTime(),
                    NodeId = NodeSettings.Configs.Node.Id,
                    Size = fileSize,
                    Hash = fileHash,
                    Storage = storageType,
                    NumericId = await poolsService.GetFileIdAsync().ConfigureAwait(false),
                    ImageMetadata = imageMetadata
                };
                await context.FilesInfo.AddAsync(newFile).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return FileInfoConverter.GetFileInfoVm(newFile, false);
            }
        }        
        public async Task<FileInfoVm> SaveFileAsync(FileInfoVm fileInfo, long userId, string url = null)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    FileInfo newFile = new FileInfo(fileInfo, url, userId);
                    await context.FilesInfo.AddAsync(newFile).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return FileInfoConverter.GetFileInfoVm(newFile);
                }
            }
            catch (Exception ex)
            {
                throw new SaveFileInformationException("Failed to save file information", ex);
            }
        }        
        public async Task<List<FileInfoVm>> GetFilesInfoAsync(DateTime navigationTime, long? uploaderId = null, int limit = 100)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                IQueryable<FileInfo> query;
                if (uploaderId == null)
                {
                    query = from file in context.FilesInfo
                            .Where(fileInfo => fileInfo.Deleted == false)
                            select file;
                }
                else
                {
                    query = from file in context.FilesInfo
                            .Where(fileInfo => fileInfo.Deleted == false)
                            where file.UploaderId == uploaderId
                            select file;
                }
                List<FileInfo> filesInfo = await query
                        .AsNoTracking()
                        .Take(limit)
                        .OrderBy(file => file.UploadDate)
                        .Where(file => file.UploadDate >= navigationTime.ToUnixTime())
                        .ToListAsync()
                        .ConfigureAwait(false);
                return FileInfoConverter.GetFilesInfoVm(filesInfo, uploaderId != null);
            }
        }        
        public async Task<List<FileInfoVm>> GetFilesInfoAsync(List<string> filesId, int limit = 100)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var filesCondition = PredicateBuilder.New<FileInfo>();
                filesCondition = filesId.Aggregate(filesCondition,
                    (current, value) => current.Or(file => file.Id == value).Expand());
                List<FileInfo> filesInfo = await context.FilesInfo
                    .AsNoTracking()
                    .Where(filesCondition)
                    .Where(file => file.Deleted == false)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return FileInfoConverter.GetFilesInfoVm(filesInfo);
            }
        }        
        public async Task<List<long>> DeleteFilesAsync(IEnumerable<string> filesId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var filesCondition = PredicateBuilder.New<FileInfo>();
                filesCondition = filesId.Aggregate(filesCondition,
                    (current, value) => current.Or(file => file.Id == value).Expand());
                List<FileInfo> filesInfo = await context.FilesInfo
                    .Where(filesCondition)
                    .Where(file => file.UploaderId == userId && file.Deleted == false)
                    .ToListAsync()
                    .ConfigureAwait(false);
                if (filesInfo.Count < filesId.Count())
                {
                    throw new ObjectDoesNotExistsException();
                }

                filesInfo.ForEach(fileInfo => fileInfo.Deleted = true);
                context.UpdateRange(filesInfo);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return filesInfo.Select(file => file.NumericId).ToList();
            }
        }       
        public async Task<List<long>> DeleteFilesAsync(IEnumerable<long> filesId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var filesCondition = PredicateBuilder.New<FileInfo>();
                filesCondition = filesId.Aggregate(filesCondition,
                    (current, value) => current.Or(file => file.NumericId == value).Expand());
                List<FileInfo> filesInfo = await context.FilesInfo
                      .Where(filesCondition)
                      .Where(file => file.Deleted == false)
                      .ToListAsync().ConfigureAwait(false);
                if (filesInfo.Count() < filesId.Count())
                {
                    throw new WrongArgumentException();
                }

                filesInfo.ForEach(fileInfo => fileInfo.Deleted = true);
                context.UpdateRange(filesInfo);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return filesInfo.Select(file => file.NumericId).ToList();
            }
        }
        
        public async Task<FileInfoVm> UpdateFileInformationAsync(string fileName, string fileId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                FileInfo fileInfo = await context.FilesInfo.FindAsync(fileId).ConfigureAwait(false);
                fileInfo.Url = fileName;
                context.Update(fileInfo);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return FileInfoConverter.GetFileInfoVm(fileInfo);
            }
        }

        public async Task<List<FileInfoVm>> CreateFilesInformationAsync(List<FileInfoVm> filesInfo)
        {
            var filesCondition = PredicateBuilder.New<FileInfo>();
            filesCondition = filesInfo.Aggregate(filesCondition,
                (current, value) => current.Or(opt => opt.Id == value.FileId).Expand());
            using (MessengerDbContext context = contextFactory.Create())
            {
                List<FileInfo> existingFiles = await context.FilesInfo
                    .Where(filesCondition)
                    .ToListAsync()
                    .ConfigureAwait(false);
                List<FileInfoVm> nonExistingFiles = filesInfo.Where(fileVm => !existingFiles.Any(file => file.Id == fileVm.FileId))?.ToList();
                if (!nonExistingFiles.IsNullOrEmpty())
                {
                    List<FileInfo> newFiles = FileInfoConverter.GetFilesInfo(nonExistingFiles);
                    await context.FilesInfo.AddRangeAsync(newFiles).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            return filesInfo;
        }
    }
}