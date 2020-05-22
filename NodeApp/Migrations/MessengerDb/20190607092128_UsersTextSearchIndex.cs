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
using NpgsqlTypes;

namespace NodeApp.Migrations.MessengerDb
{
    public partial class UsersTextSearchIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Users",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SearchVector",
                table: "Users",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.Sql(
                @"CREATE TRIGGER users_search_vector_update BEFORE INSERT OR UPDATE
                 ON ""Users"" FOR EACH ROW EXECUTE PROCEDURE
                tsvector_update_trigger(""SearchVector"", 'pg_catalog.simple', ""NameFirst"", ""NameSecond"", ""Tag"");");
            migrationBuilder.Sql(@"UPDATE ""Users"" SET ""NameFirst""=""NameFirst""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_SearchVector",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Users");
        }
    }
}
