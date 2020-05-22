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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodeApp.HttpServer.Models.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers
{
    [Authorize]
    public class LogsController : Controller
    {
        private readonly DirectoryInfo _directoryInfo = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
        [HttpGet]
        public IActionResult Index()
        {
            var logFiles = _directoryInfo.GetFiles();            
            List<LogViewModel> models = new List<LogViewModel>();
            foreach (var fileInfo in logFiles.OrderByDescending(f => f.LastWriteTime))
            {
                models.Add(new LogViewModel
                {
                    LogName = fileInfo.Name,
                    Size = (int)fileInfo.Length,
                    Time = fileInfo.LastWriteTime
                });
            }
            return View(models);
        }
        [HttpGet]
        public async Task<IActionResult> Details([FromQuery] string logName)
        {
            var logFiles = _directoryInfo.GetFiles();
            var fileInfo = logFiles.FirstOrDefault(info => info.Name == logName);
            var logContent = await System.IO.File.ReadAllTextAsync(fileInfo.FullName).ConfigureAwait(false);
            return View(new LogViewModel
            {
                LogName = fileInfo.Name,
                Size = (int)fileInfo.Length,
                Time = fileInfo.LastWriteTime,
                Text = logContent
            });
        }
        [HttpGet]
        public IActionResult Delete([FromQuery] string logName = null, bool? deleteAll = null)
        {
            var logFiles = _directoryInfo.GetFiles();
            if (deleteAll == true)
            {
                foreach (var file in logFiles)
                {
                    System.IO.File.Delete(file.FullName);
                }               
            }
            else if (!string.IsNullOrWhiteSpace(logName))
            {
                var fileInfo = logFiles.FirstOrDefault(info => info.Name == logName);
                if (fileInfo != null)
                {
                    System.IO.File.Delete(fileInfo.FullName);
                }                
            }
            return RedirectToAction("Index");
        }
    }
}