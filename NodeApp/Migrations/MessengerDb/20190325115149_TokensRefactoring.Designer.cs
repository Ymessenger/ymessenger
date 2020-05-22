﻿// <auto-generated />
using System;
using System.Collections;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodeApp.MessengerData.Entities;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

namespace NodeApp.Migrations.MessengerDb
{
    [DbContext(typeof(MessengerDbContext))]
    [Migration("20190325115149_TokensRefactoring")]
    partial class TokensRefactoring
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.0-rtm-35687")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Attachment", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<byte[]>("Hash")
                        .IsRequired();

                    b.Property<long>("MessageId");

                    b.Property<string>("Payload")
                        .HasMaxLength(300);

                    b.Property<short>("Type");

                    b.HasKey("Id");

                    b.HasIndex("MessageId")
                        .HasName("Attachments_MessageId_idx");

                    b.HasIndex("Type")
                        .HasName("Attachments_Type_idx");

                    b.ToTable("Attachments");
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.BadUser", b =>
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

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Chat", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("About")
                        .HasMaxLength(1000);

                    b.Property<bool>("Deleted");

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

                    b.Property<BitArray>("Security")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit varying(3)")
                        .HasDefaultValueSql("B'001'::bit varying");

                    b.Property<int?>("Tag");

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

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.ChatUser", b =>
                {
                    b.Property<long>("UserId");

                    b.Property<long>("ChatId");

                    b.Property<bool>("Banned")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("false");

                    b.Property<bool>("Deleted");

                    b.Property<long?>("InviterId");

                    b.Property<DateTime?>("Joined");

                    b.Property<long?>("LastReadedChatMessageId");

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

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Dialog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("FirstUID")
                        .HasColumnName("FirstUID");

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

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.DomainNode", b =>
                {
                    b.Property<long>("NodeId")
                        .HasColumnName("NodeID");

                    b.Property<string>("Domain")
                        .HasMaxLength(100);

                    b.HasKey("NodeId", "Domain")
                        .HasName("DomainNodes_pkey");

                    b.ToTable("DomainNodes");
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Emails", b =>
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

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.EncryptedKey", b =>
                {
                    b.Property<long>("UserId");

                    b.Property<byte[]>("Key");

                    b.Property<DateTime>("CreationTime")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("'0001-01-01 00:00:00'::timestamp without time zone");

                    b.Property<byte[]>("KeyInitVector")
                        .IsRequired();

                    b.HasKey("UserId", "Key")
                        .HasName("EncryptedKeys_pkey");

                    b.ToTable("EncryptedKeys");
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.FileInfo", b =>
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

                    b.Property<long>("NodeId");

                    b.Property<long>("NumericId")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("Size");

                    b.Property<DateTime>("UploadDate");

                    b.Property<long>("UploaderId");

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

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Ipnode", b =>
                {
                    b.Property<long>("NodeId")
                        .HasColumnName("NodeID");

                    b.Property<IPAddress>("Address");

                    b.HasKey("NodeId", "Address")
                        .HasName("IPNodes_pkey");

                    b.ToTable("IPNodes");
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Message", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ChatId");

                    b.Property<DateTime>("DateSend");

                    b.Property<long?>("DialogId");

                    b.Property<Guid>("GlobalId");

                    b.Property<bool>("Read");

                    b.Property<Guid?>("Replyto");

                    b.Property<long?>("SameMessageId");

                    b.Property<long?>("SenderId");

                    b.Property<string>("Text")
                        .HasMaxLength(10000);

                    b.HasKey("Id");

                    b.HasIndex("ChatId")
                        .HasName("Messages_ChatId_idx");

                    b.HasIndex("DateSend")
                        .HasName("Messages_DateSend_idx");

                    b.HasIndex("DialogId")
                        .HasName("Messages_DialogId_idx");

                    b.HasIndex("Read")
                        .HasName("Messages_Read_idx");

                    b.HasIndex("Replyto");

                    b.HasIndex("SenderId")
                        .HasName("Messages_SenderId_idx");

                    b.HasIndex("ChatId", "SenderId")
                        .HasName("Messages_ChatId_SenderId_idx");

                    b.HasIndex("GlobalId", "ChatId")
                        .IsUnique()
                        .HasName("Messages_GlobalId_ChatId_idx");

                    b.HasIndex("GlobalId", "DialogId")
                        .IsUnique()
                        .HasName("Messages_GlobalId_DialogId_idx");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Node", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("About")
                        .HasMaxLength(1000);

                    b.Property<string>("City")
                        .HasMaxLength(50);

                    b.Property<string>("Country")
                        .HasMaxLength(3);

                    b.Property<string>("Language")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("'EN'::character varying")
                        .HasMaxLength(2);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50);

                    b.Property<string>("Photo")
                        .HasMaxLength(300);

                    b.Property<bool?>("Routing")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("true");

                    b.Property<DateTime?>("Startday")
                        .HasColumnType("date");

                    b.Property<bool?>("Storage")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("true");

                    b.Property<int?>("Tag");

                    b.Property<bool?>("Visible")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("true");

                    b.HasKey("Id");

                    b.ToTable("Nodes");
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Phones", b =>
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

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Token", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AccessToken")
                        .IsRequired()
                        .HasMaxLength(300);

                    b.Property<long>("AccessTokenExpirationTime");

                    b.Property<string>("RefreshToken")
                        .HasMaxLength(300);

                    b.Property<long?>("RefreshTokenExpirationTime");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .HasName("Tokens_UserId_idx");

                    b.ToTable("Tokens");
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("About")
                        .HasMaxLength(1000);

                    b.Property<DateTime?>("Birthday")
                        .HasColumnType("date");

                    b.Property<string>("City")
                        .HasMaxLength(50);

                    b.Property<bool?>("Confirmed")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("false");

                    b.Property<string>("Country")
                        .HasMaxLength(3);

                    b.Property<bool>("Deleted");

                    b.Property<NpgsqlPoint?>("Geo");

                    b.Property<string>("Language")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("'EN'::character varying")
                        .HasMaxLength(2);

                    b.Property<string>("NameFirst")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("'unconfirmed'::character varying")
                        .HasMaxLength(30);

                    b.Property<string>("NameSecond")
                        .HasMaxLength(30);

                    b.Property<long>("NodeId");

                    b.Property<DateTime?>("Online");

                    b.Property<string>("Password")
                        .HasMaxLength(64);

                    b.Property<string>("Photo")
                        .HasMaxLength(300);

                    b.Property<DateTime?>("RegistrationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("date")
                        .HasDefaultValueSql("'2018-06-01'::date");

                    b.Property<BitArray[]>("Security")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit varying(15)[]")
                        .HasDefaultValueSql("'{11110001111001}'::bit varying[]");

                    b.Property<int?>("Tag");

                    b.Property<BitArray[]>("Visible")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit varying(23)[]")
                        .HasDefaultValueSql("'{11000000000000000000000}'::bit varying[]");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.UserChatPreview", b =>
                {
                    b.Property<long>("ChatId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("ChatType");

                    b.Property<string>("Chatname");

                    b.Property<DateTime?>("DateSend");

                    b.Property<string>("Photo");

                    b.Property<long?>("SenderId");

                    b.Property<string>("SenderName");

                    b.Property<string>("Text");

                    b.HasKey("ChatId");

                    b.ToTable("ChatsPreview");
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.UserDialogPreview", b =>
                {
                    b.Property<long>("DialogId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("Datesend");

                    b.Property<string>("Dialogname");

                    b.Property<string>("Photo");

                    b.Property<long>("SecondUid");

                    b.Property<long?>("SenderId");

                    b.Property<string>("Text");

                    b.HasKey("DialogId");

                    b.ToTable("DialogsPreview");
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Attachment", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.Message", "Message")
                        .WithMany("Attachments")
                        .HasForeignKey("MessageId")
                        .HasConstraintName("Attachments_MessageId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.BadUser", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.User", "User")
                        .WithMany("ReverseBlackList")
                        .HasForeignKey("BadUid")
                        .HasConstraintName("BadUsers_UID_fkey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerDb.Entities.User", "BlockedUser")
                        .WithMany("BlackList")
                        .HasForeignKey("Uid")
                        .HasConstraintName("BadUsers_BadUID_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Chat", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.Message", "LastMessage")
                        .WithMany("Chats")
                        .HasForeignKey("LastMessageId")
                        .HasConstraintName("Chats_LastMessageId_fkey")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.ChatUser", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.Chat", "Chat")
                        .WithMany("ChatUsers")
                        .HasForeignKey("ChatId")
                        .HasConstraintName("ChatUsers_ChatId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerDb.Entities.User", "Inviter")
                        .WithMany("ChatUsersInviter")
                        .HasForeignKey("InviterId")
                        .HasConstraintName("ChatUsers_InviterId_fkey")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("NodeApp.MessengerDb.Entities.User", "User")
                        .WithMany("ChatUsersUser")
                        .HasForeignKey("UserId")
                        .HasConstraintName("ChatUsers_UserId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Dialog", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.User", "FirstU")
                        .WithMany("DialogsFirstU")
                        .HasForeignKey("FirstUID")
                        .HasConstraintName("Dialogs_FirstUID_fkey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerDb.Entities.Message", "LastMessage")
                        .WithMany("Dialogs")
                        .HasForeignKey("LastMessageId")
                        .HasConstraintName("Dialogs_LastMessageId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerDb.Entities.User", "SecondU")
                        .WithMany("DialogsSecondU")
                        .HasForeignKey("SecondUID")
                        .HasConstraintName("Dialogs_SecondUID_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.DomainNode", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.Node", "Node")
                        .WithMany("DomainNodes")
                        .HasForeignKey("NodeId")
                        .HasConstraintName("DomainNodes_NodeID_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Emails", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.User", "User")
                        .WithMany("Emails")
                        .HasForeignKey("UserId")
                        .HasConstraintName("Emails_UserId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.EncryptedKey", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.User", "User")
                        .WithMany("EncryptedKeys")
                        .HasForeignKey("UserId")
                        .HasConstraintName("EncryptedKeys_UserId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.FileInfo", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.Node", "Node")
                        .WithMany()
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerDb.Entities.User", "Uploader")
                        .WithMany("FilesInfo")
                        .HasForeignKey("UploaderId")
                        .HasConstraintName("FilesInfo_UploaderId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Ipnode", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.Node", "Node")
                        .WithMany("Ipnodes")
                        .HasForeignKey("NodeId")
                        .HasConstraintName("IPNodes_NodeID_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Message", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.Chat", "Chat")
                        .WithMany("Messages")
                        .HasForeignKey("ChatId")
                        .HasConstraintName("Messages_ChatId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerDb.Entities.Dialog", "Dialog")
                        .WithMany("Messages")
                        .HasForeignKey("DialogId")
                        .HasConstraintName("Messages_DialogId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerDb.Entities.Message", "ReplytoNavigation")
                        .WithMany("InverseReplytoNavigation")
                        .HasForeignKey("Replyto")
                        .HasConstraintName("Messages_Replyto_fkey")
                        .HasPrincipalKey("GlobalId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NodeApp.MessengerDb.Entities.User", "Sender")
                        .WithMany("Messages")
                        .HasForeignKey("SenderId")
                        .HasConstraintName("Messages_SenderId_fkey");
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Phones", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.User", "User")
                        .WithMany("Phones")
                        .HasForeignKey("UserId")
                        .HasConstraintName("Phones_UserId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NodeApp.MessengerDb.Entities.Token", b =>
                {
                    b.HasOne("NodeApp.MessengerDb.Entities.User", "User")
                        .WithMany("Tokens")
                        .HasForeignKey("UserId")
                        .HasConstraintName("Tokens_UserId_fkey")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
