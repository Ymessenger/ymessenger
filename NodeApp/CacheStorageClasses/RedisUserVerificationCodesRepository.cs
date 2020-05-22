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
using NodeApp.Objects;
using NodeApp.Objects.SettingsObjects;
using ObjectsLibrary.Converters;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.CacheStorageClasses
{
    public class RedisUserVerificationCodesRepository : ICacheRepository<VerificationCodeInfo>
    {
        private readonly ConnectionMultiplexer redis;
        private readonly IDatabaseAsync redisDb;
        private const short CODE_LIFETIME_SECONDS = 300;

        public RedisUserVerificationCodesRepository(CacheServerConnectionInfo connection)
        {
            ConfigurationOptions options = new ConfigurationOptions
            {
                AllowAdmin = true,
                Password = connection.Password,
                AbortOnConnectFail = false,
                ConnectTimeout = 5000,
                SyncTimeout = 5000
            };
            options.EndPoints.Add(connection.Host, connection.Port);
            redis = ConnectionMultiplexer.Connect(options);
            redisDb = redis.GetDatabase();
        }

        public async Task<bool> AddObject(string key, VerificationCodeInfo vCode)
        {
            return await redisDb.StringSetAsync(key, ObjectSerializer.ObjectToByteArray(vCode), expiry: TimeSpan.FromSeconds(CODE_LIFETIME_SECONDS)).ConfigureAwait(false);
        }

        public Task<bool> AddObjects(string key, IEnumerable<VerificationCodeInfo> vCodes)
        {
            throw new InvalidOperationException();
        }

        public async Task<long> GetCount(string key)
        {
            return await redisDb.KeyExistsAsync(key).ConfigureAwait(false) ? 1 : 0;
        }

        public async Task<VerificationCodeInfo> GetObject(string key)
        {
            byte[] array = await redisDb.StringGetAsync(key).ConfigureAwait(false);
            if (array == null)
            {
                return null;
            }

            return ObjectSerializer.ByteArrayToObject<VerificationCodeInfo>(array);
        }

        public Task<IEnumerable<VerificationCodeInfo>> GetObjects(string key, long length)
        {
            throw new InvalidOperationException();
        }

        public async Task<bool> Remove(string key)
        {
            return await redisDb.KeyDeleteAsync(key).ConfigureAwait(false);
        }

        public async Task<bool> UpdateObject(string key, VerificationCodeInfo vCode)
        {
            return await redisDb.StringSetAsync(key, ObjectSerializer.ObjectToByteArray(vCode)).ConfigureAwait(false);
        }

        public Task<bool> UpdateObjects(string key, IEnumerable<VerificationCodeInfo> vCodes)
        {
            throw new InvalidOperationException();
        }
    }
}
