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
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NodeApp.Migrations.MessengerDb
{
    public partial class StoredProcedures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            try
            {
                IEnumerable<string> fileNames = Directory.GetFiles("DbBackup");
                foreach (string file in fileNames)
                {                   
                    string function_sql = File.ReadAllText(file);
                    migrationBuilder.Sql(function_sql, true);
                }
            }
            catch(DirectoryNotFoundException)
            {
                Logger.WriteLog("DbBackup directory not found.");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
