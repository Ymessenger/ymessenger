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
    public partial class AddMessagesSearchVector : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Messages",
                nullable: true);

            migrationBuilder.CreateIndex(
               name: "IX_Messages_SearchVector",
               table: "Messages",
               column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION tsvector_update_trigger_multilang() RETURNS TRIGGER AS $message$
	                                BEGIN
		                                IF NEW.""Text"" IS NOT NULL THEN
                                            NEW.""SearchVector"" = (SELECT to_tsvector('english', NEW.""Text"") ||
                                                                           to_tsvector('russian', NEW.""Text"") ||
                                                                           to_tsvector('simple', NEW.""Text""));
                                        END IF;
                                        RETURN NEW;
                                    END;
                                  $message$ LANGUAGE plpgsql; ");

            migrationBuilder.Sql(
                @"CREATE TRIGGER messages_search_vector_update BEFORE INSERT OR UPDATE
                 ON ""Messages"" FOR EACH ROW EXECUTE PROCEDURE
                    tsvector_update_trigger_multilang();");
            migrationBuilder.Sql(@"UPDATE ""Messages"" SET ""Text""=""Text""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER messages_search_vector_update");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Messages");
        }
    }
}
