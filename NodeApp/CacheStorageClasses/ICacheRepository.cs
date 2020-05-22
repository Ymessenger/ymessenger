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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.CacheStorageClasses
{
    public interface ICacheRepository<T>
       where T : class
    {
        Task<T> GetObject(string key);
        Task<IEnumerable<T>> GetObjects(string key, long length);
        Task<bool> AddObjects(string key, IEnumerable<T> objects);
        Task<bool> AddObject(string key, T obj);
        Task<bool> UpdateObject(string key, T obj);
        Task<bool> UpdateObjects(string key, IEnumerable<T> objects);
        Task<long> GetCount(string key);
        Task<bool> Remove(string key);
    }
}
