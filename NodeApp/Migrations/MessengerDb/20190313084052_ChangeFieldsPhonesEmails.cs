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
    public partial class ChangeFieldsPhonesEmails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "Phones_pkey",
                table: "Phones");

            migrationBuilder.DropPrimaryKey(
                name: "Emails_pkey",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "Confirmed",
                table: "Emails");

            migrationBuilder.RenameColumn(
                name: "Confirmed",
                table: "Phones",
                newName: "Main");

            migrationBuilder.AddPrimaryKey(
                name: "Phones_pkey",
                table: "Phones",
                columns: new[] { "UserId", "PhoneNumber" });

            migrationBuilder.AddPrimaryKey(
                name: "Emails_pkey",
                table: "Emails",
                columns: new[] { "UserId", "EmailAddress" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "Phones_pkey",
                table: "Phones");

            migrationBuilder.DropPrimaryKey(
                name: "Emails_pkey",
                table: "Emails");

            migrationBuilder.RenameColumn(
                name: "Main",
                table: "Phones",
                newName: "Confirmed");

            migrationBuilder.AddColumn<bool>(
                name: "Confirmed",
                table: "Emails",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "Phones_pkey",
                table: "Phones",
                columns: new[] { "UserId", "PhoneNumber", "Confirmed" });

            migrationBuilder.AddPrimaryKey(
                name: "Emails_pkey",
                table: "Emails",
                columns: new[] { "UserId", "EmailAddress", "Confirmed" });
        }
    }
}
