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
using NodeApp.Objects.SettingsObjects;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Converters;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CacheStorageClasses
{
    public class RedisBlockSegmentRepository : ICacheRepository<BlockSegmentVm>
    {
        private readonly ConnectionMultiplexer redis;
        private readonly IDatabaseAsync redisDb;

        public RedisBlockSegmentRepository(CacheServerConnectionInfo connection)
        {
            ConfigurationOptions options = new ConfigurationOptions
            {
                AllowAdmin = true,
                Password = connection.Password,
                AbortOnConnectFail = false,
                SyncTimeout = 5000,
                ConnectTimeout = 5000
            };
            options.EndPoints.Add(connection.Host, connection.Port);
            redis = ConnectionMultiplexer.Connect(options);
            redisDb = redis.GetDatabase();
        }

        public async Task<bool> AddObject(string key, BlockSegmentVm segment)
        {
            return await redisDb.ListRightPushAsync(key, ObjectSerializer.ObjectToByteArray(segment)).ConfigureAwait(false) > 0;
        }

        public async Task<bool> AddObjects(string key, IEnumerable<BlockSegmentVm> segments)
        {
            RedisValue[] serializedSegments = Array.Empty<RedisValue>();
            foreach (var segment in segments)
            {
                serializedSegments = serializedSegments.Append(ObjectSerializer.ObjectToByteArray(segment)).ToArray();
            }
            return await redisDb.ListRightPushAsync(key, serializedSegments).ConfigureAwait(false) > 0;
        }

        public async Task<long> GetCount(string key)
        {
            return await redisDb.ListLengthAsync(key).ConfigureAwait(false);
        }

        public async Task<BlockSegmentVm> GetObject(string key)
        {
            byte[] array = await redisDb.ListLeftPopAsync(key).ConfigureAwait(false);
            return ObjectSerializer.ByteArrayToObject<BlockSegmentVm>(array);
        }

        public async Task<IEnumerable<BlockSegmentVm>> GetObjects(string key, long length)
        {
            List<BlockSegmentVm> result = new List<BlockSegmentVm>();
            RedisValue[] segments = await redisDb.ListRangeAsync(key, 0, length - 1).ConfigureAwait(false);
            await redisDb.ListTrimAsync(key, length, -1).ConfigureAwait(false);

            foreach (RedisValue serializedSegment in segments)
            {
                result.Add(ObjectSerializer.ByteArrayToObject<BlockSegmentVm>(serializedSegment));
            }
            return result;
        }

        public async Task<bool> Remove(string key)
        {
            return await redisDb.KeyDeleteAsync(key, CommandFlags.HighPriority).ConfigureAwait(false);
        }

        public Task<bool> UpdateObject(string key, BlockSegmentVm segment)
        {
            throw new InvalidOperationException();
        }

        public async Task<bool> UpdateObjects(string key, IEnumerable<BlockSegmentVm> segments)
        {
            await Remove(key).ConfigureAwait(false);
            return await AddObjects(key, segments).ConfigureAwait(false);
        }
    }
}
