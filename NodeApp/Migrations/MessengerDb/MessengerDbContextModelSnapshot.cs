﻿/** 
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
using Microsoft.EntityFrameworkCore.Infrastructure;
using NodeApp.MessengerData.Entities;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Net;

namespace NodeApp.Migrations.MessengerDb
{
    [DbContext(typeof(MessengerDbContext))]
    partial class MessengerDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.0-rtm-35687")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Attachment", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<byte[]>("Hash")
                        .IsRequired();

                    b.Property<long>("MessageId");

                    b.Property<string>("Payload")
                        .HasMaxLength(50000);

                    b.Property<short>("Type");

                    b.HasKey("Id");

                    b.HasIndex("MessageId")
                        .HasName("Attachments_MessageId_idx");

                    b.HasIndex("Type")
                        .HasName("Attachments_Type_idx");

                    b.ToTable("Attachments");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.BadUser", b =>
                {
                    b.Property<long>("Uid")
                        .HasColumnName("UID");

                    b.Property<long>("BadUid")
                        .HasColumnName("BadUID");

                    b.HasKey("Uid", "BadUid")
                        .HasName("BadUsers_pkey");

                    b.HasIndex("BadUid");

                    b.ToTable("BadUsers");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.ChangeUserNodeOperation", b =>
                {
                    b.Property<string>("OperationId")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(64);

                    b.Property<bool>("Completed");

                    b.Property<long>("ExpirationTime");

                    b.Property<long>("NodeId");

                    b.Property<long>("RequestTime");

                    b.Property<long>("UserId");

                    b.HasKey("OperationId");

                    b.HasIndex("NodeId");

                    b.HasIndex("UserId");

                    b.ToTable("ChangeUserNodeOperations");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Channel", b =>
                {
                    b.Property<long>("ChannelId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("About");

                    b.Property<string>("ChannelName");

                    b.Property<long>("CreationTime");

                    b.Property<bool>("Deleted");

                    b.Property<Guid?>("LastMessageGlobalId");

                    b.Property<long?>("LastMessageId");

                    b.Property<long[]>("NodesId");

                    b.Property<string>("Photo");

                    b.Property<NpgsqlTsVector>("SearchVector");

                    b.Property<string>("Tag");

                    b.HasKey("ChannelId");

                    b.HasIndex("LastMessageId");

                    b.ToTable("Channels");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.ChannelIdentificator", b =>
                {
                    b.Property<long>("ChannelId")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("IsUsed")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue(false);

                    b.HasKey("ChannelId");

                    b.ToTable("ChannelsIdentificators");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.ChannelUser", b =>
                {
                    b.Property<long>("UserId");

                    b.Property<long>("ChannelId");

                    b.Property<bool>("Banned");

                    b.Property<byte>("ChannelUserRole");

                    b.Property<bool>("Deleted");

                    b.Property<bool>("IsMuted")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue(false);

                    b.Property<Guid?>("LastReadedGlobalMessageId");

                    b.Property<long?>("LastReadedMessageId");

                    b.Property<long>("SubscribedTime");

                    b.HasKey("UserId", "ChannelId");

                    b.HasIndex("ChannelId");

                    b.HasIndex("LastReadedMessageId");

                    b.ToTable("ChannelUsers");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Chat", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("About")
                        .HasMaxLength(1000);

                    b.Property<bool>("Deleted");

                    b.Property<Guid?>("LastMessageGlobalId");

                    b.Property<long?>("LastMessageId");

                    b.Property<string>("Name")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("'CHAT'::character varying")
                        .HasMaxLength(50);

                    b.Property<long[]>("NodesId");

                    b.Property<string>("Photo")
                        .HasMaxLength(300);

                    b.Property<BitArray>("Public")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit varying(3)")
                        .HasDefaultValueSql("B'010'::\"bit\"");

                    b.Property<NpgsqlTsVector>("SearchVector");

                    b.Property<BitArray>("Security")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit varying(3)")
                        .HasDefaultValueSql("B'001'::bit varying");

                    b.Property<string>("Tag")
                        .HasMaxLength(100);

                    b.Property<short>("Type");

                    b.Property<BitArray>("Visible")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit varying(8)")
                        .HasDefaultValueSql("B'11000000'::\"bit\"");

                    b.HasKey("Id");

                    b.HasIndex("LastMessageId");

                    b.ToTable("Chats");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.ChatIdentificator", b =>
                {
                    b.Property<long>("ChatId")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("IsUsed")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue(false);

                    b.HasKey("ChatId");

                    b.ToTable("ChatsIdentificators");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.ChatUser", b =>
                {
                    b.Property<long>("UserId");

                    b.Property<long>("ChatId");

                    b.Property<bool>("Banned")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("false");

                    b.Property<bool>("Deleted");

                    b.Property<long?>("InviterId");

                    b.Property<bool>("IsMuted")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue(false);

                    b.Property<long?>("Joined");

                    b.Property<long?>("LastReadedChatMessageId");

                    b.Property<Guid?>("LastReadedGlobalMessageId");

                    b.Property<byte>("UserRole");

                    b.HasKey("UserId", "ChatId")
                        .HasName("ChatUsers_pkey");

                    b.HasIndex("Banned")
                        .HasName("ChatUsers_Banned_idx");

                    b.HasIndex("ChatId");

                    b.HasIndex("Deleted")
                        .HasName("ChatUsers_Deleted_idx");

                    b.HasIndex("InviterId");

                    b.ToTable("ChatUsers");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Contact", b =>
                {
                    b.Property<Guid>("ContactId")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("ContactUserId");

                    b.Property<string>("Name")
                        .HasMaxLength(50);

                    b.Property<long>("UserId");

                    b.HasKey("ContactId");

                    b.HasIndex("ContactUserId");

                    b.HasIndex("UserId");

                    b.ToTable("Contacts");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.ContactGroup", b =>
                {
                    b.Property<Guid>("ContactId");

                    b.Property<Guid>("GroupId");

                    b.HasKey("ContactId", "GroupId");

                    b.HasIndex("GroupId");

                    b.ToTable("ContactsGroups");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.ConversationPreview", b =>
                {
                    b.Property<byte>("ConversationType");

                    b.Property<long>("ConversationId");

                    b.Property<short[]>("AttachmentTypes");

                    b.Property<bool?>("IsMuted");

                    b.Property<Guid?>("LastMessageId");

                    b.Property<long?>("LastMessageSenderId");

                    b.Property<string>("LastMessageSenderName");

                    b.Property<long?>("LastMessageTime");

                    b.Property<string>("Photo");

                    b.Property<string>("PreviewText");

                    b.Property<bool?>("Read");

                    b.Property<long?>("SecondUserId");

                    b.Property<string>("Title");

                    b.Property<int?>("UnreadedCount");

                    b.HasKey("ConversationType", "ConversationId");

                    b.ToTable("ConversationPreview");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Dialog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("FirstUID")
                        .HasColumnName("FirstUID");

                    b.Property<bool>("IsMuted")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue(false);

                    b.Property<Guid?>("LastMessageGlobalId");

                    b.Property<long?>("LastMessageId");

                    b.Property<long>("SecondUID")
                        .HasColumnName("SecondUID");

                    b.Property<BitArray>("Security")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit varying(2)")
                        .HasDefaultValueSql("B'10'::\"bit\"");

                    b.HasKey("Id");

                    b.HasIndex("FirstUID")
                        .HasName("Dialogs_FirstUID_idx");

                    b.HasIndex("LastMessageId");

                    b.HasIndex("SecondUID")
                        .HasName("Dialogs_SecondUID_idx");

                    b.HasIndex("FirstUID", "SecondUID")
                        .IsUnique()
                        .HasName("Dialogs_FirstUID_SecondUID_idx");

                    b.ToTable("Dialogs");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.DomainNode", b =>
                {
                    b.Property<long>("NodeId")
                        .HasColumnName("NodeID");

                    b.Property<string>("Domain")
                        .HasMaxLength(100);

                    b.HasKey("NodeId", "Domain")
                        .HasName("DomainNodes_pkey");

                    b.ToTable("DomainNodes");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.EditedMessage", b =>
                {
                    b.Property<long>("RecordId")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("EditorId");

                    b.Property<long>("MessageId");

                    b.Property<long>("SendingTime");

                    b.Property<byte[]>("Sign");

                    b.Property<string>("Text");

                    b.Property<long?>("UpdatedTime");

                    b.HasKey("RecordId");

                    b.HasIndex("MessageId");

                    b.ToTable("EditedMessages");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.EditedMessageAttachment", b =>
                {
                    b.Property<long>("RecordId")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("ActualAttachmentId");

                    b.Property<short>("AttachmentType");

                    b.Property<long>("EditedMessageId");

                    b.Property<byte[]>("Hash");

                    b.Property<string>("Payload");

                    b.HasKey("RecordId");

                    b.HasIndex("ActualAttachmentId");

                    b.HasIndex("EditedMessageId");

                    b.ToTable("EditedMessagesAttachments");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Emails", b =>
                {
                    b.Property<long>("UserId");

                    b.Property<string>("EmailAddress")
                        .HasMaxLength(320);

                    b.HasKey("UserId", "EmailAddress")
                        .HasName("Emails_pkey");

                    b.HasIndex("EmailAddress")
                        .IsUnique();

                    b.ToTable("Emails");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.FileIdentificator", b =>
                {
                    b.Property<long>("FileId")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("IsUsed")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue(false);

                    b.HasKey("FileId");

                    b.ToTable("FilesIdentificators");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.FileInfo", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("''::character varying")
                        .HasMaxLength(256);

                    b.Property<bool>("Deleted");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("'Unnamed File'::character varying")
                        .HasMaxLength(100);

                    b.Property<byte[]>("Hash")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("'\\x'::bytea");

                    b.Property<string>("ImageMetadata");

                    b.Property<long>("NodeId");

                    b.Property<long>("NumericId")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("Size");

                    b.Property<string>("Storage")
                        .HasMaxLength(50);

                    b.Property<long>("UploadDate");

                    b.Property<long?>("UploaderId");

                    b.Property<string>("Url")
                        .HasColumnName("URL")
                        .HasMaxLength(300);

                    b.HasKey("Id");

                    b.HasIndex("NodeId");

                    b.HasIndex("NumericId")
                        .HasName("FilesInfo_NumericId_idx");

                    b.HasIndex("UploadDate")
                        .HasName("FilesInfo_UploadDate_idx");

                    b.HasIndex("UploaderId")
                        .HasName("FilesInfo_UploaderId_idx");

                    b.ToTable("FilesInfo");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Group", b =>
                {
                    b.Property<Guid>("GroupId")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("PrivacySettings");

                    b.Property<string>("Title")
                        .HasMaxLength(100);

                    b.Property<long>("UserId");

                    b.HasKey("GroupId");

                    b.HasIndex("UserId");

                    b.ToTable("Groups");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Ipnode", b =>
                {
                    b.Property<long>("NodeId")
                        .HasColumnName("NodeID");

                    b.Property<IPAddress>("Address");

                    b.HasKey("NodeId", "Address")
                        .HasName("IPNodes_pkey");

                    b.ToTable("IPNodes");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Key", b =>
                {
                    b.Property<long>("RecordId")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ChatId");

                    b.Property<long?>("ExpirationTimeSeconds");

                    b.Property<long>("GenerationTimeSeconds");

                    b.Property<byte[]>("KeyData");

                    b.Property<long>("KeyId");

                    b.Property<byte>("Type");

                    b.Property<long?>("UserId");

                    b.Property<byte>("Version");

                    b.HasKey("RecordId");

                    b.HasIndex("ChatId")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.HasIndex("KeyId", "ChatId")
                        .IsUnique();

                    b.HasIndex("KeyId", "UserId")
                        .IsUnique();

                    b.ToTable("Keys");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Message", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ChannelId");

                    b.Property<long?>("ChatId");

                    b.Property<bool>("Deleted");

                    b.Property<long?>("DialogId");

                    b.Property<long?>("ExpiredAt");

                    b.Property<Guid>("GlobalId");

                    b.Property<long[]>("NodesIds");

                    b.Property<bool>("Read");

                    b.Property<long?>("ReceiverId");

                    b.Property<Guid?>("Replyto");

                    b.Property<long?>("SameMessageId");

                    b.Property<NpgsqlTsVector>("SearchVector");

                    b.Property<long?>("SenderId");

                    b.Property<long>("SendingTime");

                    b.Property<string>("Text")
                        .HasMaxLength(10000);

                    b.Property<long?>("UpdatedAt");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId")
                        .HasName("Messages_ChannelId_idx");

                    b.HasIndex("ChatId")
                        .HasName("Messages_ChatId_idx");

                    b.HasIndex("DialogId")
                        .HasName("Messages_DialogId_idx");

                    b.HasIndex("ExpiredAt");

                    b.HasIndex("Read")
                        .HasName("Messages_Read_idx");

                    b.HasIndex("ReceiverId");

                    b.HasIndex("Replyto");

                    b.HasIndex("SenderId")
                        .HasName("Messages_SenderId_idx");

                    b.HasIndex("SendingTime")
                        .HasName("Messages_DateSend_idx");

                    b.HasIndex("UpdatedAt")
                        .HasName("Messages_UpdatedAt_idx");

                    b.HasIndex("ChatId", "SenderId")
                        .HasName("Messages_ChatId_SenderId_idx");

                    b.HasIndex("GlobalId", "ChannelId")
                        .IsUnique();

                    b.HasIndex("GlobalId", "ChatId")
                        .IsUnique()
                        .HasName("Messages_GlobalId_ChatId_idx");

                    b.HasIndex("GlobalId", "DialogId")
                        .IsUnique()
                        .HasName("Messages_GlobalId_DialogId_idx");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Node", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("About")
                        .HasMaxLength(1000);

                    b.Property<string>("AdminEmail")
                        .HasMaxLength(254);

                    b.Property<string>("City")
                        .HasMaxLength(50);

                    b.Property<int>("ClientsPort");

                    b.Property<string>("Country")
                        .HasMaxLength(3);

                    b.Property<byte>("EncryptionType");

                    b.Property<string>("Language")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("'EN'::character varying")
                        .HasMaxLength(2);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50);

                    b.Property<int>("NodesPort");

                    b.Property<bool>("PermanentlyDeleting");

                    b.Property<string>("Photo")
                        .HasMaxLength(300);

                    b.Property<byte>("RegistrationMethod");

                    b.Property<bool?>("Routing")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("true");

                    b.Property<DateTime?>("Startday")
                        .HasColumnType("date");

                    b.Property<bool?>("Storage")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("true");

                    b.Property<string>("SupportEmail")
                        .HasMaxLength(254);

                    b.Property<string>("Tag")
                        .HasMaxLength(100);

                    b.Property<bool>("UserRegistrationAllowed");

                    b.Property<bool?>("Visible")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("true");

                    b.HasKey("Id");

                    b.ToTable("Nodes");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.NodeKeys", b =>
                {
                    b.Property<long>("NodeId");

                    b.Property<long>("KeyId");

                    b.Property<long>("ExpirationTime");

                    b.Property<long>("GenerationTime");

                    b.Property<byte[]>("PrivateKey");

                    b.Property<byte[]>("PublicKey");

                    b.Property<byte[]>("SignPrivateKey");

                    b.Property<byte[]>("SignPublicKey");

                    b.Property<byte[]>("SymmetricKey");

                    b.HasKey("NodeId", "KeyId");

                    b.ToTable("NodesKeys");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.PendingMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Content");

                    b.Property<long>("ExpiredAt");

                    b.Property<Guid?>("MessageId");

                    b.Property<long?>("NodeId");

                    b.Property<long?>("ReceiverId");

                    b.Property<long>("SentAt");

                    b.HasKey("Id");

                    b.HasIndex("NodeId");

                    b.HasIndex("ReceiverId");

                    b.ToTable("PendingMessages");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Phones", b =>
                {
                    b.Property<long>("UserId");

                    b.Property<string>("PhoneNumber")
                        .HasMaxLength(20);

                    b.Property<bool>("Main");

                    b.HasKey("UserId", "PhoneNumber")
                        .HasName("Phones_pkey");

                    b.HasIndex("PhoneNumber")
                        .IsUnique();

                    b.ToTable("Phones");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Poll", b =>
                {
                    b.Property<Guid>("PollId");

                    b.Property<byte>("ConversationType");

                    b.Property<long>("ConvertsationId");

                    b.Property<long>("CreatorId");

                    b.Property<bool>("MultipleSelection");

                    b.Property<bool>("ResultsVisibility");

                    b.Property<bool>("SignRequired");

                    b.Property<string>("Title")
                        .HasMaxLength(100);

                    b.Property<long?>("UserId");

                    b.HasKey("PollId", "ConversationType", "ConvertsationId");

                    b.HasIndex("UserId");

                    b.ToTable("Polls");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.PollOption", b =>
                {
                    b.Property<byte>("OptionId");

                    b.Property<Guid>("PollId");

                    b.Property<byte>("ConversationType");

                    b.Property<long>("ConversationId");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.HasKey("OptionId", "PollId", "ConversationType", "ConversationId");

                    b.HasIndex("PollId", "ConversationType", "ConversationId");

                    b.ToTable("PollsOptions");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.PollOptionVote", b =>
                {
                    b.Property<byte>("OptionId");

                    b.Property<long>("UserId");

                    b.Property<Guid>("PollId");

                    b.Property<byte>("ConversationType");

                    b.Property<long>("ConversationId");

                    b.Property<byte[]>("Sign");

                    b.Property<long?>("SignKeyId");

                    b.HasKey("OptionId", "UserId", "PollId", "ConversationType", "ConversationId");

                    b.HasIndex("UserId");

                    b.HasIndex("OptionId", "PollId", "ConversationType", "ConversationId");

                    b.ToTable("PollsOptionsVotes");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.QRCode", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<byte[]>("SequenceHash");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("QRCodes");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Token", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AccessToken")
                        .IsRequired()
                        .HasMaxLength(300);

                    b.Property<long>("AccessTokenExpirationTime");

                    b.Property<string>("AppName")
                        .HasMaxLength(50);

                    b.Property<string>("DeviceName")
                        .HasMaxLength(50);

                    b.Property<string>("DeviceTokenId")
                        .HasMaxLength(1000);

                    b.Property<string>("IPAddress")
                        .HasMaxLength(30);

                    b.Property<long>("LastActivityTime")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue(0L);

                    b.Property<string>("OSName")
                        .HasMaxLength(50);

                    b.Property<string>("RefreshToken")
                        .HasMaxLength(300);

                    b.Property<long?>("RefreshTokenExpirationTime");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .HasName("Tokens_UserId_idx");

                    b.ToTable("Tokens");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("About")
                        .HasMaxLength(1000);

                    b.Property<bool>("Banned");

                    b.Property<DateTime?>("Birthday")
                        .HasColumnType("date");

                    b.Property<string>("City")
                        .HasMaxLength(50);

                    b.Property<bool?>("Confirmed")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("false");

                    b.Property<int?>("ContactsPrivacy");

                    b.Property<string>("Country")
                        .HasMaxLength(3);

                    b.Property<bool>("Deleted");

                    b.Property<NpgsqlPoint?>("Geo");

                    b.Property<string>("Language")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("'EN'::character varying")
                        .HasMaxLength(2);

                    b.Property<string>("NameFirst")
                        .HasMaxLength(30);

                    b.Property<string>("NameSecond")
                        .HasMaxLength(30);

                    b.Property<long?>("NodeId");

                    b.Property<long?>("Online");

                    b.Property<string>("Photo")
                        .HasMaxLength(300);

                    b.Property<int>("Privacy");

                    b.Property<long?>("RegistrationDate")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("-9223372036854775808");

                    b.Property<NpgsqlTsVector>("SearchVector");

                    b.Property<BitArray[]>("Security")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit varying(15)[]")
                        .HasDefaultValueSql("'{11110001111001}'::bit varying[]");

                    b.Property<byte[]>("Sha512Password");

                    b.Property<bool>("SyncContacts");

                    b.Property<string>("Tag")
                        .HasMaxLength(100);

                    b.HasKey("Id");

                    b.HasIndex("SearchVector")
                        .HasAnnotation("Npgsql:IndexMethod", "GIN");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.UserChatPreview", b =>
                {
                    b.Property<long>("ChatId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("ChatType");

                    b.Property<string>("Chatname");

                    b.Property<long?>("DateSend");

                    b.Property<string>("Photo");

                    b.Property<long?>("SenderId");

                    b.Property<string>("SenderName");

                    b.Property<string>("Text");

                    b.HasKey("ChatId");

                    b.ToTable("ChatsPreview");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.UserDialogPreview", b =>
                {
                    b.Property<long>("DialogId")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("Datesend");

                    b.Property<string>("Dialogname");

                    b.Property<string>("Photo");

                    b.Property<long>("SecondUid");

                    b.Property<long?>("SenderId");

                    b.Property<string>("Text");

                    b.HasKey("DialogId");

                    b.ToTable("DialogsPreview");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.UserFavorite", b =>
                {
                    b.Property<long>("RecordId")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ChannelId");

                    b.Property<long?>("ChatId");

                    b.Property<Guid?>("ContactId");

                    b.Property<short>("SerialNumber");

                    b.Property<long>("UserId");

                    b.HasKey("RecordId");

                    b.HasIndex("ChannelId");

                    b.HasIndex("ChatId");

                    b.HasIndex("ContactId")
                        .IsUnique();

                    b.HasIndex("UserId", "ChannelId")
                        .IsUnique();

                    b.HasIndex("UserId", "ChatId")
                        .IsUnique();

                    b.HasIndex("UserId", "ContactId")
                        .IsUnique();

                    b.ToTable("UsersFavorites");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.UserIdentificator", b =>
                {
                    b.Property<long>("UserId")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("IsUsed")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue(false);

                    b.HasKey("UserId");

                    b.ToTable("UsersIdentificators");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Attachment", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Message", "Message")
                        .WithMany("Attachments")
                        .HasForeignKey("MessageId")
                        .HasConstraintName("Attachments_MessageId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.BadUser", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("ReverseBlackList")
                        .HasForeignKey("BadUid")
                        .HasConstraintName("BadUsers_UID_fkey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.User", "BlockedUser")
                        .WithMany("BlackList")
                        .HasForeignKey("Uid")
                        .HasConstraintName("BadUsers_BadUID_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.ChangeUserNodeOperation", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Node", "Node")
                        .WithMany("ChangeUserNodeOperations")
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("ChangeNodeOperations")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Channel", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Message", "LastMessage")
                        .WithMany()
                        .HasForeignKey("LastMessageId");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.ChannelUser", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Channel", "Channel")
                        .WithMany("ChannelUsers")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.Message", "LastReadedMessage")
                        .WithMany()
                        .HasForeignKey("LastReadedMessageId");

                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("ChannelUsers")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Chat", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Message", "LastMessage")
                        .WithMany("Chats")
                        .HasForeignKey("LastMessageId")
                        .HasConstraintName("Chats_LastMessageId_fkey")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.ChatUser", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Chat", "Chat")
                        .WithMany("ChatUsers")
                        .HasForeignKey("ChatId")
                        .HasConstraintName("ChatUsers_ChatId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.User", "Inviter")
                        .WithMany("ChatUsersInviter")
                        .HasForeignKey("InviterId")
                        .HasConstraintName("ChatUsers_InviterId_fkey")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("ChatUsersUser")
                        .HasForeignKey("UserId")
                        .HasConstraintName("ChatUsers_UserId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Contact", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.User", "ContactUser")
                        .WithMany("UserContacts")
                        .HasForeignKey("ContactUserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("Contacts")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.ContactGroup", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Contact", "Contact")
                        .WithMany("ContactGroups")
                        .HasForeignKey("ContactId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.Group", "Group")
                        .WithMany("ContactGroups")
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Dialog", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.User", "FirstU")
                        .WithMany("DialogsFirstU")
                        .HasForeignKey("FirstUID")
                        .HasConstraintName("Dialogs_FirstUID_fkey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.Message", "LastMessage")
                        .WithMany("Dialogs")
                        .HasForeignKey("LastMessageId")
                        .HasConstraintName("Dialogs_LastMessageId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.User", "SecondU")
                        .WithMany("DialogsSecondU")
                        .HasForeignKey("SecondUID")
                        .HasConstraintName("Dialogs_SecondUID_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.DomainNode", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Node", "Node")
                        .WithMany("DomainNodes")
                        .HasForeignKey("NodeId")
                        .HasConstraintName("DomainNodes_NodeID_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.EditedMessage", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Message", "ActualMessage")
                        .WithMany("EditedMessages")
                        .HasForeignKey("MessageId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.EditedMessageAttachment", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Attachment", "ActualAttachment")
                        .WithMany("EditedMessageAttachments")
                        .HasForeignKey("ActualAttachmentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.EditedMessage", "Message")
                        .WithMany("Attachments")
                        .HasForeignKey("EditedMessageId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Emails", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("Emails")
                        .HasForeignKey("UserId")
                        .HasConstraintName("Emails_UserId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.FileInfo", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Node", "Node")
                        .WithMany()
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.User", "Uploader")
                        .WithMany("FilesInfo")
                        .HasForeignKey("UploaderId")
                        .HasConstraintName("FilesInfo_UploaderId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Group", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("Groups")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Ipnode", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Node", "Node")
                        .WithMany("Ipnodes")
                        .HasForeignKey("NodeId")
                        .HasConstraintName("IPNodes_NodeID_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Key", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Chat", "Chat")
                        .WithOne("Key")
                        .HasForeignKey("NodeApp.MessengerData.Entities.Key", "ChatId");

                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("UserPublicKeys")
                        .HasForeignKey("UserId")
                        .HasConstraintName("UsersPublicKeys_UserId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Message", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Channel", "Channel")
                        .WithMany("Messages")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.Chat", "Chat")
                        .WithMany("Messages")
                        .HasForeignKey("ChatId")
                        .HasConstraintName("Messages_ChatId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.Dialog", "Dialog")
                        .WithMany("Messages")
                        .HasForeignKey("DialogId")
                        .HasConstraintName("Messages_DialogId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.User", "Receiver")
                        .WithMany("ReceivedMessages")
                        .HasForeignKey("ReceiverId")
                        .HasConstraintName("Messages_ReceiverId_fkey");

                    b.HasOne("NodeApp.MessengerData.Entities.Message", "ReplytoNavigation")
                        .WithMany("InverseReplytoNavigation")
                        .HasForeignKey("Replyto")
                        .HasConstraintName("Messages_Replyto_fkey")
                        .HasPrincipalKey("GlobalId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.User", "Sender")
                        .WithMany("SentMessages")
                        .HasForeignKey("SenderId")
                        .HasConstraintName("Messages_SenderId_fkey");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.NodeKeys", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Node", "Node")
                        .WithMany("NodeKeys")
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.PendingMessage", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Node", "Node")
                        .WithMany()
                        .HasForeignKey("NodeId");

                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("PendingMessages")
                        .HasForeignKey("ReceiverId");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Phones", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("Phones")
                        .HasForeignKey("UserId")
                        .HasConstraintName("Phones_UserId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Poll", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.PollOption", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Poll", "Poll")
                        .WithMany("Options")
                        .HasForeignKey("PollId", "ConversationType", "ConversationId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.PollOptionVote", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("PollOptionsVotes")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerData.Entities.PollOption", "PollOption")
                        .WithMany("PollOptionVotes")
                        .HasForeignKey("OptionId", "PollId", "ConversationType", "ConversationId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.QRCode", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("QRCodes")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.Token", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.User", "User")
                        .WithMany("Tokens")
                        .HasForeignKey("UserId")
                        .HasConstraintName("Tokens_UserId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerData.Entities.UserFavorite", b =>
                {
                    b.HasOne("NodeApp.MessengerData.Entities.Channel", "Channel")
                        .WithMany("Favorites")
                        .HasForeignKey("ChannelId");

                    b.HasOne("NodeApp.MessengerData.Entities.Chat", "Chat")
                        .WithMany("Favorites")
                        .HasForeignKey("ChatId");

                    b.HasOne("NodeApp.MessengerData.Entities.Contact", "Contact")
                        .WithOne("UserFavorite")
                        .HasForeignKey("NodeApp.MessengerData.Entities.UserFavorite", "ContactId");

                    b.HasOne("NodeApp.MessengerData.Entities.User")
                        .WithMany("Favorites")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
