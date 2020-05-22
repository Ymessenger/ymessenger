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
using Microsoft.EntityFrameworkCore.Design;
using NodeApp.Objects.SettingsObjects;
using ObjectsLibrary.Blockchain.Entities;

namespace NodeApp.Blockchain
{
    public class BlockchainDbDesignTimeDbContextFactory : IDesignTimeDbContextFactory<BlockchainDbContext>
    {
        public BlockchainDbContext CreateDbContext(string[] args)
        {
            Configs connectionSettings = new Configs("Config/appsettings.json");
            string connectionString = connectionSettings.BlockchainDbConnection.ToString();
            return new BlockchainDbContext(connectionString);
        }
    }
}
