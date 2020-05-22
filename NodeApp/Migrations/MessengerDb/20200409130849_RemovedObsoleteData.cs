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
    public partial class RemovedObsoleteData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Administrators");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "PrivateKey",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "SymmetricKey",
                table: "Nodes");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Password",
                table: "Nodes",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PrivateKey",
                table: "Nodes",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PublicKey",
                table: "Nodes",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "SymmetricKey",
                table: "Nodes",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Administrators",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    Password = table.Column<string>(maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Administrators", x => x.Id);
                });
        }
    }
}
