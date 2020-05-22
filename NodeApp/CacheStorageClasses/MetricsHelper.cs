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
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CacheStorageClasses
{
    public class MetricsHelper
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabaseAsync _redisDb;
        private static readonly Lazy<MetricsHelper> singleton = new Lazy<MetricsHelper>(() => new MetricsHelper(NodeSettings.Configs.CacheServerConnection));
        public static MetricsHelper Instance => singleton.Value;
        public MetricsHelper(CacheServerConnectionInfo connection)
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
            _redis = ConnectionMultiplexer.Connect(options);
            _redisDb = _redis.GetDatabase();
        }

        public async Task<bool> IsCrossNodeApiInvolvedAsync(long requestId)
        {
            var value = await _redisDb.SetMembersAsync(requestId.ToString()).ConfigureAwait(false);
            await _redisDb.KeyDeleteAsync(requestId.ToString(), CommandFlags.FireAndForget).ConfigureAwait(false);
            return !value?.FirstOrDefault().IsNullOrEmpty ?? false;
        }
        public async Task SetCrossNodeApiInvolvedAsync(long requestId)
        {
            try
            {
                await _redisDb.SetAddAsync(requestId.ToString(), true, CommandFlags.FireAndForget).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
    }
}
