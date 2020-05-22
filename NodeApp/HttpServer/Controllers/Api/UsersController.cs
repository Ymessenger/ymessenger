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
using NodeApp.Extensions;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ILoadUsersService _loadUsersService;
        private readonly IUpdateUsersService _updateUsersService;
        private readonly IConnectionsService _connectionsService;
        public UsersController(ILoadUsersService loadUsersService, IUpdateUsersService updateUsersService, IConnectionsService connectionsService)
        {
            _loadUsersService = loadUsersService;
            _updateUsersService = updateUsersService;
            _connectionsService = connectionsService;
        }
        [HttpPost]
        public async Task<List<UserVm>> GetUsers([FromForm]List<long> usersIds)
        {
            if (usersIds.IsNullOrEmpty())
            {
                return new List<UserVm>();
            }
            return await _loadUsersService.GetUsersByIdAsync(usersIds).ConfigureAwait(false);
        }
        public async Task<IActionResult> Ban([FromForm] long userId)
        {
            _connectionsService.RemoveAllUserClientConnections(userId);
            var user = await _updateUsersService.SetUserBannedAsync(userId).ConfigureAwait(false);
            return new JsonResult(user);
        }
    }    
}