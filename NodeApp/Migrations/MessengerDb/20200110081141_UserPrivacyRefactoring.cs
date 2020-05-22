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
using System.Collections;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NodeApp.Migrations.MessengerDb
{
    public partial class UserPrivacyRefactoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
               name: "Privacy",
               table: "Users",
               nullable: false,
               defaultValue: 0);

            migrationBuilder.Sql(@"UPDATE ""Users"" SET ""Privacy""=rpad(""Visible""[1]::text, 32, '0')::bit(32)::int");

            migrationBuilder.DropColumn(
                name: "Visible",
                table: "Users");           
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {  
            migrationBuilder.AddColumn<BitArray[]>(
                name: "Visible",
                table: "Users",
                type: "bit varying(32)[]",
                nullable: false,
                defaultValueSql: "'{11000000000000000000000}'::bit varying[]");

            migrationBuilder.DropColumn(
               name: "Privacy",
               table: "Users");
        }
    }
}
