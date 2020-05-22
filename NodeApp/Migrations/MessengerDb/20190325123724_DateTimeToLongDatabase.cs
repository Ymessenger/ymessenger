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
    public partial class DateTimeToLongDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ChangeDatetimeToUnixTimestampWithConvertion(
                migrationBuilder,
                "Messages",
                "DateSend",
                "DateSendTemp",
                "SendingTime",
                "Messages_DateSend_idx",
                false);

            ChangeDatetimeToUnixTimestampWithConvertion(
                migrationBuilder,
                "Users",
                "RegistrationDate",
                "RegistrationDateTemp",
                "RegistrationDate",
                null,
                true);

            ChangeDatetimeToUnixTimestampWithConvertion(
                migrationBuilder,
                "Users",
                "Online",
                "OnlineTemp",
                "Online",
                null,
                true);

            ChangeDatetimeToUnixTimestampWithConvertion(
                migrationBuilder,
                "FilesInfo",
                "UploadDate",
                "UploadDateTemp",
                "UploadDate",
                null,
                false);


            ChangeDatetimeToUnixTimestampWithConvertion(
                migrationBuilder,
                "EncryptedKeys",
                "CreationTime",
                "CreationTimeTemp",
                "CreationTime",
                null,
                false);

            ChangeDatetimeToUnixTimestampWithConvertion(
                migrationBuilder,
                "ChatUsers",
                "Joined",
                "JoinedTemp",
                "Joined",
                null,
                true);
        }

        private void ChangeDatetimeToUnixTimestampWithConvertion(MigrationBuilder migrationBuilder, string tableName, string columnName, string tmpColumnName, string newColumnName, string indexName, bool nullable)
        {
            string quotedTableName = $"\"{tableName}\"";
            string quotedTempColumnName = $"\"{tmpColumnName}\"";
            string quotedColumnName = $"\"{columnName}\"";
            migrationBuilder.AddColumn<long>(
                tmpColumnName,
                tableName,
                nullable: nullable,
                defaultValue: 0L);
            
            /*migrationBuilder.Sql($@"UPDATE {quotedTableName}
                                    SET {quotedTempColumnName} = (select * from get_unix_timestamp({quotedColumnName}))");*/

            if (indexName != null)
            {
                migrationBuilder.DropIndex(
                    name: indexName,
                    table: tableName);
            }
            migrationBuilder.DropColumn(
                name: columnName,
                table: tableName);            
            migrationBuilder.RenameColumn(
               name: tmpColumnName,
               table: tableName,
               newName: newColumnName);

            if (indexName != null)
            {
                migrationBuilder.CreateIndex(
                   name: indexName,
                   table: tableName,
                   column: newColumnName);
            }

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "Messages_DateSend_idx",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SendingTime",
                table: "Messages");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegistrationDate",
                table: "Users",
                type: "date",
                nullable: true,
                defaultValueSql: "'2018-06-01'::date",
                oldClrType: typeof(long),
                oldNullable: true,
                oldDefaultValueSql: "-9223372036854775808");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Online",
                table: "Users",
                nullable: true,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateSend",
                table: "Messages",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "UploadDate",
                table: "FilesInfo",
                nullable: false,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationTime",
                table: "EncryptedKeys",
                nullable: false,
                defaultValueSql: "'0001-01-01 00:00:00'::timestamp without time zone",
                oldClrType: typeof(long),
                oldDefaultValueSql: "-9223372036854775808");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Datesend",
                table: "DialogsPreview",
                nullable: true,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Joined",
                table: "ChatUsers",
                nullable: true,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateSend",
                table: "ChatsPreview",
                nullable: true,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "Messages_DateSend_idx",
                table: "Messages",
                column: "DateSend");
        }
    }
}
