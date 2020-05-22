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
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System.Collections;

namespace NodeApp.Migrations.MessengerDb
{
    public partial class AddEditedMessagesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<BitArray[]>(
                name: "Visible",
                table: "Users",
                type: "bit varying(32)[]",
                nullable: false,
                defaultValueSql: "'{11000000000000000000000}'::bit varying[]",
                oldClrType: typeof(BitArray[]),
                oldType: "bit varying(23)[]",
                oldDefaultValueSql: "'{11000000000000000000000}'::bit varying[]");

            migrationBuilder.AlterColumn<string>(
                name: "NameFirst",
                table: "Users",
                maxLength: 30,
                nullable: false,
                defaultValueSql: "'noname'::character varying",
                oldClrType: typeof(string),
                oldMaxLength: 30,
                oldDefaultValueSql: "'unconfirmed'::character varying");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Contacts",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "EditedMessages",
                columns: table => new
                {
                    RecordId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    EditorId = table.Column<long>(nullable: false),
                    MessageId = table.Column<long>(nullable: false),
                    Text = table.Column<string>(nullable: true),
                    UpdatedTime = table.Column<long>(nullable: true),
                    SendingTime = table.Column<long>(nullable: false),
                    Sign = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditedMessages", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK_EditedMessages_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EditedMessagesAttachments",
                columns: table => new
                {
                    RecordId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ActualAttachmentId = table.Column<long>(nullable: false),
                    EditedMessageId = table.Column<long>(nullable: false),
                    AttachmentType = table.Column<byte>(nullable: false),
                    Payload = table.Column<string>(nullable: true),
                    Hash = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditedMessagesAttachments", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK_EditedMessagesAttachments_Attachments_ActualAttachmentId",
                        column: x => x.ActualAttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EditedMessagesAttachments_EditedMessages_EditedMessageId",
                        column: x => x.EditedMessageId,
                        principalTable: "EditedMessages",
                        principalColumn: "RecordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EditedMessages_MessageId",
                table: "EditedMessages",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EditedMessagesAttachments_ActualAttachmentId",
                table: "EditedMessagesAttachments",
                column: "ActualAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EditedMessagesAttachments_EditedMessageId",
                table: "EditedMessagesAttachments",
                column: "EditedMessageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EditedMessagesAttachments");

            migrationBuilder.DropTable(
                name: "EditedMessages");

            migrationBuilder.AlterColumn<BitArray[]>(
                name: "Visible",
                table: "Users",
                type: "bit varying(23)[]",
                nullable: false,
                defaultValueSql: "'{11000000000000000000000}'::bit varying[]",
                oldClrType: typeof(BitArray[]),
                oldType: "bit varying(32)[]",
                oldDefaultValueSql: "'{11000000000000000000000}'::bit varying[]");

            migrationBuilder.AlterColumn<string>(
                name: "NameFirst",
                table: "Users",
                maxLength: 30,
                nullable: false,
                defaultValueSql: "'unconfirmed'::character varying",
                oldClrType: typeof(string),
                oldMaxLength: 30,
                oldDefaultValueSql: "'noname'::character varying");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Contacts",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
