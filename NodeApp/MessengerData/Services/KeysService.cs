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
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using Npgsql;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.Entities;
using ObjectsLibrary.Blockchain.PublicDataEntities;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static NodeApp.HttpServer.Models.CreateNewKeysModel;

namespace NodeApp.MessengerData.Services
{
    public class KeysService : IKeysService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        private readonly INodeRequestSender nodeRequestSender;
        private readonly IConnectionsService connectionsService;
        public KeysService(IDbContextFactory<MessengerDbContext> contextFactory, INodeRequestSender nodeRequestSender, IConnectionsService connectionsService)
        {
            this.contextFactory = contextFactory;
            this.nodeRequestSender = nodeRequestSender;
            this.connectionsService = connectionsService;
        }

        public async Task<List<KeyVm>> DeleteUserKeysAsync(IEnumerable<long> keysId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var keysCondition = PredicateBuilder.New<Key>();
                keysCondition = keysId.Aggregate(keysCondition,
                    (current, value) => current.Or(opt => opt.KeyId == value && opt.UserId == userId).Expand());
                List<Key> keys = await context.Keys.Where(keysCondition).ToListAsync().ConfigureAwait(false);
                context.RemoveRange(keys);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return KeyConverter.GetKeysVm(keys);
            }
        }

        public async Task<KeyVm> GetUserKeyAsync(long publicKeyId, long userId, bool isSignKey)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                KeyType keyType = isSignKey ? KeyType.SignAsymKey : KeyType.EncryptionAsymKey;
                Key key = await context.Keys.FirstOrDefaultAsync(opt => opt.UserId == userId && opt.KeyId == publicKeyId && opt.Type == keyType).ConfigureAwait(false);
                KeyVm userKey = KeyConverter.GetKeyVm(key);
                if (userKey == null)
                {
                    using (BlockchainDbContext blockchainCtx = new BlockchainDbContext())
                    {

                        var userKeys = await blockchainCtx.BlockSegments
                            .Where(opt => opt.SegmentHeader.Type == ObjectsLibrary.Blockchain.BlockSegmentType.NewUserKeys && opt.SegmentHeader.Uid == userId)
                            .Where(opt => ((NewUserKeysBlockData)opt.PublicData).Keys.Any(p => p.KeyId == publicKeyId && p.Type == keyType))
                            .Select(opt => (NewUserKeysBlockData)opt.PublicData)
                            .FirstOrDefaultAsync()
                            .ConfigureAwait(false);
                        if (userKeys == null)
                        {
                            throw new ObjectDoesNotExistsException();
                        }
                        userKey = userKeys.Keys.FirstOrDefault(opt => opt.KeyId == publicKeyId);
                    }
                }
                return userKey;
            }
        }

        public async Task<List<KeyVm>> AddNewUserKeysAsync(IEnumerable<KeyVm> keys, long userId)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    var publicKeys = KeyConverter.GetKeys(keys);
                    publicKeys.ForEach(key => key.UserId = userId);
                    await context.Keys.AddRangeAsync(publicKeys).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return KeyConverter.GetKeysVm(publicKeys);
                }
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is PostgresException postgresException)
                {
                    if (postgresException.ConstraintName == "IX_Keys_KeyId_UserId")
                    {
                        throw new ObjectAlreadyExistsException();
                    }
                }
                throw new Exception("Error while saving key", ex);
            }
        }

        public async Task<List<KeyVm>> GetUserPublicKeysAsync(long userId, IEnumerable<long> keysId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var keysCondition = PredicateBuilder.New<Key>();
                keysCondition = keysId.Aggregate(keysCondition,
                    (current, value) => current.Or(opt => opt.KeyId == value && opt.UserId == userId).Expand());
                var publicKeys = await context.Keys.Where(keysCondition).ToListAsync().ConfigureAwait(false);
                return KeyConverter.GetKeysVm(publicKeys);
            }
        }

        public async Task<List<KeyVm>> GetUserPublicKeysAsync(long userId, long? time = 0, bool? direction = true)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                IQueryable<Key> query = context.Keys
                .Where(opt => opt.UserId == userId);
                if (direction == false)
                {
                    query = query.Where(opt => opt.GenerationTimeSeconds > time)
                        .OrderBy(opt => opt.GenerationTimeSeconds)
                        .ThenByDescending(opt => opt.ExpirationTimeSeconds);
                }
                else
                {
                    if (time == 0)
                    {
                        time = DateTime.UtcNow.ToUnixTime();
                    }

                    query = query.Where(opt => opt.GenerationTimeSeconds < time)
                        .OrderByDescending(opt => opt.GenerationTimeSeconds)
                        .ThenByDescending(opt => opt.ExpirationTimeSeconds);
                }
                return KeyConverter.GetKeysVm(await query.Take(30).ToListAsync().ConfigureAwait(false));
            }
        }

        public async Task<KeyVm> SetNewSymmetricKeyForChat(KeyVm key, long chatId, long userId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                ChatUser chatUser = await context.ChatUsers
                .Where(opt => opt.ChatId == chatId
                    && opt.UserId == userId
                    && opt.UserRole >= UserRole.Admin
                    && !opt.Deleted && !opt.Banned)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
                if (chatUser == null)
                {
                    throw new UserIsNotInConversationException();
                }

                Key chatKey = await context.Keys.FirstOrDefaultAsync(opt => opt.ChatId == chatId).ConfigureAwait(false);
                if (chatKey == null)
                {
                    chatKey = KeyConverter.GetKey(key);
                    chatKey.ChatId = chatId;
                    chatKey.UserId = null;
                    chatKey.KeyId = RandomExtensions.NextInt64();
                    await context.AddAsync(chatKey).ConfigureAwait(false);
                }
                else
                {
                    chatKey.ChatId = chatId;
                    chatKey.KeyData = key.Data;
                    chatKey.Version = 1;
                    chatKey.UserId = null;
                    chatKey.GenerationTimeSeconds = key.GenerationTime;
                    chatKey.ExpirationTimeSeconds = key.GenerationTime + key.Lifetime;
                    context.Update(chatKey);
                }
                await context.SaveChangesAsync().ConfigureAwait(false);
                return KeyConverter.GetKeyVm(chatKey);
            }
        }

        public async Task<NodeKeysDto> GetActualNodeKeysAsync(long? nodeId)
        {
            byte[] password;
            using (SHA256 sha256 = SHA256.Create())
            {
                password = sha256.ComputeHash(Encoding.UTF8.GetBytes(NodeSettings.Configs.Password));
            }
            using (MessengerDbContext context = contextFactory.Create())
            {
                var nodeKeys = await context.NodesKeys
                .OrderByDescending(opt => opt.GenerationTime)
                .ThenByDescending(opt => opt.ExpirationTime)
                .FirstOrDefaultAsync(opt => opt.NodeId == nodeId)
                .ConfigureAwait(false);
                if (nodeKeys == null)
                {
                    return null;
                }

                return new NodeKeysDto
                {
                    ExpirationTime = nodeKeys.ExpirationTime,
                    GenerationTime = nodeKeys.GenerationTime,
                    KeyId = nodeKeys.KeyId,
                    NodeId = nodeKeys.NodeId,
                    Password = password,
                    PrivateKey = nodeKeys.PrivateKey,
                    PublicKey = nodeKeys.PublicKey,
                    SymmetricKey = nodeKeys.SymmetricKey,
                    SignPublicKey = nodeKeys.SignPublicKey,
                    SignPrivateKey = nodeKeys.SignPrivateKey
                };
            }
        }

        public async Task<NodeKeysDto> SaveNodePublicKeyAsync(long nodeId, byte[] publicKey, long keyId)
        {
            if (nodeId == NodeSettings.Configs.Node.Id)
            {
                throw new InvalidOperationException();
            }
            using (MessengerDbContext context = contextFactory.Create())
            {
                var nodeKeys = await context.NodesKeys.FirstOrDefaultAsync(opt => opt.KeyId == keyId && opt.NodeId == nodeId).ConfigureAwait(false);
                if (nodeKeys == null)
                {
                    nodeKeys = new NodeKeys
                    {
                        KeyId = keyId,
                        PublicKey = publicKey,
                        NodeId = nodeId
                    };
                    await context.NodesKeys.AddAsync(nodeKeys).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
                return new NodeKeysDto
                {
                    NodeId = nodeId,
                    KeyId = keyId,
                    PublicKey = publicKey
                };
            }
        }

        public async Task<NodeKeysDto> CreateNewNodeKeysAsync(long? nodeId, KeyLength keyLength, uint lifetime)
        {
            byte[] password;
            using (SHA256 sha256 = SHA256.Create())
            {
                password = sha256.ComputeHash(Encoding.UTF8.GetBytes(NodeSettings.Configs.Password));
            }
            long keyId = RandomExtensions.NextInt64();
            using (MessengerDbContext context = contextFactory.Create())
            {
                var nodeKeysId = await context.NodesKeys
                .Where(opt => opt.NodeId == nodeId)
                .Select(opt => opt.KeyId)
                .ToListAsync()
                .ConfigureAwait(false);
                while (nodeKeysId.Contains(keyId))
                {
                    keyId = RandomExtensions.NextInt64();
                }
                byte[] symmetricKey = Encryptor.GetSymmetricKey(256, keyId, lifetime, password);
                Encryptor.KeyLengthType encKeysLength;
                Encryptor.KeyLengthType signKeysLength;
                switch (keyLength)
                {
                    case KeyLength.Short:
                        encKeysLength = Encryptor.KeyLengthType.EncryptShort;
                        signKeysLength = Encryptor.KeyLengthType.SignShort;
                        break;
                    case KeyLength.Medium:
                        encKeysLength = Encryptor.KeyLengthType.EncryptMedium;
                        signKeysLength = Encryptor.KeyLengthType.SignMedium;
                        break;
                    case KeyLength.Long:
                    default:
                        encKeysLength = Encryptor.KeyLengthType.EncryptLong;
                        signKeysLength = Encryptor.KeyLengthType.SignLong;
                        break;
                }
                var asymKeys = Encryptor.GenerateAsymmetricKeys(keyId, lifetime, encKeysLength, password);
                var signAsymKeys = Encryptor.GenerateAsymmetricKeys(keyId, lifetime, signKeysLength, password);
                long generationTime = DateTime.UtcNow.ToUnixTime();
                NodeKeys nodeKeys = new NodeKeys
                {
                    GenerationTime = generationTime,
                    ExpirationTime = generationTime + lifetime,
                    KeyId = keyId,
                    NodeId = nodeId.GetValueOrDefault(),
                    PublicKey = asymKeys.FirstValue,
                    PrivateKey = asymKeys.SecondValue,
                    SymmetricKey = symmetricKey,
                    SignPublicKey = signAsymKeys.FirstValue,
                    SignPrivateKey = signAsymKeys.SecondValue
                };
                await context.AddAsync(nodeKeys).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return new NodeKeysDto
                {
                    ExpirationTime = nodeKeys.ExpirationTime,
                    GenerationTime = nodeKeys.GenerationTime,
                    KeyId = nodeKeys.KeyId,
                    NodeId = nodeKeys.NodeId,
                    PrivateKey = nodeKeys.PrivateKey,
                    PublicKey = nodeKeys.PublicKey,
                    SymmetricKey = nodeKeys.SymmetricKey,
                    SignPublicKey = nodeKeys.SignPublicKey,
                    SignPrivateKey = nodeKeys.SignPrivateKey,
                    Password = password
                };
            }
        }

        public async Task<NodeKeysDto> GetNodeKeysAsync(long keyId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                NodeKeys nodeKeys = await context.NodesKeys.FirstOrDefaultAsync(keys => keys.KeyId == keyId).ConfigureAwait(false);
                if (nodeKeys != null)
                {
                    return new NodeKeysDto
                    {
                        ExpirationTime = nodeKeys.ExpirationTime,
                        GenerationTime = nodeKeys.GenerationTime,
                        KeyId = nodeKeys.KeyId,
                        NodeId = nodeKeys.NodeId,
                        PrivateKey = nodeKeys.PrivateKey,
                        PublicKey = nodeKeys.PublicKey,
                        SignPrivateKey = nodeKeys.SignPrivateKey,
                        SignPublicKey = nodeKeys.SignPublicKey,
                        SymmetricKey = nodeKeys.SymmetricKey
                    };
                }

                return null;
            }
        }

        public async Task<List<NodeKeysDto>> ReencryptNodeKeysAsync(long nodeId, string oldPassword, string newPassword)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var nodeKeys = await context.NodesKeys.Where(key => key.NodeId == nodeId).ToListAsync().ConfigureAwait(false);
                foreach (var key in nodeKeys)
                {
                    key.PrivateKey = Encryptor.ReencryptKey(key.PrivateKey, oldPassword, newPassword);
                    key.SignPrivateKey = Encryptor.ReencryptKey(key.SignPrivateKey, oldPassword, newPassword);
                    key.SymmetricKey = Encryptor.ReencryptKey(key.SymmetricKey, oldPassword, newPassword);
                }
                await context.SaveChangesAsync().ConfigureAwait(false);
                return nodeKeys.Select(key => new NodeKeysDto
                {
                    ExpirationTime = key.ExpirationTime,
                    GenerationTime = key.GenerationTime,
                    KeyId = key.KeyId,
                    NodeId = key.NodeId,
                    PublicKey = key.PublicKey,
                    PrivateKey = key.PrivateKey,
                    Password = Encryptor.GetPassword(newPassword),
                    SignPrivateKey = key.SignPrivateKey,
                    SignPublicKey = key.SignPublicKey,
                    SymmetricKey = key.SymmetricKey
                }).ToList();
            }
        }
        public async Task<NodeKeysDto> GetNodeKeysAsync(long nodeId, long keyId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var nodeKey = await context.NodesKeys
                    .FirstOrDefaultAsync(key => key.KeyId == keyId && key.NodeId == nodeId)
                    .ConfigureAwait(false);
                if (nodeKey != null)
                {
                    return new NodeKeysDto
                    {
                        ExpirationTime = nodeKey.ExpirationTime,
                        GenerationTime = nodeKey.GenerationTime,
                        KeyId = nodeKey.KeyId,
                        NodeId = nodeKey.NodeId,                        
                        PublicKey = nodeKey.PublicKey,                        
                        SignPublicKey = nodeKey.SignPublicKey                        
                    };
                }
                if (nodeId != NodeSettings.Configs.Node.Id)
                {
                    var connection = connectionsService.GetNodeConnection(nodeId);
                    if (connection != null)
                    {
                        var loadedKey = await nodeRequestSender.GetNodePublicKeyAsync(connection, keyId).ConfigureAwait(false);
                        if(loadedKey == null)
                        {
                            return null;
                        }
                        var newKey = new NodeKeys
                        {
                            ExpirationTime = loadedKey.ExpirationTime,
                            GenerationTime = loadedKey.GenerationTime,
                            KeyId = loadedKey.KeyId,
                            NodeId = loadedKey.NodeId,
                            PublicKey = loadedKey.PublicKey,
                            SignPublicKey = loadedKey.SignPublicKey
                        };
                        await context.NodesKeys.AddAsync(newKey).ConfigureAwait(false);
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        return loadedKey;
                    }
                }
            }
            return null;
        }
    }
}