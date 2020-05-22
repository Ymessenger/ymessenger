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
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.Helpers
{
    public class DbConnectionChecker
    {
        public async Task<bool> CheckDbConnectionAsync(string connectionString)
        {
            Errors = new Dictionary<byte, string>();
            byte attempts = 0;
            const byte ATTEMPTS_COUNT = 10;
            while (attempts < ATTEMPTS_COUNT)
            {
                try
                {
                    using (NpgsqlConnection connection = new NpgsqlConnection(connectionString)) 
                    {
                        await connection.OpenAsync();
                        return true;
                    }
                }
                catch(Exception ex)
                {
                    Errors.Add(attempts, ex.ToString());
                    await Task.Delay(TimeSpan.FromSeconds(1));                    
                    attempts++;
                }
            }
            return false;
        }
        public Dictionary<byte, string> Errors { get; private set; }
    }
}
