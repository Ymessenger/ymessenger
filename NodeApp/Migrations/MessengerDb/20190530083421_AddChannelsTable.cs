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
    public partial class AddChannelsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ChannelId",
                table: "Messages",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    ChannelId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ChannelName = table.Column<string>(nullable: true),
                    Tag = table.Column<string>(nullable: true),
                    Photo = table.Column<string>(nullable: true),
                    About = table.Column<string>(nullable: true),
                    LastMessageId = table.Column<long>(nullable: true),
                    NodesId = table.Column<long[]>(nullable: true),
                    Deleted = table.Column<bool>(nullable: false),
                    CreationTime = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.ChannelId);
                    table.ForeignKey(
                        name: "FK_Channels_Messages_LastMessageId",
                        column: x => x.LastMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChannelUsers",
                columns: table => new
                {
                    UserId = table.Column<long>(nullable: false),
                    ChannelId = table.Column<long>(nullable: false),
                    ChannelUserRole = table.Column<byte>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    SubscribedTime = table.Column<long>(nullable: false),
                    LastReadedMessageId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelUsers", x => new { x.UserId, x.ChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelUsers_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "ChannelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelUsers_Messages_LastReadedMessageId",
                        column: x => x.LastReadedMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChannelUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "Messages_ChannelId_idx",
                table: "Messages",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelUsers_ChannelId",
                table: "ChannelUsers",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelUsers_LastReadedMessageId",
                table: "ChannelUsers",
                column: "LastReadedMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_LastMessageId",
                table: "Channels",
                column: "LastMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Channels_ChannelId",
                table: "Messages",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "ChannelId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Channels_ChannelId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "ChannelUsers");

            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropIndex(
                name: "Messages_ChannelId_idx",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "Messages");
        }
    }
}
