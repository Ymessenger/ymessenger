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
    public partial class AddTextSearchColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ///Add chats text search
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Chats",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chats_SearchVector",
                table: "Chats",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.Sql(
                @"CREATE TRIGGER chats_search_vector_update BEFORE INSERT OR UPDATE
                 ON ""Chats"" FOR EACH ROW EXECUTE PROCEDURE
                tsvector_update_trigger(""SearchVector"", 'pg_catalog.simple', ""Name"", ""Tag"");");
            migrationBuilder.Sql(@"UPDATE ""Chats"" SET ""Name""=""Name""");

            ///Add channels text search
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Channels",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channels_SearchVector",
                table: "Channels",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.Sql(
                @"CREATE TRIGGER channels_search_vector_update BEFORE INSERT OR UPDATE
                 ON ""Channels"" FOR EACH ROW EXECUTE PROCEDURE
                tsvector_update_trigger(""SearchVector"", 'pg_catalog.simple', ""ChannelName"", ""Tag"");");
            migrationBuilder.Sql(@"UPDATE ""Channels"" SET ""ChannelName""=""ChannelName""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
               name: "SearchVector",
               table: "Chats");

            migrationBuilder.DropColumn(
               name: "SearchVector",
               table: "Channels");
        }
    }
}
