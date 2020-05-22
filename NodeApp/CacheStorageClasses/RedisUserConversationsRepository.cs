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
using ObjectsLibrary.Converters;
using ObjectsLibrary.ViewModels;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CacheStorageClasses
{
    public class RedisUserConversationsRepository : ICacheRepository<ConversationPreviewVm>
    {
        private readonly ConnectionMultiplexer redis;
        private readonly IDatabaseAsync redisDb;

        public RedisUserConversationsRepository(CacheServerConnectionInfo connection)
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

        public async Task<bool> AddObject(string key, ConversationPreviewVm conversation)
        {
            return await redisDb.ListRightPushAsync(key, ObjectSerializer.ObjectToByteArray(conversation)).ConfigureAwait(false) > 0;
        }

        public async Task<bool> AddObjects(string key, IEnumerable<ConversationPreviewVm> conversations)
        {
            RedisValue[] serialized = System.Array.Empty<RedisValue>();
            foreach (var conversation in conversations)
            {
                serialized = serialized.Append(ObjectSerializer.ObjectToByteArray(conversation)).ToArray();
            }
            return await redisDb.ListRightPushAsync(key, serialized).ConfigureAwait(false) > 0;
        }

        public async Task<long> GetCount(string key)
        {
            return await redisDb.ListLengthAsync(key).ConfigureAwait(false);
        }

        public async Task<ConversationPreviewVm> GetObject(string key)
        {
            byte[] array = (await redisDb.ListRangeAsync(key, 0, 0).ConfigureAwait(false)).FirstOrDefault();
            if (array == null)
            {
                return null;
            }

            return ObjectSerializer.ByteArrayToObject<ConversationPreviewVm>(array);
        }

        public async Task<IEnumerable<ConversationPreviewVm>> GetObjects(string key, long length)
        {
            RedisValue[] conversations = await redisDb.ListRangeAsync(key, 0, length).ConfigureAwait(false);
            if (conversations == null || !conversations.Any())
            {
                return null;
            }

            return conversations.Select(opt => ObjectSerializer.ByteArrayToObject<ConversationPreviewVm>(opt));
        }

        public async Task<bool> Remove(string key)
        {
            return await redisDb.KeyDeleteAsync(key).ConfigureAwait(false);
        }

        public async Task<bool> UpdateObject(string key, ConversationPreviewVm conversation)
        {
            RedisValue[] conversations = await redisDb.ListRangeAsync(key, -2, 0).ConfigureAwait(false);
            await redisDb.ListTrimAsync(key, 0, -2).ConfigureAwait(false);
            List<ConversationPreviewVm> userConversations = conversations.Select(opt => ObjectSerializer.ByteArrayToObject<ConversationPreviewVm>(opt)).ToList();
            RedisValue[] serializedChats = System.Array.Empty<RedisValue>();
            userConversations.ForEach(item =>
            {
                if (item.ConversationId == conversation.ConversationId)
                {
                    item = conversation;
                }

                serializedChats = serializedChats.Append(ObjectSerializer.ObjectToByteArray(item)).ToArray();
            });
            return await redisDb.ListRightPushAsync(key, serializedChats).ConfigureAwait(false) > 0;
        }

        public async Task<bool> UpdateObjects(string key, IEnumerable<ConversationPreviewVm> conversations)
        {
            await redisDb.KeyDeleteAsync(key, CommandFlags.HighPriority).ConfigureAwait(false);
            RedisValue[] serialized = System.Array.Empty<RedisValue>();
            foreach (var conversation in conversations)
            {
                serialized = serialized.Append(ObjectSerializer.ObjectToByteArray(conversation)).ToArray();
            }
            return await redisDb.ListRightPushAsync(key, serialized).ConfigureAwait(false) > 0;
        }
    }
}
