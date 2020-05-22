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
using Microsoft.EntityFrameworkCore;
using NodeApp.MessengerData.Entities;
using NodeApp.Tests.Mocks;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NodeApp.Tests
{
    public class FillTestDbHelper
    {
        private readonly MessengerDbContext _context;
        private readonly MockMessengerDbContextFactory contextFactory;        
        public List<User> Users 
        { 
            get 
            {
                using (var context = contextFactory.Create())
                {
                    return context.Users
                        .Include(user => user.Emails)
                        .Include(user => user.Phones)
                        .Include(user => user.BlackList)
                        .Include(user => user.ReverseBlackList)
                        .Include(user => user.DialogsFirstU)
                        .Include(user => user.Contacts)
                        .Include(user => user.Favorites)
                        .Include(user => user.Tokens)
                        .Include(user => user.Groups)
                        .ThenInclude(user => user.ContactGroups)
                        .ToList();
                }
            } 
        }
        public List<Chat> Chats 
        { 
            get 
            {
                using (var context = contextFactory.Create())
                {
                    return context.Chats
                        .Include(chat => chat.ChatUsers)
                        .Include(chat => chat.Messages)
                        .ToList();
                }
            } 
        }
        public List<Channel> Channels 
        { 
            get 
            {
                using (var context = contextFactory.Create())
                {
                    return context.Channels
                        .Include(channel => channel.ChannelUsers)
                        .Include(channel => channel.Messages)
                        .ToList();
                }
            }
        }
        public List<Dialog> Dialogs 
        { 
            get 
            {
                using (var context = contextFactory.Create())
                {
                    return context.Dialogs
                        .Include(opt => opt.FirstU)
                        .Include(opt => opt.SecondU)
                        .Include(opt => opt.Messages)
                        .ToList();
                }
            } 
        }
        public List<Message> Messages 
        { 
            get 
            {
                using (var context = contextFactory.Create())
                {
                    return context.Messages
                        .Include(message => message.Attachments)                        
                        .ToList();
                }
            } 
        }
        public List<FileInfo> Files
        {
            get
            {
                using (var context = contextFactory.Create())
                {
                    return context.FilesInfo
                        .ToList();
                }
            }
        }
        public List<Contact> Contacts
        {
            get
            {
                using (var context = contextFactory.Create())
                {
                    return context.Contacts.ToList();
                }
            }
        }
        public List<UserFavorite> Favorites
        {
            get
            {
                using (var context = contextFactory.Create())
                {
                    return context.UsersFavorites.ToList();
                }
            }
        }
        public List<Group> Groups
        {
            get
            {
                using (var context = contextFactory.Create()) 
                {
                    return context.Groups
                        .Include(opt => opt.ContactGroups)
                        .ThenInclude(opt => opt.Contact)
                        .ThenInclude(opt => opt.ContactUser)
                        .ToList();
                }
            }
        }
        public List<Node> Nodes
        {
            get
            {
                using (var context = contextFactory.Create())
                {
                    return context.Nodes.ToList();
                }
            }
        }
        public List<Poll> Polls
        {
            get
            {
                using(var context = contextFactory.Create())
                {
                    return context.Polls.ToList();
                }
            }
        }
        public FillTestDbHelper(MockMessengerDbContextFactory contextFactory)
        {
            _context = contextFactory.Create();
            this.contextFactory = contextFactory;
        }
        public async Task FillMessengerContextAsync()
        {
            await AddUsersAsync();
            await AddChatsAsync();
            await AddDialogsAsync();
            await AddChannelsAsync();
            await AddMessagesAsync();
            await MarkMessagesAsReadAsync();
            await AddPoolsAsync();
            await AddFilesAsync();
            await AddUsersContacts();
            await AddFavoritesAsync();
            await AddGroupsAsync();
            await AddNodesAsync();
        }
        private async Task<List<User>> AddUsersAsync(int count = 5)
        {
            byte[] passwordHash;
            using (SHA512 sha = SHA512.Create())
            {
                passwordHash = sha.ComputeHash(Encoding.UTF8.GetBytes("password"));
            }
            List<User> users = new List<User>();
            for(int i = 0; i < count; i++)
            {
                users.Add(new User
                {
                    About = RandomExtensions.NextString(100),
                    Birthday = DateTime.UtcNow,
                    Confirmed = true,
                    Photo = RandomExtensions.NextString(20),
                    Deleted = false,
                    Tag = RandomExtensions.NextString(10).ToUpper(),
                    NameFirst = "User",
                    NameSecond = i.ToString(),
                    Emails = new HashSet<Emails>
                    {
                        new Emails
                        {
                            EmailAddress = $"user_{RandomExtensions.NextString(i)}@test.test"
                        }
                    },
                    Phones = new HashSet<Phones>
                    {
                        new Phones
                        {
                            PhoneNumber = $"+7{RandomExtensions.NextString(10, "1234567890")}"
                        }
                    },
                    Sha512Password = passwordHash,
                    ContactsPrivacy = 0,
                    NodeId = 1
                });
            }
            users[4].Deleted = true;
            users[3].BlackList = new HashSet<BadUser> 
            { 
                new BadUser 
                { 
                    BadUid = 1
                } 
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();
            return users;
        }
        private async Task<List<Chat>> AddChatsAsync(int count = 5)
        {
            List<Chat> chats = new List<Chat>();
            for(int i = 0; i < count; i++)
            {
                var chatCreator = await _context.Users.FirstOrDefaultAsync();
                chats.Add(new Chat 
                {                     
                    About = RandomExtensions.NextString(20),
                    Deleted = false,
                    Name = "Chat "+i,
                    Tag = RandomExtensions.NextString(10).ToUpper(),
                    Type = RandomExtensions.NextInt64() > 0 ? (short)ChatType.Private : (short)ChatType.Public,
                    ChatUsers = _context.Users.Take(3).ToList().Select(user => new ChatUser 
                    {                         
                        UserId = user.Id,
                        Banned = false,
                        Deleted = false,
                        IsMuted = false,
                        UserRole = user.Id == chatCreator.Id ? UserRole.Creator : UserRole.User
                    }).ToList()
                });
            }
            await _context.Chats.AddRangeAsync(chats);
            await _context.SaveChangesAsync();
            return chats;
        }
        private async Task<List<Dialog>> AddDialogsAsync()
        {
            List<Dialog> dialogs = new List<Dialog>();
            var users = await _context.Users.ToListAsync();
            foreach(var first in users)
            {
                foreach(var second in users)
                {
                    if(!dialogs.Any(dialog => (dialog.FirstUID == first.Id && dialog.SecondUID == second.Id)))
                    {
                        dialogs.Add(new Dialog 
                        { 
                            FirstUID = first.Id,
                            SecondUID = second.Id                            
                        });
                    }
                }
            }
            await _context.Dialogs.AddRangeAsync(dialogs);
            await _context.SaveChangesAsync();
            return dialogs;
        }
        private async Task<List<Channel>> AddChannelsAsync(int count = 5)
        {
            List<Channel> channels = new List<Channel>();
            for (int i =0; i < count; i++)
            {
                var channelCreator = await _context.Users.LastOrDefaultAsync();
                channels.Add(new Channel
                {
                    About = RandomExtensions.NextString(20),
                    Deleted = false,
                    ChannelName = "Channel " + i,
                    Tag = RandomExtensions.NextString(10).ToUpper(),
                    ChannelUsers = _context.Users.ToList().Select(user => new ChannelUser 
                    { 
                        UserId = user.Id,
                        Banned = false,
                        Deleted = false,
                        SubscribedTime = DateTime.UtcNow.ToUnixTime(),
                        ChannelUserRole = user.Id == channelCreator.Id ? ChannelUserRole.Creator : ChannelUserRole.Subscriber
                    }).ToList(),
                    NodesId = RandomExtensions.GetRandomInt64Sequence(10, 0)
                });
            }
            await _context.Channels.AddRangeAsync(channels);
            await _context.SaveChangesAsync();
            return channels;
        }
        private async Task<List<Message>> AddMessagesAsync()
        {
            int counter = 0;
            List<Message> messages = new List<Message>();
            var dialogs = await _context.Dialogs.ToListAsync();
            var chats = await _context.Chats.Include(opt => opt.ChatUsers).ToListAsync();
            var channels = await _context.Channels.Include(opt => opt.ChannelUsers).ToListAsync();
            foreach (var dialog in dialogs)
            {
                string messageText = $"DIALOG MESSAGE #dialogId = {dialog.Id}";
                var secondDialog = dialogs.FirstOrDefault(opt => opt.FirstUID == dialog.SecondUID && opt.SecondUID == dialog.FirstUID);
                var message = new Message
                {
                    DialogId = dialog.Id,
                    SenderId = dialog.FirstUID,
                    ReceiverId = dialog.SecondUID,
                    Text = messageText,
                    SendingTime = DateTime.UtcNow.AddSeconds(counter).ToUnixTime(),
                    GlobalId = RandomExtensions.NextGuid()
                };
                if (dialog.Id != secondDialog.Id)
                {
                    var sameMessage = new Message
                    {
                        DialogId = secondDialog.Id,
                        SenderId = message.SenderId,
                        ReceiverId = message.ReceiverId,
                        Text = messageText,
                        SendingTime = message.SendingTime,
                        GlobalId = RandomExtensions.NextGuid()
                    };
                    messages.Add(sameMessage);
                }
                messages.Add(message);
                counter++;
            }
            foreach (var chat in chats)
            {                
                var senders = chat.ChatUsers;
                foreach (var sender in senders)
                {
                    string messageText = $"CHAT MESSAGE #ChatId = {chat.Id} #SenderId = {sender.UserId}";
                    messages.Add(new Message
                    {
                        ChatId = chat.Id,
                        SenderId = sender.UserId,
                        Text = messageText,
                        SendingTime = DateTime.UtcNow.AddSeconds(counter).ToUnixTime(),
                        GlobalId = RandomExtensions.NextGuid()
                    });
                    counter++;
                }
            }
            foreach (var channel in channels)
            {
                var sender = channel.ChannelUsers.FirstOrDefault(opt => opt.ChannelUserRole == ChannelUserRole.Creator);
                string messageText = $"CHANNEL MESSAGE #ChannelId = {channel.ChannelId}";
                messages.Add(new Message
                {
                    ChannelId = channel.ChannelId,
                    SenderId = sender.UserId,
                    Text = messageText,
                    SendingTime = DateTime.UtcNow.AddSeconds(counter).ToUnixTime(),
                    GlobalId = RandomExtensions.NextGuid()
                });
                counter++;
            }           
            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();
            var tempChat = await _context.Chats.Include(opt => opt.ChatUsers).FirstOrDefaultAsync();
            var replyMessage = await _context.Messages.FirstOrDefaultAsync(opt => opt.ChatId == tempChat.Id);
            var messageWithReply = new Message
            {
                ChatId = tempChat.Id,
                SenderId = tempChat.ChatUsers.FirstOrDefault().UserId,
                Text = "Reply message",
                SendingTime = DateTime.UtcNow.AddSeconds(counter).ToUnixTime(),
                GlobalId = RandomExtensions.NextGuid(),
                Replyto = replyMessage.GlobalId
            };
            messages.Add(messageWithReply);
            await _context.Messages.AddAsync(messageWithReply);
            await _context.SaveChangesAsync();
            return messages;
        }
        private async Task AddPoolsAsync()
        {
            List<long> poolIds = new List<long> { 1000, 1001, 1002, 1003 };            
            var usersIds = poolIds.Select(id => new UserIdentificator { IsUsed  = false, UserId = id });
            var chatsIds = poolIds.Select(id => new ChatIdentificator { IsUsed = false, ChatId = id });
            var channelsIds = poolIds.Select(id => new ChannelIdentificator { IsUsed = false, ChannelId = id });
            var filesIds = poolIds.Select(id => new FileIdentificator { IsUsed = false, FileId = id });
            await _context.AddRangeAsync(usersIds);
            await _context.AddRangeAsync(chatsIds);
            await _context.AddRangeAsync(channelsIds);
            await _context.AddRangeAsync(filesIds);
            await _context.SaveChangesAsync();
        }
        private async Task MarkMessagesAsReadAsync()
        {
            var chats = await _context.Chats.Include(opt => opt.ChatUsers).ToListAsync();
            foreach(var chat in chats)
            {
                var firstMessage = await _context.Messages.OrderBy(opt => opt.SendingTime).FirstOrDefaultAsync(opt => opt.ChatId == chat.Id);
                var lastMessage = await _context.Messages.OrderByDescending(opt => opt.SendingTime).FirstOrDefaultAsync(opt => opt.ChatId == chat.Id);
                foreach(var chatUser in chat.ChatUsers)
                {
                    chatUser.LastReadedChatMessageId = firstMessage.Id;
                    chatUser.LastReadedGlobalMessageId = firstMessage.GlobalId;
                }
                chat.LastMessageGlobalId = lastMessage.GlobalId;
                chat.LastMessageId = lastMessage.Id;
            }
            _context.UpdateRange(chats);
            var channels = await _context.Channels.Include(opt => opt.ChannelUsers).ToListAsync();
            foreach (var channel in channels)
            {
                var firstMessage = await _context.Messages.OrderBy(opt => opt.SendingTime).FirstOrDefaultAsync(opt => opt.ChannelId == channel.ChannelId);
                var lastMessage = await _context.Messages.OrderByDescending(opt => opt.SendingTime).FirstOrDefaultAsync(opt => opt.ChannelId == channel.ChannelId);
                foreach(var channelUser in channel.ChannelUsers)
                {
                    channelUser.LastReadedGlobalMessageId = firstMessage.GlobalId;
                }
            }
            await _context.SaveChangesAsync();
        }
        private async Task AddFilesAsync(int count = 5)
        {
            var uploader = await _context.Users.FirstOrDefaultAsync();
            List<FileInfo> files = new List<FileInfo>();
            for(int i=0; i<5; i++)
            {
                files.Add(new FileInfo
                {
                    FileName = "File " + i,
                    Hash = Array.Empty<byte>(),
                    Size = 1,
                    UploaderId = uploader.Id,
                    Storage = "Test",
                    UploadDate = DateTime.UtcNow.ToUnixTime(),
                    Url = "url"                    
                });
            }
            await _context.FilesInfo.AddRangeAsync(files);
            await _context.SaveChangesAsync();
        }
        private async Task AddUsersContacts()
        {
            var users = await _context.Users.ToListAsync();
            List<Contact> contacts = new List<Contact>();
            foreach(var firstUser in users)
                foreach(var secondUser in users)
                {
                    if(!contacts.Any(opt => opt.UserId == firstUser.Id && opt.ContactUserId == secondUser.Id))
                    {
                        contacts.Add(new Contact 
                        { 
                            UserId = firstUser.Id,
                            ContactUserId = secondUser.Id,
                            Name = $"Generated {firstUser.Id}_{secondUser.Id}"                            
                        });
                    }
                }
            await _context.Contacts.AddRangeAsync(contacts);
            await _context.SaveChangesAsync();
        }
        private async Task AddFavoritesAsync()
        {
            var users = await _context.Users
                .Include(opt => opt.Contacts)
                .ToListAsync();
            var favorites = new List<UserFavorite>();
            foreach(var user in users)
            {
                short serialNumber = 1;
                var chat = _context.Chats.FirstOrDefault(opt => opt.ChatUsers.Any(p => p.UserId == user.Id));
                var channel = _context.Channels.FirstOrDefault(opt => opt.ChannelUsers.Any(p => p.UserId == user.Id));
                if (channel != null)
                {
                    favorites.Add(new UserFavorite
                    {
                        ChannelId = channel.ChannelId,
                        SerialNumber = serialNumber,
                        UserId = user.Id
                    });
                    serialNumber++;
                }
                if (chat != null)
                {
                    favorites.Add(new UserFavorite
                    {
                        ChatId = chat.Id,
                        SerialNumber = serialNumber, 
                        UserId = user.Id
                    });
                    serialNumber++;
                }
                if (user.Contacts?.FirstOrDefault() != null)
                {
                    favorites.Add(new UserFavorite
                    {
                        ContactId = user.Contacts.FirstOrDefault().ContactId,
                        SerialNumber = serialNumber,
                        UserId = user.Id
                    });
                }
            }
            await _context.UsersFavorites.AddRangeAsync(favorites);
            await _context.SaveChangesAsync();                
        }
        private async Task AddGroupsAsync()
        {
            var contacts = await _context.Contacts.ToListAsync();
            var groups = contacts.GroupBy(opt => opt.UserId);
            var usersGroups = new List<Group>();
            foreach(var group in groups)
            {
                var userGroup = new Group
                {
                    Title = $"{group.Key} USER GROUP",
                    PrivacySettings = 0,
                    UserId = group.Key,
                    ContactGroups = group.Select(opt => new ContactGroup { ContactId = opt.ContactId }).ToHashSet()                    
                };
                usersGroups.Add(userGroup);
            }
            await _context.Groups.AddRangeAsync(usersGroups);
            await _context.SaveChangesAsync();
        }
        private async Task AddNodesAsync()
        {
            await _context.Nodes.AddRangeAsync(new Node 
            { 
                Id = 1,
                Name = "Node 1"                
            },
            new Node
            {
                Id = 2,
                Name = "Node 2"
            });
            await _context.SaveChangesAsync();
        }       
    }
}