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
using NodeApp.Extensions;
using ObjectsLibrary.ViewModels;
using System;
using System.Linq;
using System.Net;

namespace NodeApp.MessengerData.Entities
{
    public partial class MessengerDbContext : DbContext
    {
        private readonly bool _isTesting;
        public MessengerDbContext()
        {            
        }

        public MessengerDbContext(DbContextOptions<MessengerDbContext> options, bool isTesting = false)
            : base(options)
        {
            _isTesting = isTesting;
        }

        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<BadUser> BadUsers { get; set; }
        public DbSet<ChatUser> ChatUsers { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Dialog> Dialogs { get; set; }
        public DbSet<DomainNode> DomainNodes { get; set; }
        public DbSet<Emails> Emails { get; set; }
        public DbSet<FileInfo> FilesInfo { get; set; }
        public DbSet<Ipnode> Ipnodes { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Node> Nodes { get; set; }
        public DbSet<Phones> Phones { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Key> Keys { get; set; }        
        public DbSet<ConversationPreview> ConversationPreview { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<ChannelUser> ChannelUsers { get; set; }        
        public DbSet<ChangeUserNodeOperation> ChangeUserNodeOperations { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<PollOption> PollsOptions { get; set; }
        public DbSet<PollOptionVote> PollsOptionsVotes { get; set; }
        public DbSet<NodeKeys> NodesKeys { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<ContactGroup> ContactsGroups { get; set; }
        public DbSet<EditedMessage> EditedMessages { get; set; }
        public DbSet<EditedMessageAttachment> EditedMessagesAttachments { get; set; }
        public DbSet<UserFavorite> UsersFavorites { get; set; }
        public DbSet<ChannelIdentificator> ChannelsIdentificators { get; set; }
        public DbSet<ChatIdentificator> ChatsIdentificators { get; set; }
        public DbSet<UserIdentificator> UsersIdentificators { get; set; }
        public DbSet<FileIdentificator> FilesIdentificators { get; set; }
        public DbSet<QRCode> QRCodes { get; set; }
        public DbSet<PendingMessage> PendingMessages { get; set; }

        /* protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
         {
             if (!optionsBuilder.IsConfigured)
             {
                 optionsBuilder.UseNpgsql(NodeSettings.Configs.MessengerDbConnection.ToString());
             }
         }*/

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.0-rtm-35687");

            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.HasIndex(e => e.MessageId)
                    .HasName("Attachments_MessageId_idx");

                entity.HasIndex(e => e.Type)
                    .HasName("Attachments_Type_idx");

                entity.Property(e => e.Hash).IsRequired();

                entity.Property(e => e.Payload).HasMaxLength(50000);

                entity.HasOne(d => d.Message)
                    .WithMany(p => p.Attachments)
                    .HasForeignKey(d => d.MessageId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Attachments_MessageId_fkey");
            });

            modelBuilder.Entity<BadUser>(entity =>
            {
                entity.HasKey(e => new { e.Uid, e.BadUid })
                    .HasName("BadUsers_pkey");

                entity.Property(e => e.Uid).HasColumnName("UID");

                entity.Property(e => e.BadUid).HasColumnName("BadUID");

                entity.HasOne(d => d.BlockedUser)
                    .WithMany(p => p.BlackList)
                    .HasForeignKey(d => d.Uid)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("BadUsers_BadUID_fkey");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ReverseBlackList)
                    .HasForeignKey(d => d.BadUid)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("BadUsers_UID_fkey");
            });

            modelBuilder.Entity<ChatUser>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.ChatId })
                    .HasName("ChatUsers_pkey");

                entity.HasIndex(e => e.Banned)
                    .HasName("ChatUsers_Banned_idx");

                entity.HasIndex(e => e.Deleted)
                    .HasName("ChatUsers_Deleted_idx");

                entity.Property(e => e.Banned).HasDefaultValueSql("false");

                entity.Property(e => e.IsMuted).HasDefaultValue(false);

                entity.HasOne(d => d.Chat)
                    .WithMany(p => p.ChatUsers)
                    .HasForeignKey(d => d.ChatId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("ChatUsers_ChatId_fkey");

                entity.HasOne(d => d.Inviter)
                    .WithMany(p => p.ChatUsersInviter)
                    .HasForeignKey(d => d.InviterId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("ChatUsers_InviterId_fkey");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ChatUsersUser)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("ChatUsers_UserId_fkey");
            });

            modelBuilder.Entity<Chat>(entity =>
            {
                entity.Property(e => e.About).HasMaxLength(1000);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValueSql("'CHAT'::character varying");

                entity.Property(e => e.Photo).HasMaxLength(300);

                entity.Property(e => e.Public)
                    .IsRequired()
                    .HasColumnType("bit varying(3)")
                    .HasDefaultValueSql("B'010'::\"bit\"");

                entity.Property(e => e.Security)
                    .IsRequired()
                    .HasColumnType("bit varying(3)")
                    .HasDefaultValueSql("B'001'::bit varying");

                entity.Property(e => e.Visible)
                    .IsRequired()
                    .HasColumnType("bit varying(8)")
                    .HasDefaultValueSql("B'11000000'::\"bit\"");

                entity.HasOne(d => d.LastMessage)
                    .WithMany(p => p.Chats)
                    .HasForeignKey(d => d.LastMessageId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("Chats_LastMessageId_fkey");
                if (_isTesting)
                {
                    entity.Ignore(e => e.SearchVector);
                    entity.Property(e => e.NodesId).HasConversion<string>(
                        value => value.AsString(","),
                        strValue => strValue.Split(",", StringSplitOptions.None).Select(subStr => Convert.ToInt64(subStr)).ToArray());
                    entity.Ignore(e => e.Public);
                    entity.Ignore(e => e.Security);
                    entity.Ignore(e => e.Visible);
                }
            });

            modelBuilder.Entity<Dialog>(entity =>
            {
                entity.HasIndex(e => e.FirstUID)
                    .HasName("Dialogs_FirstUID_idx");

                entity.HasIndex(e => e.SecondUID)
                    .HasName("Dialogs_SecondUID_idx");

                entity.HasIndex(e => new { e.FirstUID, e.SecondUID })
                    .HasName("Dialogs_FirstUID_SecondUID_idx")
                    .IsUnique();

                entity.Property(e => e.FirstUID).HasColumnName("FirstUID");

                entity.Property(e => e.SecondUID).HasColumnName("SecondUID");

                entity.Property(e => e.IsMuted).HasDefaultValue(false);

                entity.Property(e => e.Security)
                    .IsRequired()
                    .HasColumnType("bit varying(2)")
                    .HasDefaultValueSql("B'10'::\"bit\"");

                entity.HasOne(d => d.FirstU)
                    .WithMany(p => p.DialogsFirstU)
                    .HasForeignKey(d => d.FirstUID)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Dialogs_FirstUID_fkey");

                entity.HasOne(d => d.LastMessage)
                    .WithMany(p => p.Dialogs)
                    .HasForeignKey(d => d.LastMessageId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Dialogs_LastMessageId_fkey");

                entity.HasOne(d => d.SecondU)
                    .WithMany(p => p.DialogsSecondU)
                    .HasForeignKey(d => d.SecondUID)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Dialogs_SecondUID_fkey");
                if (_isTesting)
                {
                    entity.Ignore(e => e.Security);
                }
            });

            modelBuilder.Entity<DomainNode>(entity =>
            {
                entity.HasKey(e => new { e.NodeId, e.Domain })
                    .HasName("DomainNodes_pkey");

                entity.Property(e => e.NodeId).HasColumnName("NodeID");

                entity.Property(e => e.Domain).HasMaxLength(100);

                entity.HasOne(d => d.Node)
                    .WithMany(p => p.DomainNodes)
                    .HasForeignKey(d => d.NodeId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("DomainNodes_NodeID_fkey");
            });

            modelBuilder.Entity<Emails>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.EmailAddress })
                    .HasName("Emails_pkey");

                entity.Property(e => e.EmailAddress).HasMaxLength(320);

                entity.HasIndex(e => e.EmailAddress).IsUnique();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Emails)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Emails_UserId_fkey");
            });

            modelBuilder.Entity<FileInfo>(entity =>
            {
                entity.HasIndex(e => e.NumericId)
                    .HasName("FilesInfo_NumericId_idx");

                entity.HasIndex(e => e.UploadDate)
                    .HasName("FilesInfo_UploadDate_idx");

                entity.HasIndex(e => e.UploaderId)
                    .HasName("FilesInfo_UploaderId_idx");


                entity.Property(e => e.Id)
                    .HasMaxLength(256)
                    .HasDefaultValueSql("''::character varying");

                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasDefaultValueSql("'Unnamed File'::character varying");

                entity.Property(e => e.Hash)
                    .IsRequired()
                    .HasDefaultValueSql("'\\x'::bytea");

                entity.Property(e => e.NumericId).ValueGeneratedOnAdd();

                entity.Property(e => e.Url)
                    .HasColumnName("URL")
                    .HasMaxLength(300);

                entity.HasOne(d => d.Uploader)
                    .WithMany(p => p.FilesInfo)
                    .HasForeignKey(d => d.UploaderId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FilesInfo_UploaderId_fkey");

                entity.Property(e => e.ImageMetadata)
                    .HasConversion(
                        metadata => metadata.ToJson(false), 
                        json => ObjectsLibrary.Converters.ObjectSerializer.JsonToObject<ImageMetadata>(json));
            });

            modelBuilder.Entity<Ipnode>(entity =>
            {
                entity.HasKey(e => new { e.NodeId, e.Address })
                    .HasName("IPNodes_pkey");

                entity.ToTable("IPNodes");

                entity.Property(e => e.NodeId).HasColumnName("NodeID");

                entity.HasOne(d => d.Node)
                    .WithMany(p => p.Ipnodes)
                    .HasForeignKey(d => d.NodeId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("IPNodes_NodeID_fkey");
                if (_isTesting)
                {
                    entity.Property(e => e.Address).HasConversion(e => e.ToString(), e => IPAddress.Parse(e));
                }
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasIndex(e => e.ChatId)
                    .HasName("Messages_ChatId_idx");

                entity.HasIndex(e => e.SendingTime)
                    .HasName("Messages_DateSend_idx");

                entity.HasIndex(e => e.DialogId)
                    .HasName("Messages_DialogId_idx");

                entity.HasIndex(e => e.Read)
                    .HasName("Messages_Read_idx");

                entity.HasIndex(e => e.SenderId)
                    .HasName("Messages_SenderId_idx");

                entity.HasIndex(e => e.UpdatedAt)
                    .HasName("Messages_UpdatedAt_idx");

                entity.HasIndex(e => e.ChannelId)
                    .HasName("Messages_ChannelId_idx");

                entity.HasIndex(e => new { e.ChatId, e.SenderId })
                    .HasName("Messages_ChatId_SenderId_idx");

                entity.HasIndex(e => new { e.GlobalId, e.ChatId })
                    .HasName("Messages_GlobalId_ChatId_idx")
                    .IsUnique();

                entity.HasIndex(e => new { e.GlobalId, e.DialogId })
                    .HasName("Messages_GlobalId_DialogId_idx")
                    .IsUnique();
                entity.HasIndex(e => new { e.GlobalId, e.ChannelId })
                    .IsUnique();
                entity.HasIndex(e => e.ExpiredAt);
                entity.Property(e => e.Text).HasMaxLength(10000);

                entity.HasOne(d => d.Chat)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(d => d.ChatId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Messages_ChatId_fkey");

                entity.HasOne(d => d.Dialog)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(d => d.DialogId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Messages_DialogId_fkey");

                entity.HasOne(d => d.ReplytoNavigation)
                    .WithMany(p => p.InverseReplytoNavigation)
                    .HasPrincipalKey(p => p.GlobalId)
                    .HasForeignKey(d => d.Replyto)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Messages_Replyto_fkey");

                entity.HasOne(d => d.Sender)
                    .WithMany(p => p.SentMessages)
                    .HasForeignKey(d => d.SenderId)
                    .HasConstraintName("Messages_SenderId_fkey");

                entity.HasOne(d => d.Receiver)
                    .WithMany(p => p.ReceivedMessages)
                    .HasForeignKey(d => d.ReceiverId)
                    .HasConstraintName("Messages_ReceiverId_fkey");

                entity.HasOne(d => d.Channel)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(d => d.ChannelId)
                    .OnDelete(DeleteBehavior.Cascade);
                if (_isTesting)
                {
                    entity.Ignore(e => e.SearchVector);
                    entity.Property(e => e.NodesIds).HasConversion<string>(
                       value => value.AsString(","),
                       strValue => strValue.Split(",", StringSplitOptions.None).Select(subStr => Convert.ToInt64(subStr)).ToArray());
                }

            });

            modelBuilder.Entity<Node>(entity =>
            {
                entity.Property(e => e.About).HasMaxLength(1000);

                entity.Property(e => e.City).HasMaxLength(50);

                entity.Property(e => e.Country).HasMaxLength(3);

                entity.Property(e => e.Language)
                    .HasMaxLength(2)
                    .HasDefaultValueSql("'EN'::character varying");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Photo).HasMaxLength(300);

                entity.Property(e => e.Routing).HasDefaultValueSql("true");

                entity.Property(e => e.Startday).HasColumnType("date");

                entity.Property(e => e.Storage).HasDefaultValueSql("true");

                entity.Property(e => e.Visible).HasDefaultValueSql("true");

                entity.HasMany(e => e.NodeKeys)
                    .WithOne(e => e.Node)
                    .HasForeignKey(e => e.NodeId);
                entity.Property(e => e.SupportEmail)
                    .HasMaxLength(254);
                entity.Property(e => e.AdminEmail)
                    .HasMaxLength(254);
            });

            modelBuilder.Entity<Phones>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.PhoneNumber })
                    .HasName("Phones_pkey");

                entity.Property(e => e.PhoneNumber).HasMaxLength(20);

                entity.HasIndex(e => e.PhoneNumber).IsUnique();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Phones)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Phones_UserId_fkey");
            });

            modelBuilder.Entity<Token>(entity =>
            {
                entity.HasIndex(e => e.UserId)
                    .HasName("Tokens_UserId_idx");

                entity.Property(e => e.AccessToken)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(e => e.RefreshToken)
                    .HasMaxLength(300);
                entity.Property(e => e.AppName)
                    .HasMaxLength(50);
                entity.Property(e => e.OSName)
                    .HasMaxLength(50);
                entity.Property(e => e.DeviceName)
                    .HasMaxLength(50);
                entity.Property(e => e.IPAddress)
                    .HasMaxLength(30);
                entity.Property(e => e.LastActivityTime)
                    .HasDefaultValue(0);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Tokens)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Tokens_UserId_fkey");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.About).HasMaxLength(1000);

                entity.Property(e => e.Birthday).HasColumnType("date");

                entity.Property(e => e.City).HasMaxLength(50);

                entity.Property(e => e.Confirmed).HasDefaultValueSql("false");

                entity.Property(e => e.Country).HasMaxLength(3);

                entity.Property(e => e.Language)
                    .HasMaxLength(2)
                    .HasDefaultValueSql("'EN'::character varying");

                entity.Property(e => e.NameFirst).HasMaxLength(30);

                entity.Property(e => e.NameSecond).HasMaxLength(30);

                entity.Property(e => e.Photo).HasMaxLength(300);

                entity.Property(e => e.RegistrationDate)
                    .HasDefaultValueSql(long.MinValue.ToString());

                entity.Property(e => e.Security)
                    .IsRequired()
                    .HasColumnType("bit varying(15)[]")
                    .HasDefaultValueSql("'{11110001111001}'::bit varying[]");

                entity.HasIndex(e => e.SearchVector)
                    .ForNpgsqlHasMethod("GIN");
                if (_isTesting)
                {
                    entity.Ignore(e => e.SearchVector);
                    entity.Ignore(e => e.Security);
                }
            });

            modelBuilder.Entity<Key>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany(e => e.UserPublicKeys)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("UsersPublicKeys_UserId_fkey");

                entity.HasOne(e => e.Chat)
                    .WithOne(e => e.Key)
                    .HasForeignKey<Key>(e => e.ChatId);

                entity.HasKey(e => new { e.RecordId });

                entity.HasIndex(e => new { e.KeyId, e.UserId })
                    .IsUnique(true);

                entity.HasIndex(e => new { e.KeyId, e.ChatId })
                    .IsUnique(true);
            });

            modelBuilder.Entity<Channel>(entity =>
            {
                entity.HasKey(e => e.ChannelId);
                entity.HasMany(e => e.Messages)
                    .WithOne(e => e.Channel)
                    .HasForeignKey(e => e.ChannelId);
                if (_isTesting)
                {
                    entity.Property(e => e.NodesId)
                        .HasConversion(
                            e => e.AsString(","),
                            e => e.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => Convert.ToInt64(v)).ToArray());
                    entity.Ignore(e => e.SearchVector);
                    entity.Property(e => e.NodesId).HasConversion<string>(
                       value => value.AsString(","),
                       strValue => strValue.Split(",", StringSplitOptions.None).Select(subStr => Convert.ToInt64(subStr)).ToArray());
                }
            });

            modelBuilder.Entity<ChannelUser>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.ChannelId });
                entity.Property(e => e.IsMuted).HasDefaultValue(false);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.ChannelUsers)
                    .HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Channel)
                    .WithMany(e => e.ChannelUsers)
                    .HasForeignKey(e => e.ChannelId);
            });
           
            modelBuilder.Entity<ChangeUserNodeOperation>(entity =>
            {
                entity.HasKey(e => e.OperationId);
                entity.Property(e => e.OperationId).HasMaxLength(64);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.ChangeNodeOperations)
                    .HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Node)
                    .WithMany(e => e.ChangeUserNodeOperations)
                    .HasForeignKey(e => e.NodeId);
            });

            modelBuilder.Entity<Poll>(entity =>
            {
                entity.HasKey(e => new { e.PollId, e.ConversationType, e.ConvertsationId });
                entity.Property(e => e.Title).HasMaxLength(100);
                entity.HasMany(e => e.Options)
                    .WithOne(p => p.Poll)
                    .HasForeignKey(e => new { e.PollId, e.ConversationType, e.ConversationId });
            });

            modelBuilder.Entity<PollOption>(entity =>
            {
                entity.HasKey(e => new { e.OptionId, e.PollId, e.ConversationType, e.ConversationId });
                entity.Property(e => e.Description)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.HasOne(e => e.Poll)
                    .WithMany(e => e.Options)
                    .HasForeignKey(e => new { e.PollId, e.ConversationType, e.ConversationId });
                entity.HasMany(e => e.PollOptionVotes)
                    .WithOne(e => e.PollOption)
                    .HasForeignKey(e => new { e.PollId, e.ConversationType, e.ConversationId });
            });

            modelBuilder.Entity<PollOptionVote>(entity =>
            {
                entity.HasKey(e => new { e.OptionId, e.UserId, e.PollId, e.ConversationType, e.ConversationId });
                entity.HasOne(e => e.User)
                    .WithMany(e => e.PollOptionsVotes)
                    .HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.PollOption)
                    .WithMany(e => e.PollOptionVotes)
                    .HasForeignKey(e => new { e.OptionId, e.PollId, e.ConversationType, e.ConversationId });
            });

            modelBuilder.Entity<NodeKeys>(entity =>
            {
                entity.HasKey(e => new { e.NodeId, e.KeyId });
                entity.HasOne(e => e.Node)
                    .WithMany(e => e.NodeKeys)
                    .HasForeignKey(e => e.NodeId);
            });

            modelBuilder.Entity<Group>(entity =>
            {
                entity.HasKey(e => e.GroupId);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.Groups)
                    .HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<Contact>(entity =>
            {
                entity.HasKey(e => e.ContactId);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.Contacts)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ContactUser)
                    .WithMany(e => e.UserContacts)
                    .HasForeignKey(e => e.ContactUserId);
            });

            modelBuilder.Entity<ContactGroup>(entity =>
            {
                entity.HasKey(e => new { e.ContactId, e.GroupId });
                entity.HasOne(e => e.Contact)
                      .WithMany(e => e.ContactGroups)
                      .HasForeignKey(e => e.ContactId);
                entity.HasOne(e => e.Group)
                      .WithMany(e => e.ContactGroups)
                      .HasForeignKey(e => e.GroupId);
            });

            modelBuilder.Entity<EditedMessage>(entity =>
            {
                entity.HasKey(e => e.RecordId);
                entity.HasOne(e => e.ActualMessage)
                    .WithMany(e => e.EditedMessages)
                    .HasForeignKey(e => e.MessageId);
            });

            modelBuilder.Entity<EditedMessageAttachment>(entity =>
            {
                entity.HasKey(e => e.RecordId);
                entity.HasOne(e => e.ActualAttachment)
                    .WithMany(e => e.EditedMessageAttachments)
                    .HasForeignKey(e => e.ActualAttachmentId);
                entity.HasOne(e => e.Message)
                    .WithMany(e => e.Attachments)
                    .HasForeignKey(e => e.EditedMessageId);
            });

            modelBuilder.Entity<UserFavorite>(entity =>
            {
                entity.HasKey(e => e.RecordId);
                entity.HasOne(e => e.Contact)
                    .WithOne(e => e.UserFavorite)
                    .HasForeignKey<UserFavorite>(e => e.ContactId);
                entity.HasOne(e => e.Channel)
                    .WithMany(e => e.Favorites)
                    .HasForeignKey(e => e.ChannelId);
                entity.HasOne(e => e.Chat)
                    .WithMany(e => e.Favorites)
                    .HasForeignKey(e => e.ChatId);
                entity.HasIndex(e => new
                {
                    e.UserId,
                    e.ContactId
                }).IsUnique();
                entity.HasIndex(e => new
                {
                    e.UserId,
                    e.ChatId
                }).IsUnique();
                entity.HasIndex(e => new
                {
                    e.UserId,
                    e.ChannelId
                }).IsUnique();
            });
            modelBuilder.Entity<UserIdentificator>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.IsUsed).HasDefaultValue(false);
            });
            modelBuilder.Entity<ChatIdentificator>(entity =>
            {
                entity.HasKey(e => e.ChatId);
                entity.Property(e => e.IsUsed).HasDefaultValue(false);
            });
            modelBuilder.Entity<ChannelIdentificator>(entity =>
            {
                entity.HasKey(e => e.ChannelId);
                entity.Property(e => e.IsUsed).HasDefaultValue(false);
            });
            modelBuilder.Entity<FileIdentificator>(entity =>
            {
                entity.HasKey(e => e.FileId);
                entity.Property(e => e.IsUsed).HasDefaultValue(false);
            });
            modelBuilder.Entity<QRCode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.QRCodes)
                    .HasForeignKey(e => e.UserId);
            });
            modelBuilder.Entity<PendingMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.PendingMessages)
                    .HasForeignKey(e => e.ReceiverId);
            });
            modelBuilder.Entity<ConversationPreview>(entity =>
            {
                entity.HasKey(e => new { e.ConversationType, e.ConversationId });
                if(_isTesting)
                    entity.Ignore(e => e.AttachmentTypes);
            });
        }
    }
}
