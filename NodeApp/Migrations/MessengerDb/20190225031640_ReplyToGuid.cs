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
    public partial class ReplyToGuid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {                    
            migrationBuilder.DropForeignKey(
                name: "Messages_Replyto_fkey",
                table: "Messages");

            migrationBuilder.Sql("ALTER TABLE \"Messages\" ALTER COLUMN \"Replyto\" SET DATA TYPE UUID USING (null);");

            migrationBuilder.AlterColumn<Guid>(
                name: "Replyto",
                table: "Messages",
                nullable: true,
                oldClrType: typeof(long),
                oldNullable: true);           
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Messages_Replyto_fkey",
                table: "Messages");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Messages_GlobalId",
                table: "Messages");

            migrationBuilder.AlterColumn<long>(
                name: "Replyto",
                table: "Messages",
                nullable: true,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "Messages_Replyto_fkey",
                table: "Messages",
                column: "Replyto",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
