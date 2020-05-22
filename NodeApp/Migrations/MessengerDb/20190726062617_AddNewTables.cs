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
using Microsoft.EntityFrameworkCore.Migrations;

namespace NodeApp.Migrations.MessengerDb
{
    public partial class AddNewTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LastMessageGlobalId",
                table: "Dialogs",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastReadedGlobalMessageId",
                table: "ChatUsers",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastMessageGlobalId",
                table: "Chats",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastReadedGlobalMessageId",
                table: "ChannelUsers",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastMessageGlobalId",
                table: "Channels",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Polls",
                columns: table => new
                {
                    PollId = table.Column<Guid>(nullable: false),
                    ConvertsationId = table.Column<long>(nullable: false),
                    ConversationType = table.Column<byte>(nullable: false),
                    Title = table.Column<string>(maxLength: 100, nullable: true),
                    MultipleSelection = table.Column<bool>(nullable: false),
                    ResultsVisibility = table.Column<bool>(nullable: false),
                    CreatorId = table.Column<long>(nullable: false),
                    UserId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Polls", x => new { x.PollId, x.ConversationType, x.ConvertsationId });
                    table.ForeignKey(
                        name: "FK_Polls_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PollsOptions",
                columns: table => new
                {
                    OptionId = table.Column<byte>(nullable: false),
                    PollId = table.Column<Guid>(nullable: false),
                    ConversationId = table.Column<long>(nullable: false),
                    ConversationType = table.Column<byte>(nullable: false),
                    Description = table.Column<string>(maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollsOptions", x => new { x.OptionId, x.PollId, x.ConversationType, x.ConversationId });
                    table.ForeignKey(
                        name: "FK_PollsOptions_Polls_PollId_ConversationType_ConversationId",
                        columns: x => new { x.PollId, x.ConversationType, x.ConversationId },
                        principalTable: "Polls",
                        principalColumns: new[] { "PollId", "ConversationType", "ConvertsationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollsOptionsVotes",
                columns: table => new
                {
                    UserId = table.Column<long>(nullable: false),
                    PollId = table.Column<Guid>(nullable: false),
                    ConversationId = table.Column<long>(nullable: false),
                    ConversationType = table.Column<byte>(nullable: false),
                    OptionId = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollsOptionsVotes", x => new { x.OptionId, x.UserId, x.PollId, x.ConversationType, x.ConversationId });
                    table.ForeignKey(
                        name: "FK_PollsOptionsVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PollsOptionsVotes_PollsOptions_OptionId_PollId_Conversation~",
                        columns: x => new { x.OptionId, x.PollId, x.ConversationType, x.ConversationId },
                        principalTable: "PollsOptions",
                        principalColumns: new[] { "OptionId", "PollId", "ConversationType", "ConversationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Polls_UserId",
                table: "Polls",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PollsOptions_PollId_ConversationType_ConversationId",
                table: "PollsOptions",
                columns: new[] { "PollId", "ConversationType", "ConversationId" });

            migrationBuilder.CreateIndex(
                name: "IX_PollsOptionsVotes_UserId",
                table: "PollsOptionsVotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PollsOptionsVotes_OptionId_PollId_ConversationType_Conversa~",
                table: "PollsOptionsVotes",
                columns: new[] { "OptionId", "PollId", "ConversationType", "ConversationId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PollsOptionsVotes");

            migrationBuilder.DropTable(
                name: "PollsOptions");

            migrationBuilder.DropTable(
                name: "Polls");

            migrationBuilder.DropColumn(
                name: "LastMessageGlobalId",
                table: "Dialogs");

            migrationBuilder.DropColumn(
                name: "LastReadedGlobalMessageId",
                table: "ChatUsers");

            migrationBuilder.DropColumn(
                name: "LastMessageGlobalId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "LastReadedGlobalMessageId",
                table: "ChannelUsers");

            migrationBuilder.DropColumn(
                name: "LastMessageGlobalId",
                table: "Channels");
        }
    }
}
