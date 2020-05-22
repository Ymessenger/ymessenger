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
    public partial class PengingMessagesAddedNodeId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PendingMessages_Users_ReceiverId",
                table: "PendingMessages");

            migrationBuilder.AlterColumn<long>(
                name: "ReceiverId",
                table: "PendingMessages",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "PendingMessages",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 50000,
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NodeId",
                table: "PendingMessages",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SentAt",
                table: "PendingMessages",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_PendingMessages_NodeId",
                table: "PendingMessages",
                column: "NodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PendingMessages_Nodes_NodeId",
                table: "PendingMessages",
                column: "NodeId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingMessages_Users_ReceiverId",
                table: "PendingMessages",
                column: "ReceiverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PendingMessages_Nodes_NodeId",
                table: "PendingMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingMessages_Users_ReceiverId",
                table: "PendingMessages");

            migrationBuilder.DropIndex(
                name: "IX_PendingMessages_NodeId",
                table: "PendingMessages");

            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "PendingMessages");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "PendingMessages");

            migrationBuilder.AlterColumn<long>(
                name: "ReceiverId",
                table: "PendingMessages",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "PendingMessages",
                maxLength: 50000,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingMessages_Users_ReceiverId",
                table: "PendingMessages",
                column: "ReceiverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}