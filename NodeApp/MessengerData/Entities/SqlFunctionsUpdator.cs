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
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Entities
{
    public static class SqlFunctionsUpdator
    {
        public static async Task UpdateFunctionsAsync(string pathToSqlFiles)
        {
            try
            {
                IEnumerable<string> fileNames = Directory.GetFiles(pathToSqlFiles);
                DbContextOptionsBuilder<MessengerDbContext> optionsBuilder = new DbContextOptionsBuilder<MessengerDbContext>()
                    .UseNpgsql(NodeSettings.Configs.MessengerDbConnection.ToString());
                using (MessengerDbContext context = new MessengerDbContext(optionsBuilder.Options))
                {
                    foreach (string file in fileNames)
                    {
                        if (file.Contains(".sql"))
                        {
                            string functionSql = File.ReadAllText(file);
                            await context.Database.ExecuteSqlCommandAsync(functionSql).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                Logger.WriteLog("DbBackup directory not found.");
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
    }
}
