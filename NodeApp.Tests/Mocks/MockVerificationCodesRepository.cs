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
using NodeApp.CacheStorageClasses;
using NodeApp.Objects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.Tests.Mocks
{
    public class MockVerificationCodesRepository : ICacheRepository<VerificationCodeInfo>
    {
        private readonly ConcurrentDictionary<string, VerificationCodeInfo> _storage;
        public MockVerificationCodesRepository()
        {
            _storage = new ConcurrentDictionary<string, VerificationCodeInfo>();
        }
        public async Task<bool> AddObject(string key, VerificationCodeInfo obj)
        {
            try
            {
                if (_storage.TryGetValue(key, out _))
                {
                    return await UpdateObject(key, obj);                    
                }
                _storage.GetOrAdd(key, (k) =>
                {
                    return obj;
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<bool> AddObjects(string key, IEnumerable<VerificationCodeInfo> objects)
        {
            throw new NotSupportedException();
        }

        public async Task<long> GetCount(string key)
        {
            if (_storage.TryGetValue(key, out _))
                return 1;
            return 0;
        }

        public async Task<VerificationCodeInfo> GetObject(string key)
        {
            if (_storage.TryGetValue(key, out VerificationCodeInfo verificationCodeInfo))
                return verificationCodeInfo;
            return null;
        }

        public async Task<IEnumerable<VerificationCodeInfo>> GetObjects(string key, long length)
        {
            throw new NotSupportedException();
        }

        public async Task<bool> Remove(string key)
        {
            if (_storage.TryRemove(key, out _))
                return true;
            return false;
        }

        public async Task<bool> UpdateObject(string key, VerificationCodeInfo obj)
        {
            if (_storage.TryGetValue(key, out var value))
                if (_storage.TryUpdate(key, obj, value))
                    return true;
            return false;
        }

        public Task<bool> UpdateObjects(string key, IEnumerable<VerificationCodeInfo> objects)
        {
            throw new NotSupportedException();
        }
    }
}