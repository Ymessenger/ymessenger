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

namespace NodeApp.Migrations.MessengerDb
{
    public partial class AddNewColumnsNode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EncryptedKeys");

            migrationBuilder.AddColumn<byte[]>(
                name: "Password",
                table: "Nodes",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "SymmetricKey",
                table: "Nodes",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "SymmetricKey",
                table: "Nodes");

            migrationBuilder.CreateTable(
                name: "EncryptedKeys",
                columns: table => new
                {
                    UserId = table.Column<long>(nullable: false),
                    Key = table.Column<byte[]>(nullable: false),
                    CreationTime = table.Column<long>(nullable: false, defaultValueSql: "-9223372036854775808"),
                    KeyInitVector = table.Column<byte[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("EncryptedKeys_pkey", x => new { x.UserId, x.Key });
                    table.ForeignKey(
                        name: "EncryptedKeys_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
