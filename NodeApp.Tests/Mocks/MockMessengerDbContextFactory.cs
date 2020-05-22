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
using Microsoft.EntityFrameworkCore.Diagnostics;
using NodeApp.Interfaces;
using NodeApp.MessengerData.Entities;

namespace NodeApp.Tests.Mocks
{
    public class MockMessengerDbContextFactory : IDbContextFactory<MessengerDbContext>
    {
        private readonly string dbName;
        public MockMessengerDbContextFactory(string dbName)
        {
            this.dbName = dbName;
        }

        public MessengerDbContext Create()
        {
            DbContextOptionsBuilder<MessengerDbContext> optionsBuilder = new DbContextOptionsBuilder<MessengerDbContext>()
               .UseInMemoryDatabase(databaseName: dbName)
               .ConfigureWarnings(options => options.Ignore(InMemoryEventId.TransactionIgnoredWarning))               
               .EnableSensitiveDataLogging(true);
            return new MessengerDbContext(optionsBuilder.Options, true);
        }        
    }
}
