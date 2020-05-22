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
    public partial class CascadeDeleting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Chats_LastMessageId_fkey",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "Dialogs_LastMessageId_fkey",
                table: "Dialogs");

            migrationBuilder.DropForeignKey(
                name: "Emails_UserId_fkey",
                table: "Emails");

            migrationBuilder.DropForeignKey(
                name: "EncryptedKeys_UserId_fkey",
                table: "EncryptedKeys");

            migrationBuilder.DropForeignKey(
                name: "FilesInfo_UploaderId_fkey",
                table: "FilesInfo");

            migrationBuilder.DropForeignKey(
                name: "Phones_UserId_fkey",
                table: "Phones");

            migrationBuilder.AddForeignKey(
                name: "Chats_LastMessageId_fkey",
                table: "Chats",
                column: "LastMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "Dialogs_LastMessageId_fkey",
                table: "Dialogs",
                column: "LastMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "Emails_UserId_fkey",
                table: "Emails",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "EncryptedKeys_UserId_fkey",
                table: "EncryptedKeys",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FilesInfo_UploaderId_fkey",
                table: "FilesInfo",
                column: "UploaderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "Phones_UserId_fkey",
                table: "Phones",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Chats_LastMessageId_fkey",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "Dialogs_LastMessageId_fkey",
                table: "Dialogs");

            migrationBuilder.DropForeignKey(
                name: "Emails_UserId_fkey",
                table: "Emails");

            migrationBuilder.DropForeignKey(
                name: "EncryptedKeys_UserId_fkey",
                table: "EncryptedKeys");

            migrationBuilder.DropForeignKey(
                name: "FilesInfo_UploaderId_fkey",
                table: "FilesInfo");

            migrationBuilder.DropForeignKey(
                name: "Phones_UserId_fkey",
                table: "Phones");

            migrationBuilder.AddForeignKey(
                name: "Chats_LastMessageId_fkey",
                table: "Chats",
                column: "LastMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "Dialogs_LastMessageId_fkey",
                table: "Dialogs",
                column: "LastMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "Emails_UserId_fkey",
                table: "Emails",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "EncryptedKeys_UserId_fkey",
                table: "EncryptedKeys",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FilesInfo_UploaderId_fkey",
                table: "FilesInfo",
                column: "UploaderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "Phones_UserId_fkey",
                table: "Phones",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
