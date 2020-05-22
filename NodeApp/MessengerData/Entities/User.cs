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
using System;
using System.Collections.Generic;
using System.Collections;
using NpgsqlTypes;
using System.ComponentModel.DataAnnotations;

namespace NodeApp.MessengerData.Entities
{
    public partial class User
    {
        public User()
        {
            BlackList = new HashSet<BadUser>();
            ReverseBlackList = new HashSet<BadUser>();
            ChatUsersInviter = new HashSet<ChatUser>();
            ChatUsersUser = new HashSet<ChatUser>();
            DialogsFirstU = new HashSet<Dialog>();
            DialogsSecondU = new HashSet<Dialog>();
            Emails = new HashSet<Emails>();            
            FilesInfo = new HashSet<FileInfo>();
            SentMessages = new HashSet<Message>();
            Phones = new HashSet<Phones>();
            Tokens = new HashSet<Token>();
            ReceivedMessages = new HashSet<Message>();
            UserPublicKeys = new HashSet<Key>();
            ChangeNodeOperations = new HashSet<ChangeUserNodeOperation>();
            Favorites = new HashSet<UserFavorite>();
            UserContacts = new HashSet<Contact>();
            Contacts = new HashSet<Contact>();
            Groups = new HashSet<Group>();
            PollOptionsVotes = new HashSet<PollOptionVote>();
            ChannelUsers = new HashSet<ChannelUser>();
            QRCodes = new HashSet<QRCode>();
            PendingMessages = new HashSet<PendingMessage>();
        }

        public long Id { get; set; }
        public string NameFirst { get; set; }
        public string NameSecond { get; set; }
        [MaxLength(100)]
        public string Tag { get; set; }
        public string About { get; set; }
        public string Photo { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public DateTime? Birthday { get; set; }
        public string Language { get; set; }
        public NpgsqlPoint? Geo { get; set; }
        public long? Online { get; set; }
        public bool? Confirmed { get; set; }
        public byte[] Sha512Password { get; set; }
        public long? RegistrationDate { get; set; }
        public long? NodeId { get; set; }        
        public int? ContactsPrivacy { get; set; }
        public int Privacy { get; set; }
        public bool Deleted { get; set; }
        public bool Banned { get; set; }
        public BitArray[] Security { get; set; }
        public NpgsqlTsVector SearchVector { get; set; }
        public bool SyncContacts { get; set; }

        public ICollection<QRCode> QRCodes { get; set; }
        public ICollection<ChangeUserNodeOperation> ChangeNodeOperations { get; set; }
        public ICollection<BadUser> BlackList { get; set; }
        public ICollection<BadUser> ReverseBlackList { get; set; }
        public ICollection<ChatUser> ChatUsersInviter { get; set; }
        public ICollection<ChatUser> ChatUsersUser { get; set; }
        public ICollection<Dialog> DialogsFirstU { get; set; }
        public ICollection<Dialog> DialogsSecondU { get; set; }
        public ICollection<Emails> Emails { get; set; }        
        public ICollection<FileInfo> FilesInfo { get; set; }
        public ICollection<Message> SentMessages { get; set; }
        public ICollection<Message> ReceivedMessages { get; set; }
        public ICollection<Phones> Phones { get; set; }
        public ICollection<Token> Tokens { get; set; }
        public ICollection<Key> UserPublicKeys { get; set; }
        public ICollection<ChannelUser> ChannelUsers { get; set; }
        public ICollection<PollOptionVote> PollOptionsVotes { get; set; }
        public ICollection<Group> Groups { get; set; }
        public ICollection<Contact> Contacts { get; set; }
        public ICollection<Contact> UserContacts { get; set; }
        public ICollection<UserFavorite> Favorites { get; set; }
        public ICollection<PendingMessage> PendingMessages { get; set; }

    }
}
