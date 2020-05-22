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

namespace NodeApp.Migrations.MessengerDb
{
    public partial class PublicKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsersPublicKeys",
                columns: table => new
                {
                    RecordId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    UserId = table.Column<long>(nullable: true),
                    ChatId = table.Column<long>(nullable: true),
                    KeyId = table.Column<long>(nullable: false),
                    KeyData = table.Column<byte[]>(nullable: true),
                    ExpirationTimeSeconds = table.Column<long>(nullable: true),
                    GenerationTimeSeconds = table.Column<long>(nullable: false),
                    Version = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersPublicKeys", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK_UsersPublicKeys_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "UsersPublicKeys_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsersPublicKeys_ChatId",
                table: "UsersPublicKeys",
                column: "ChatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsersPublicKeys_UserId",
                table: "UsersPublicKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsersPublicKeys_KeyId_ChatId",
                table: "UsersPublicKeys",
                columns: new[] { "KeyId", "ChatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsersPublicKeys_KeyId_UserId",
                table: "UsersPublicKeys",
                columns: new[] { "KeyId", "UserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsersPublicKeys");
        }
    }
}
