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
using System.Diagnostics;
using System.Threading.Tasks;

namespace NodeApp.Helpers
{
    public static class DockerCleanHepler
    {
        public static void Clear()
        {
            try
            {                
                string command = "\"docker system prune -f\"";
                Task.Run(() => 
                {
                    Process clearProcess = Process.Start("/bin/bash", $"-c {command}");
                    clearProcess.WaitForExit();
                    if(clearProcess.ExitCode != 0)
                    {
                        Console.WriteLine("DOCKER SYSTEM PRUNE RETURNED ERROR.");
                    }
                    else
                    {
                        Console.WriteLine("DOCKER SYSTEM PRUNE SUCCESS.");
                    }
                });                

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
