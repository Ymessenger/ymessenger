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
using System.Collections;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

namespace NodeApp.Migrations.MessengerDb
{
    public partial class FIRST : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    Tag = table.Column<int>(nullable: true),
                    About = table.Column<string>(maxLength: 1000, nullable: true),
                    Photo = table.Column<string>(maxLength: 300, nullable: true),
                    Country = table.Column<string>(maxLength: 3, nullable: true),
                    City = table.Column<string>(maxLength: 50, nullable: true),
                    Startday = table.Column<DateTime>(type: "date", nullable: true),
                    Language = table.Column<string>(maxLength: 2, nullable: true, defaultValueSql: "'EN'::character varying"),
                    Visible = table.Column<bool>(nullable: true, defaultValueSql: "true"),
                    Storage = table.Column<bool>(nullable: true, defaultValueSql: "true"),
                    Routing = table.Column<bool>(nullable: true, defaultValueSql: "true")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    NameFirst = table.Column<string>(maxLength: 30, nullable: false, defaultValueSql: "'unconfirmed'::character varying"),
                    NameSecond = table.Column<string>(maxLength: 30, nullable: true),
                    Tag = table.Column<int>(nullable: true),
                    About = table.Column<string>(maxLength: 1000, nullable: true),
                    Photo = table.Column<string>(maxLength: 300, nullable: true),
                    Country = table.Column<string>(maxLength: 3, nullable: true),
                    City = table.Column<string>(maxLength: 50, nullable: true),
                    Birthday = table.Column<DateTime>(type: "date", nullable: true),
                    Language = table.Column<string>(maxLength: 2, nullable: true, defaultValueSql: "'EN'::character varying"),
                    Geo = table.Column<NpgsqlPoint>(nullable: true),
                    Online = table.Column<DateTime>(nullable: true),
                    Confirmed = table.Column<bool>(nullable: true, defaultValueSql: "false"),
                    Password = table.Column<string>(maxLength: 64, nullable: true),
                    RegistrationDate = table.Column<DateTime>(type: "date", nullable: true, defaultValueSql: "'2018-06-01'::date"),
                    NodeId = table.Column<long>(nullable: false),
                    Visible = table.Column<BitArray[]>(type: "bit varying(23)[]", nullable: false, defaultValueSql: "'{11000000000000000000000}'::bit varying[]"),
                    Deleted = table.Column<bool>(nullable: false),
                    Security = table.Column<BitArray[]>(type: "bit varying(15)[]", nullable: false, defaultValueSql: "'{11110001111001}'::bit varying[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DomainNodes",
                columns: table => new
                {
                    NodeID = table.Column<long>(nullable: false),
                    Domain = table.Column<string>(maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DomainNodes_pkey", x => new { x.NodeID, x.Domain });
                    table.ForeignKey(
                        name: "DomainNodes_NodeID_fkey",
                        column: x => x.NodeID,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IPNodes",
                columns: table => new
                {
                    NodeID = table.Column<long>(nullable: false),
                    Address = table.Column<IPAddress>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("IPNodes_pkey", x => new { x.NodeID, x.Address });
                    table.ForeignKey(
                        name: "IPNodes_NodeID_fkey",
                        column: x => x.NodeID,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BadUsers",
                columns: table => new
                {
                    UID = table.Column<long>(nullable: false),
                    BadUID = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("BadUsers_pkey", x => new { x.UID, x.BadUID });
                    table.ForeignKey(
                        name: "BadUsers_UID_fkey",
                        column: x => x.BadUID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "BadUsers_BadUID_fkey",
                        column: x => x.UID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Emails",
                columns: table => new
                {
                    UserId = table.Column<long>(nullable: false),
                    EmailAddress = table.Column<string>(maxLength: 320, nullable: false),
                    Confirmed = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Emails_pkey", x => new { x.UserId, x.EmailAddress, x.Confirmed });
                    table.ForeignKey(
                        name: "Emails_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EncryptedKeys",
                columns: table => new
                {
                    UserId = table.Column<long>(nullable: false),
                    Key = table.Column<byte[]>(nullable: false),
                    KeyInitVector = table.Column<byte[]>(nullable: false),
                    CreationTime = table.Column<DateTime>(nullable: false, defaultValueSql: "'0001-01-01 00:00:00'::timestamp without time zone")
                },
                constraints: table =>
                {
                    table.PrimaryKey("EncryptedKeys_pkey", x => new { x.UserId, x.Key });
                    table.ForeignKey(
                        name: "EncryptedKeys_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FilesInfo",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 256, nullable: false, defaultValueSql: "''::character varying"),
                    NumericId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    UploaderId = table.Column<long>(nullable: false),
                    NodeId = table.Column<long>(nullable: false),
                    Hash = table.Column<byte[]>(nullable: false, defaultValueSql: "'\\x'::bytea"),
                    URL = table.Column<string>(maxLength: 300, nullable: true),
                    UploadDate = table.Column<DateTime>(nullable: false),
                    FileName = table.Column<string>(maxLength: 100, nullable: false, defaultValueSql: "'Unnamed File'::character varying"),
                    Size = table.Column<long>(nullable: true),
                    Deleted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilesInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilesInfo_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FilesInfo_UploaderId_fkey",
                        column: x => x.UploaderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Phones",
                columns: table => new
                {
                    UserId = table.Column<long>(nullable: false),
                    PhoneNumber = table.Column<string>(maxLength: 20, nullable: false),
                    Confirmed = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Phones_pkey", x => new { x.UserId, x.PhoneNumber, x.Confirmed });
                    table.ForeignKey(
                        name: "Phones_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    UserId = table.Column<long>(nullable: false),
                    AccessToken = table.Column<string>(maxLength: 300, nullable: false),
                    RefreshToken = table.Column<string>(maxLength: 300, nullable: false),
                    RefreshCreated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                    table.ForeignKey(
                        name: "Tokens_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatUsers",
                columns: table => new
                {
                    UserId = table.Column<long>(nullable: false),
                    ChatId = table.Column<long>(nullable: false),
                    UserRole = table.Column<byte>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    Banned = table.Column<bool>(nullable: false, defaultValueSql: "false"),
                    Joined = table.Column<DateTime>(nullable: true),
                    LastReadedChatMessageId = table.Column<long>(nullable: true),
                    InviterId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ChatUsers_pkey", x => new { x.UserId, x.ChatId });
                    table.ForeignKey(
                        name: "ChatUsers_InviterId_fkey",
                        column: x => x.InviterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "ChatUsers_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateSend = table.Column<DateTime>(nullable: false),
                    SenderId = table.Column<long>(nullable: false),
                    Replyto = table.Column<long>(nullable: true),
                    Text = table.Column<string>(maxLength: 10000, nullable: true),
                    ChatId = table.Column<long>(nullable: true),
                    DialogId = table.Column<long>(nullable: true),
                    Read = table.Column<bool>(nullable: false),
                    SameMessageId = table.Column<long>(nullable: true),
                    GlobalId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "Messages_Replyto_fkey",
                        column: x => x.Replyto,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "Messages_SenderId_fkey",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Type = table.Column<short>(nullable: false),
                    Hash = table.Column<byte[]>(nullable: false),
                    MessageId = table.Column<long>(nullable: false),
                    Payload = table.Column<string>(maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "Attachments_MessageId_fkey",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chats",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false, defaultValueSql: "'CHAT'::character varying"),
                    Tag = table.Column<int>(nullable: true),
                    Photo = table.Column<string>(maxLength: 300, nullable: true),
                    About = table.Column<string>(maxLength: 1000, nullable: true),
                    Visible = table.Column<BitArray>(type: "bit varying(8)", nullable: false, defaultValueSql: "B'11000000'::\"bit\""),
                    Public = table.Column<BitArray>(type: "bit varying(3)", nullable: false, defaultValueSql: "B'010'::\"bit\""),
                    Deleted = table.Column<bool>(nullable: false),
                    LastMessageId = table.Column<long>(nullable: true),
                    NodesId = table.Column<long[]>(nullable: true),
                    Type = table.Column<short>(nullable: false),
                    Security = table.Column<BitArray>(type: "bit varying(3)", nullable: false, defaultValueSql: "B'001'::bit varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chats", x => x.Id);
                    table.ForeignKey(
                        name: "Chats_LastMessageId_fkey",
                        column: x => x.LastMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Dialogs",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    FirstUID = table.Column<long>(nullable: false),
                    SecondUID = table.Column<long>(nullable: false),
                    Security = table.Column<BitArray>(type: "bit varying(2)", nullable: false, defaultValueSql: "B'10'::\"bit\""),
                    LastMessageId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dialogs", x => x.Id);
                    table.ForeignKey(
                        name: "Dialogs_FirstUID_fkey",
                        column: x => x.FirstUID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "Dialogs_LastMessageId_fkey",
                        column: x => x.LastMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "Dialogs_SecondUID_fkey",
                        column: x => x.SecondUID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "Attachments_MessageId_idx",
                table: "Attachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "Attachments_Type_idx",
                table: "Attachments",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_BadUsers_BadUID",
                table: "BadUsers",
                column: "BadUID");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_LastMessageId",
                table: "Chats",
                column: "LastMessageId");

            migrationBuilder.CreateIndex(
                name: "ChatUsers_Banned_idx",
                table: "ChatUsers",
                column: "Banned");

            migrationBuilder.CreateIndex(
                name: "IX_ChatUsers_ChatId",
                table: "ChatUsers",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "ChatUsers_Deleted_idx",
                table: "ChatUsers",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ChatUsers_InviterId",
                table: "ChatUsers",
                column: "InviterId");

            migrationBuilder.CreateIndex(
                name: "Dialogs_FirstUID_idx",
                table: "Dialogs",
                column: "FirstUID");

            migrationBuilder.CreateIndex(
                name: "IX_Dialogs_LastMessageId",
                table: "Dialogs",
                column: "LastMessageId");

            migrationBuilder.CreateIndex(
                name: "Dialogs_SecondUID_idx",
                table: "Dialogs",
                column: "SecondUID");

            migrationBuilder.CreateIndex(
                name: "Dialogs_FirstUID_SecondUID_idx",
                table: "Dialogs",
                columns: new[] { "FirstUID", "SecondUID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FilesInfo_NodeId",
                table: "FilesInfo",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "FilesInfo_NumericId_idx",
                table: "FilesInfo",
                column: "NumericId");

            migrationBuilder.CreateIndex(
                name: "FilesInfo_UploadDate_idx",
                table: "FilesInfo",
                column: "UploadDate");

            migrationBuilder.CreateIndex(
                name: "FilesInfo_UploaderId_idx",
                table: "FilesInfo",
                column: "UploaderId");

            migrationBuilder.CreateIndex(
                name: "Messages_ChatId_idx",
                table: "Messages",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "Messages_DateSend_idx",
                table: "Messages",
                column: "DateSend");

            migrationBuilder.CreateIndex(
                name: "Messages_DialogId_idx",
                table: "Messages",
                column: "DialogId");

            migrationBuilder.CreateIndex(
                name: "Messages_Read_idx",
                table: "Messages",
                column: "Read");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Replyto",
                table: "Messages",
                column: "Replyto");

            migrationBuilder.CreateIndex(
                name: "Messages_SenderId_idx",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "Messages_ChatId_SenderId_idx",
                table: "Messages",
                columns: new[] { "ChatId", "SenderId" });

            migrationBuilder.CreateIndex(
                name: "Messages_GlobalId_ChatId_idx",
                table: "Messages",
                columns: new[] { "GlobalId", "ChatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Messages_GlobalId_DialogId_idx",
                table: "Messages",
                columns: new[] { "GlobalId", "DialogId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Tokens_UserId_idx",
                table: "Tokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "ChatUsers_ChatId_fkey",
                table: "ChatUsers",
                column: "ChatId",
                principalTable: "Chats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "Messages_ChatId_fkey",
                table: "Messages",
                column: "ChatId",
                principalTable: "Chats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "Messages_DialogId_fkey",
                table: "Messages",
                column: "DialogId",
                principalTable: "Dialogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);            
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Chats_LastMessageId_fkey",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "Dialogs_LastMessageId_fkey",
                table: "Dialogs");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "BadUsers");

            migrationBuilder.DropTable(
                name: "ChatUsers");

            migrationBuilder.DropTable(
                name: "DomainNodes");

            migrationBuilder.DropTable(
                name: "Emails");

            migrationBuilder.DropTable(
                name: "EncryptedKeys");

            migrationBuilder.DropTable(
                name: "FilesInfo");

            migrationBuilder.DropTable(
                name: "IPNodes");

            migrationBuilder.DropTable(
                name: "Phones");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "Nodes");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Chats");

            migrationBuilder.DropTable(
                name: "Dialogs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
