using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Utility.Constants.ManagerUser)]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public UsersController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            return View(await _db.ApplicationUsers.Where(u => u.Id != claim.Value).ToListAsync());
        }

        public async Task<IActionResult> Lock(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                var applicationUser = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == id);

                if (applicationUser != null)
                {
                    applicationUser.LockoutEnd = DateTime.Now.AddYears(100);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> UnLock(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                var applicationUser = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == id);

                if (applicationUser != null)
                {
                    applicationUser.LockoutEnd = null;
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }
    }
}