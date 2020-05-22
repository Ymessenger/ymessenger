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
using NodeApp.Interfaces;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using NodeApp.MessengerData.Services;
using ObjectsLibrary.Blockchain;
using ObjectsLibrary.Blockchain.PrivateDataLocalEntities;
using ObjectsLibrary.Blockchain.PublicDataEntities;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeApp.Blockchain
{
    public class BlockchainDataRestorer : IBlockchainDataRestorer
    {
        public List<KeyValuePair<string, string>> Errors { get; private set; }
        private readonly IKeysService _keysService;
        private MessengerDbContext CreateContext()
        {
            DbContextOptionsBuilder<MessengerDbContext> optionsBuilder = new DbContextOptionsBuilder<MessengerDbContext>()
                .UseNpgsql(NodeSettings.Configs.MessengerDbConnection.ToString());
            return new MessengerDbContext(optionsBuilder.Options);
        }
        public BlockchainDataRestorer(KeysService keysService)
        {
            _keysService = keysService;
        }

        public BlockchainDataRestorer()
        {
            Errors = new List<KeyValuePair<string, string>>();
            _keysService = AppServiceProvider.Instance.KeysService;
        }
        public async Task SaveBlockDataAsync(BlockVm block)
        {
            foreach (var segment in block.BlockSegments.OrderBy(opt => opt.SegmentHeader.BlockSegmentType))
            {
                await SaveBlockSegmentDataAsync(segment).ConfigureAwait(false);
            }
        }
        public async Task SaveBlockSegmentsDataAsync(IEnumerable<BlockSegmentVm> segments)
        {
            foreach (var segment in segments)
            {
                await SaveBlockSegmentDataAsync(segment).ConfigureAwait(false);
            }
        }
        private async Task SaveBlockSegmentDataAsync(BlockSegmentVm segment)
        {
            if (segment.PublicData == null && segment.PrivateData == null)
            {
                return;
            }

            switch (segment.SegmentHeader.BlockSegmentType)
            {
                case BlockSegmentType.NewUser:
                    await HandleNewUserBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.EditUser:
                    await HandleEditUserBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.DeleteUser:
                    await HandleDeleteUserBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.UsersAddedToUserBlacklist:
                    await HandleAddedToBlacklistBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.UsersRemovedFromUserBlacklist:
                    await HandleRemovedFromBlacklistBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.UserNodeChanged:
                    await HandleNodeChangedBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.NewNode:
                    await HandleNewNodeBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.EditNode:
                    await HandleEditNodeBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.DeleteNode:
                    await HandleRemoveNodeBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.NewKeysNode:
                    await HandleNewKeysNodeBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.NewFile:
                    await HandleNewFileBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.DeleteFile:
                    await HandleDeleteFileBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.NewChat:
                    await HandleNewChatBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.EditChat:
                    await HandleEditChatBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.DeleteChat:
                    await HandleDeleteChatBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.AddUsersChats:
                    await HandleAddUserChatsBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.ChangeUsersChat:
                    await HandleChangeUsersChatSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.NewPrivateChat:
                    await HandlePrivateChatSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.EditPrivateChat:
                    await HandlePrivateChatSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.DeletePrivateChat:
                    await HandleDeleteChatBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.NewUserKeys:
                    await HandleNewUserKeysBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.DeleteUserKeys:
                    await HandleDeleteUserKeyBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.Channel:
                    await HandleChannelBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.DeleteChannel:
                    await HandleDeleteChannelBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
                case BlockSegmentType.ChannelUsers:
                    await HandleChannelUsersBlockSegmentAsync(segment).ConfigureAwait(false);
                    break;
            }
        }

        private bool TryDecryptPrivateData<T>(BlockSegmentVm segment, out T decryptedObject)
        {
            if (segment.PrivateData != null && segment.KeyId != null && segment.NodeId == NodeSettings.Configs.Node.Id)
            {
                try
                {
                    NodeKeysDto nodeKeys = null;
                    if (Convert.ToInt64(segment.KeyId) != NodeData.Instance.NodeKeys.KeyId)
                    {
                        nodeKeys = _keysService.GetNodeKeysAsync(Convert.ToInt64(segment.KeyId)).Result;
                    }
                    else
                    {
                        nodeKeys = NodeData.Instance.NodeKeys;
                    }

                    if (nodeKeys != null)
                    {
                        byte[] password = NodeData.Instance.NodeKeys.Password;
                        var decryptedData = Encryptor.SymmetricDataDecrypt(
                            segment.PrivateData,
                            nodeKeys.PublicKey,
                            nodeKeys.SymmetricKey,
                            password).DecryptedData;
                        decryptedObject = ObjectSerializer.JsonToObject<T>(Encoding.UTF8.GetString(decryptedData), new BitArrayJsonConverter());
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex);
                    decryptedObject = default;
                    return false;
                }
            }
            decryptedObject = default;
            return false;
        }

        private void AddErrorMessage(string errorMethodName, string errorText)
        {
            Errors.Add(new KeyValuePair<string, string>(errorMethodName, errorText));
        }
        private async Task HandleNewUserBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        if (!await _context.Users.AnyAsync(user => user.Id == segment.SegmentHeader.ObjectId).ConfigureAwait(false))
                        {
                            if (TryDecryptPrivateData<UserVm>(segment, out var userData))
                            {
                                User user = UserConverter.GetUser(UserConverter.GetUserDto(userData));
                                await _context.Users.AddAsync(user).ConfigureAwait(false);
                            }
                            else
                            {
                                await _context.AddAsync(new User
                                {
                                    Id = segment.SegmentHeader.ObjectId,
                                    NodeId = segment.NodeId,
                                    Confirmed = true
                                }).ConfigureAwait(false);
                            }
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleNewUserBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }
        private async Task HandleEditUserBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(_user => _user.Id == segment.SegmentHeader.ObjectId).ConfigureAwait(false);
                        if (user != null)
                        {
                            if (TryDecryptPrivateData<UserVm>(segment, out var userData))
                            {
                                user = UserConverter.GetUser(UserConverter.GetUserDto(userData));
                                user.Confirmed = true;
                                _context.Users.Update(user);
                            }
                        }
                        else
                        {
                            if (TryDecryptPrivateData<UserVm>(segment, out var userData))
                            {
                                user = UserConverter.GetUser(UserConverter.GetUserDto(userData));
                            }
                            else
                            {
                                user = new User
                                {
                                    Id = segment.SegmentHeader.ObjectId,
                                    NodeId = segment.NodeId,
                                    Confirmed = true
                                };
                            }
                            await _context.Users.AddAsync(user).ConfigureAwait(false);
                        }
                        await _context.SaveChangesAsync().ConfigureAwait(false);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleNewUserBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }
        private async Task HandleDeleteUserBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(_user => _user.Id == segment.SegmentHeader.ObjectId).ConfigureAwait(false);
                        if (user == null)
                        {
                            await _context.Users.AddAsync(new User
                            {
                                Deleted = true,
                                Id = segment.SegmentHeader.ObjectId,
                                NodeId = segment.NodeId
                            }).ConfigureAwait(false);
                        }
                        else
                        {
                            if (!user.Deleted)
                            {
                                user.Deleted = true;
                                _context.Users.Update(user);
                            }
                        }
                        await _context.SaveChangesAsync().ConfigureAwait(false);
                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleDeleteUserBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }
        private async Task HandleAddedToBlacklistBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        if (TryDecryptPrivateData<UsersAddedToUserBlacklistBlockData>(segment, out var blockData))
                        {
                            var usersCondition = PredicateBuilder.New<User>();
                            usersCondition = blockData.UsersId.Aggregate(usersCondition,
                                (current, value) => current.Or(user => user.Id == value).Expand());
                            var usersIds = await _context.Users.Where(usersCondition).Select(user => user.Id).ToListAsync().ConfigureAwait(false);
                            var userBlockedIds = (await _context.Users
                                .Include(user => user.BlackList)
                                .Where(user => user.Id == blockData.UserId)
                                .Select(user => user.BlackList)
                                .FirstOrDefaultAsync().ConfigureAwait(false))
                                ?.Select(opt => opt.BadUid);
                            List<long> newBlockedUsersIds = new List<long>();
                            if (userBlockedIds != null)
                            {
                                newBlockedUsersIds = usersIds.Except(userBlockedIds).ToList();
                            }
                            else
                            {
                                newBlockedUsersIds = usersIds;
                            }

                            await _context.BadUsers.AddRangeAsync(newBlockedUsersIds.Select(id => new BadUser
                            {
                                BadUid = id,
                                Uid = blockData.UserId
                            })).ConfigureAwait(false);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleAddedToBlacklistBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }
        private async Task HandleRemovedFromBlacklistBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        if (TryDecryptPrivateData<UsersRemovedFromUserBlacklistBlockData>(segment, out var blockData))
                        {
                            var blockedCondition = PredicateBuilder.New<BadUser>();
                            blockedCondition = blockData.UsersId.Aggregate(blockedCondition,
                                (current, value) => current.Or(blocked => blocked.BadUid == value && blocked.Uid == blockData.UserId).Expand());
                            List<BadUser> badUsers = await _context.BadUsers.Where(blockedCondition).ToListAsync().ConfigureAwait(false);
                            _context.BadUsers.RemoveRange(badUsers);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleRemovedFromBlacklistBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleNodeChangedBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        UserNodeChangedBlockData blockData = (UserNodeChangedBlockData)segment.PublicData;
                        var user = await _context.Users.FindAsync(blockData.UserId).ConfigureAwait(false);
                        if (user != null)
                        {
                            user.NodeId = blockData.NodeId;
                            _context.Users.Update(user);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleNodeChangedBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }
        private async Task HandleNewNodeBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        NewNodeBlockData blockData = (NewNodeBlockData)segment.PublicData;
                        var newNode = NodeConverter.GetNode(blockData.Node);
                        if (!await _context.Nodes.AnyAsync(node => node.Id == newNode.Id).ConfigureAwait(false))
                        {
                            await _context.Nodes.AddAsync(newNode).ConfigureAwait(false);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleNewNodeBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleEditNodeBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        EditNodeBlockData blockData = (EditNodeBlockData)segment.PublicData;
                        var editedNode = NodeConverter.GetNode(blockData.Node);
                        if (await _context.Nodes.AnyAsync(node => node.Id == editedNode.Id).ConfigureAwait(false))
                        {
                            _context.Nodes.Attach(editedNode);
                            _context.Nodes.Update(editedNode);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleEditNodeBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleRemoveNodeBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        DeleteNodeBlockData blockData = (DeleteNodeBlockData)segment.PublicData;
                        var deletedNode = await _context.Nodes
                            .Include(node => node.DomainNodes)
                            .FirstOrDefaultAsync(node => node.Id == segment.NodeId).ConfigureAwait(false);
                        if (deletedNode != null)
                        {
                            deletedNode.DomainNodes = null;
                            _context.Nodes.Update(deletedNode);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleRemoveNodeBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleNewKeysNodeBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        NewKeysNodeBlockData blockData = (NewKeysNodeBlockData)segment.PublicData;
                        NodeKeys nodeKeys = new NodeKeys
                        {
                            GenerationTime = blockData.NodeKey.GenerationTime,
                            ExpirationTime = blockData.NodeKey.Lifetime != null
                                ? blockData.NodeKey.GenerationTime + blockData.NodeKey.Lifetime.Value
                                : long.MaxValue,
                            KeyId = blockData.NodeKey.KeyId,
                            NodeId = segment.NodeId,
                            PublicKey = blockData.NodeKey.Type == KeyType.EncryptionAsymKey
                                ? blockData.NodeKey.Data
                                : null,
                            SignPublicKey = blockData.NodeKey.Type == KeyType.SignAsymKey
                                ? blockData.NodeKey.Data
                                : null,
                        };
                        if (!await _context.NodesKeys.AnyAsync(key => key.KeyId == nodeKeys.KeyId && key.NodeId == nodeKeys.NodeId).ConfigureAwait(false))
                        {
                            await _context.NodesKeys.AddRangeAsync(nodeKeys).ConfigureAwait(false);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleNewKeysNodeBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleNewFileBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        if (TryDecryptPrivateData<FilePrivateData>(segment, out var filePrivateData))
                        {
                            var fileInfo = _context.FilesInfo
                                .FirstOrDefaultAsync(file => file.Id == filePrivateData.File.FileId && file.NumericId == file.NumericId);
                            if (fileInfo == null)
                            {
                                await _context.FilesInfo.AddAsync(new FileInfo(
                                    filePrivateData.File,
                                    filePrivateData.File.Url,
                                    filePrivateData.File.UploaderId.GetValueOrDefault())).ConfigureAwait(false);
                                await _context.SaveChangesAsync().ConfigureAwait(false);
                                transaction.Commit();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleNewFileBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleDeleteFileBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        DeleteFileBlockData blockData = (DeleteFileBlockData)segment.PublicData;
                        var fileInfo = await _context.FilesInfo.FirstOrDefaultAsync(file => file.NumericId == blockData.FileId).ConfigureAwait(false);
                        if (fileInfo != null)
                        {
                            fileInfo.Deleted = true;
                            _context.FilesInfo.Update(fileInfo);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleDeleteFileBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();

                    }
                }
            }
        }

        private async Task HandleNewChatBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        NewChatBlockData newChatBlockData = (NewChatBlockData)segment.PublicData;
                        if (!await _context.Chats.AnyAsync(chat => chat.Id == newChatBlockData.Chat.Id).ConfigureAwait(false))
                        {
                            Chat newChat = ChatConverter.GetChat(newChatBlockData.Chat);
                            await _context.Chats.AddAsync(newChat).ConfigureAwait(false);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleNewChatBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleEditChatBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        EditChatBlockData blockData = (EditChatBlockData)segment.PublicData;
                        var editedChat = await _context.Chats.FirstOrDefaultAsync(chat => chat.Id == blockData.Chat.Id).ConfigureAwait(false);
                        if (editedChat != null)
                        {
                            editedChat = ChatConverter.GetChat(editedChat, ChatConverter.GetChatDto(blockData.Chat));
                            _context.Chats.Update(editedChat);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleEditChatBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }
        private async Task HandleDeleteChatBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        DeleteChatBlockData blockData = (DeleteChatBlockData)segment.PublicData;
                        if (NodeSettings.Configs.Node.Id == blockData.NodeId)
                        {
                            var deletedChat = await _context.Chats.FirstOrDefaultAsync(chat => chat.Id == blockData.ChatId).ConfigureAwait(false);
                            if (deletedChat != null)
                            {
                                deletedChat.Deleted = true;
                                _context.Chats.Update(deletedChat);
                                await _context.SaveChangesAsync().ConfigureAwait(false);
                                transaction.Commit();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleDeleteChatBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleAddUserChatsBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        if (TryDecryptPrivateData<AddUsersChatBlockData>(segment, out var blockData))
                        {
                            var usersCondition = PredicateBuilder.New<ChatUser>();
                            usersCondition = blockData.UsersId.Aggregate(usersCondition,
                                (current, value) => current.Or(chatUser => chatUser.UserId == value && chatUser.ChatId == blockData.ChatId).Expand());
                            var existingChatUsers = await _context.ChatUsers.Where(usersCondition).ToListAsync().ConfigureAwait(false);
                            var newChatUsers = blockData.UsersId
                                .Where(id => !existingChatUsers.Any(chatUser => chatUser.UserId == id))
                                .Select(id => new ChatUser
                                {
                                    ChatId = blockData.ChatId,
                                    UserId = id,
                                    InviterId = blockData.RequestedUserId,
                                    UserRole = UserRole.User,
                                    Banned = false,
                                    Deleted = false
                                });
                            if (newChatUsers.Any())
                            {
                                await _context.ChatUsers.AddRangeAsync(newChatUsers).ConfigureAwait(false);
                                await _context.SaveChangesAsync().ConfigureAwait(false);
                                transaction.Commit();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleAddUserChatsBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleChangeUsersChatSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        if (TryDecryptPrivateData<ChangeUsersChatBlockData>(segment, out var blockData))
                        {
                            var usersCondition = PredicateBuilder.New<ChatUser>();
                            usersCondition = blockData.ChatUsers.Aggregate(usersCondition,
                                (current, value) => current.Or(chatUser => chatUser.UserId == value.UserId && chatUser.ChatId == blockData.ChatId).Expand());
                            var existingChatUsers = await _context.ChatUsers.Where(usersCondition).ToListAsync().ConfigureAwait(false);
                            var nonExistingChatUsers = blockData.ChatUsers
                                .Where(chatUser => !existingChatUsers.Any(opt => chatUser.UserId == opt.UserId)).ToList();
                            if (nonExistingChatUsers.Any())
                            {
                                await _context.ChatUsers.AddRangeAsync(ChatUserConverter.GetChatUsers(nonExistingChatUsers)).ConfigureAwait(false);
                            }

                            var updatedChatUsersVm = blockData.ChatUsers.Where(chatUser => existingChatUsers.Any(opt => opt.UserId == chatUser.UserId));
                            if (updatedChatUsersVm.Any())
                            {
                                var updatedChatUsers = ChatUserConverter.GetChatUsers(updatedChatUsersVm);
                                _context.ChatUsers.AttachRange(updatedChatUsers);
                                _context.ChatUsers.UpdateRange(updatedChatUsers);
                            }
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleChangeUsersChatSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }


        private async Task HandlePrivateChatSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        bool chatExists = !await _context.Chats.AnyAsync(chat => chat.Id == segment.SegmentHeader.ObjectId).ConfigureAwait(false);
                        if (TryDecryptPrivateData<PrivateChatPrivateData>(segment, out var privateData))
                        {
                            Chat newChat = ChatConverter.GetChat(privateData.Chat);
                            if (!chatExists)
                            {
                                await _context.Chats.AddAsync(newChat).ConfigureAwait(false);
                            }
                            else
                            {
                                _context.Chats.Attach(newChat);
                                _context.Chats.Update(newChat);
                            }
                        }
                        else
                        {
                            if (!chatExists)
                            {
                                Chat emptyChat = new Chat
                                {
                                    Id = segment.SegmentHeader.ObjectId,
                                    Type = (short)ChatType.Private,
                                    Name = "Restored-private-chat"
                                };
                                await _context.Chats.AddAsync(emptyChat).ConfigureAwait(false);
                            }
                        }
                        await _context.SaveChangesAsync().ConfigureAwait(false);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandlePrivateChatSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleNewUserKeysBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        NewUserKeysBlockData blockData = (NewUserKeysBlockData)segment.PublicData;
                        var keysCondition = PredicateBuilder.New<Key>();
                        keysCondition = blockData.Keys.Aggregate(keysCondition,
                            (current, value) => current.Or(opt => opt.KeyId == value.KeyId && opt.UserId == value.UserId).Expand());
                        var existingKeys = await _context.Keys.Where(keysCondition).ToListAsync().ConfigureAwait(false);
                        var nonExistingKeys = blockData.Keys.Where(key => !existingKeys.Any(opt => opt.KeyId == key.KeyId && opt.UserId == blockData.UserId));
                        if (nonExistingKeys.Any())
                        {
                            var newKeys = KeyConverter.GetKeys(nonExistingKeys);
                            await _context.Keys.AddRangeAsync(newKeys).ConfigureAwait(false);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleNewUserKeysBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleDeleteUserKeyBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        DeleteUserKeysBlockData blockData = (DeleteUserKeysBlockData)segment.PublicData;
                        var keysCondition = PredicateBuilder.New<Key>();
                        keysCondition = blockData.KeysId.Aggregate(keysCondition,
                            (current, value) => current.Or(opt => opt.KeyId == value && opt.UserId == blockData.UserId).Expand());
                        var keys = await _context.Keys.Where(keysCondition).ToListAsync().ConfigureAwait(false);
                        if (keys.Any())
                        {
                            _context.Keys.RemoveRange(keys);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleDeleteUserKeyBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleChannelBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        ChannelBlockData blockData = (ChannelBlockData)segment.PublicData;
                        var channel = await _context.Channels.FirstOrDefaultAsync(opt => opt.ChannelId == blockData.Channel.ChannelId).ConfigureAwait(false);
                        if (channel == null)
                        {
                            channel = ChannelConverter.GetChannel(blockData.Channel);
                            await _context.Channels.AddAsync(channel).ConfigureAwait(false);
                        }
                        else
                        {
                            channel = ChannelConverter.GetChannel(channel, blockData.Channel);
                            _context.Channels.Update(channel);
                        }
                        await _context.SaveChangesAsync().ConfigureAwait(false);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleChannelBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleDeleteChannelBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        DeleteChannelBlockData blockData = (DeleteChannelBlockData)segment.PublicData;
                        var channel = await _context.Channels.FirstOrDefaultAsync(opt => opt.ChannelId == blockData.ChannelId).ConfigureAwait(false);
                        if (channel != null)
                        {
                            _context.Channels.Remove(channel);
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleDeleteChannelBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }

        private async Task HandleChannelUsersBlockSegmentAsync(BlockSegmentVm segment)
        {
            using (MessengerDbContext _context = CreateContext())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        if (TryDecryptPrivateData<ChannelUsersBlockData>(segment, out var blockData))
                        {
                            var channelUserCondition = PredicateBuilder.New<ChannelUser>();
                            channelUserCondition = blockData.ChannelUsers.Aggregate(channelUserCondition,
                                (current, value) => current.Or(opt => opt.ChannelId == value.ChannelId && opt.UserId == value.UserId).Expand());
                            var existingChannelUsers = await _context.ChannelUsers.Where(channelUserCondition).ToListAsync().ConfigureAwait(false);
                            if (existingChannelUsers.Any())
                            {
                                foreach (var channelUser in existingChannelUsers)
                                {
                                    var editedChannelUser = blockData.ChannelUsers.FirstOrDefault(opt => opt.UserId == channelUser.UserId);
                                    if (editedChannelUser != null)
                                    {
                                        channelUser.Banned = editedChannelUser.Banned ?? channelUser.Banned;
                                        channelUser.Deleted = editedChannelUser.Deleted ?? channelUser.Deleted;
                                        channelUser.ChannelUserRole = editedChannelUser.ChannelUserRole ?? channelUser.ChannelUserRole;
                                    }
                                }
                                _context.ChannelUsers.UpdateRange(existingChannelUsers);
                            }
                            var nonExistingChannelUsers = blockData.ChannelUsers.Where(channelUser => !existingChannelUsers.Any(opt => opt.UserId == channelUser.UserId));
                            if (nonExistingChannelUsers.Any())
                            {
                                var newChannelUsers = ChannelConverter.GetChannelUsers(nonExistingChannelUsers);
                                await _context.ChannelUsers.AddRangeAsync(newChannelUsers).ConfigureAwait(false);
                            }
                            await _context.SaveChangesAsync().ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddErrorMessage(nameof(HandleChannelUsersBlockSegmentAsync), ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }
    }
}