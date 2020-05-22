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
    public partial class AddNodesKeysTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NodesKeys",
                columns: table => new
                {
                    NodeId = table.Column<long>(nullable: false),
                    KeyId = table.Column<long>(nullable: false),
                    PublicKey = table.Column<byte[]>(nullable: true),
                    PrivateKey = table.Column<byte[]>(nullable: true),
                    SymmetricKey = table.Column<byte[]>(nullable: true),
                    Password = table.Column<byte[]>(nullable: true),
                    ExpirationTime = table.Column<long>(nullable: false),
                    GenerationTime = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodesKeys", x => new { x.NodeId, x.KeyId });
                    table.ForeignKey(
                        name: "FK_NodesKeys_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NodesKeys");
        }
    }
}
