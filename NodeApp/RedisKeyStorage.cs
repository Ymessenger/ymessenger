using NodeApp.Interfaces;
using NodeApp.ViewModels;
using ObjectsLibrary.Converters;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp
{
    public class RedisKeyStorage : IKeyStorage
    {
        private ConnectionMultiplexer redis;
        private readonly IDatabase redisDb;
        private readonly ISubscriber subscriber;

        public RedisKeyStorage(CacheServerConnectionVm connection)
        {
            ConfigurationOptions options = new ConfigurationOptions
            {
                AllowAdmin = true,
                Password = connection.Password
            };
            options.EndPoints.Add(connection.Host, connection.Port);
            redis = ConnectionMultiplexer.Connect(options);
            redisDb = redis.GetDatabase();            
            subscriber = redis.GetSubscriber();
        }
        
        public T GetObjectByKey<T>(object keyValue)
        {
            try
            {
                return ObjectSerializer.ByteArrayToObject<T>(redisDb.StringGet(keyValue.ToString()));
            }
            catch
            {
                return default;
            }
        }

        public void SetObjectToStorage(object keyValue, object obj)
        {
            //redisDb.StringSet();
        }

        public void SubscribeToChanges(object keyValue, Action action)
        {
            throw new NotImplementedException();
        }
        
    }
}
