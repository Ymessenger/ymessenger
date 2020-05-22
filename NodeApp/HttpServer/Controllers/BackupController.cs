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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NodeApp.DbBackup;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers
{
    [Authorize]
    public class BackupController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.BackupList = await DbBackupHelper.GetBackupsListAsync(NodeSettings.Configs.MessengerDbConnection).ConfigureAwait(false);
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Apply([FromQuery] string id, [FromQuery] bool confirm)
        {
            if (confirm)
            {
                bool backupResult = await DbBackupHelper.RestoreDbAsync(NodeSettings.Configs.MessengerDbConnection, id).ConfigureAwait(false);
                if (backupResult)
                {
                    return Content("Backup was applied");
                }
                else
                {
                    return StatusCode(StatusCodes.Status400BadRequest);
                }
            }
            return Redirect(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var backupsList = await DbBackupHelper.GetBackupsListAsync(NodeSettings.Configs.MessengerDbConnection).ConfigureAwait(false);
            string dbName = NodeSettings.Configs.MessengerDbConnection.Database;
            string filename = $"{dbName}_backup_{NodeSettings.Configs.Node.Id}_{DateTime.UtcNow.ToString("ddMMyyyy")}";
            int backupsCount = backupsList.Count(opt => opt.FileId.Contains(filename));
            filename = $"{dbName}_backup_{NodeSettings.Configs.Node.Id}_{DateTime.UtcNow.ToString("ddMMyyyy")}_{backupsCount}";
            bool backupResult = await DbBackupHelper.CreateBackupAsync(NodeSettings.Configs.MessengerDbConnection, filename).ConfigureAwait(false);
            if (backupResult)
            {
                return Redirect(nameof(Index));
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
        }
    }
}