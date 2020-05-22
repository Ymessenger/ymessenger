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
    public partial class AddIdTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "SignPrivateKey",
                table: "NodesKeys",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "SignPublicKey",
                table: "NodesKeys",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChannelsIdentificators",
                columns: table => new
                {
                    ChannelId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    IsUsed = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelsIdentificators", x => x.ChannelId);
                });

            migrationBuilder.CreateTable(
                name: "ChatsIdentificators",
                columns: table => new
                {
                    ChatId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    IsUsed = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatsIdentificators", x => x.ChatId);
                });

            migrationBuilder.CreateTable(
                name: "FilesIdentificators",
                columns: table => new
                {
                    FileId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    IsUsed = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilesIdentificators", x => x.FileId);
                });

            migrationBuilder.CreateTable(
                name: "UsersIdentificators",
                columns: table => new
                {
                    UserId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    IsUsed = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersIdentificators", x => x.UserId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelsIdentificators");

            migrationBuilder.DropTable(
                name: "ChatsIdentificators");

            migrationBuilder.DropTable(
                name: "FilesIdentificators");

            migrationBuilder.DropTable(
                name: "UsersIdentificators");

            migrationBuilder.DropColumn(
                name: "SignPrivateKey",
                table: "NodesKeys");

            migrationBuilder.DropColumn(
                name: "SignPublicKey",
                table: "NodesKeys");
        }
    }
}
