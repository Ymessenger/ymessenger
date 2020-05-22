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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodeApp.Areas.Identity.Data;
using NodeApp.Areas.Identity.Models;
using NodeApp.HttpServer.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AdminDbContext _context;
        private readonly UserManager<AdminUser> _userManager;        
        public AdminController(AdminDbContext context, UserManager<AdminUser> userManager)
        {
            _context = context;
            _userManager = userManager;           
        }
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync().ConfigureAwait(false));
        }
        [HttpGet]
        public async Task<IActionResult> Edit([FromQuery] string id)
        {
            var user = await _userManager.FindByIdAsync(id).ConfigureAwait(false);
            return PartialView(new UpdateAdminModel
            {
                Banned = user.Banned,
                Email = user.Email,
                Id = user.Id,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName
            });
        }
        [HttpPost]
        public async Task<IActionResult> Edit([FromForm]UpdateAdminModel user)
        {
            var edited = await _context.Users.FindAsync(user.Id).ConfigureAwait(false);
            edited.UserName = user.UserName;
            edited.Email = user.Email;
            edited.PhoneNumber = user.PhoneNumber;
            edited.Banned = user.Banned;
            _context.Entry(edited).State = EntityState.Modified;
            if (user.Banned)
            {
                var userClaims = await _context.UserClaims.Where(claim => claim.UserId == user.Id).ToListAsync().ConfigureAwait(false);
                _context.UserClaims.RemoveRange(userClaims);
            }
            await _context.SaveChangesAsync().ConfigureAwait(false);
            return Redirect(nameof(Index));
        }
    }
}