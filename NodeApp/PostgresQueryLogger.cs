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
using System;
using System.IO;
using System.Text;
using Npgsql.Logging;

namespace NodeApp
{
    public class PostgresQueryLogger : NpgsqlLogger, INpgsqlLoggingProvider
    {
        public static object locker = new object();    
        private static string currentFileName;
        private static FileStream fStream;
        public NpgsqlLogger CreateLogger(string name)
        {            
            return new PostgresQueryLogger();
        }
        

        public override bool IsEnabled(NpgsqlLogLevel level)
        {
            return level == NpgsqlLogLevel.Info;
        }

        public override void Log(NpgsqlLogLevel level, int connectorId, string msg, Exception exception = null)
        {             
            lock (locker)
            {                
                currentFileName = $"Logs/PostgresLog{DateTime.Today.ToString("yyyyMMdd")}.txt";
                fStream = new FileStream(currentFileName, FileMode.Append, FileAccess.Write);
                using (StreamWriter streamWriter = new StreamWriter(fStream, Encoding.UTF8))
                {                    
                    streamWriter.Write(Environment.NewLine + msg + Environment.NewLine);
                }
            }
        }
    }
}
