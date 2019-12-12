using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spice.Models;
using Spice.Utility;

namespace Spice.Data
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbInitializer(ApplicationDbContext db, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async void Initializer()
        {
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                //Log should be here
                var exceptionMessage = ex.Message;
            }

            //Creation of the Roles if they do not exists.
            if (!await _roleManager.RoleExistsAsync(Constants.ManagerUser))
            {
                _roleManager.CreateAsync(new IdentityRole(Constants.ManagerUser)).GetAwaiter().GetResult();
            }
            if (!await _roleManager.RoleExistsAsync(Constants.KitchenUser))
            {
                _roleManager.CreateAsync(new IdentityRole(Constants.KitchenUser)).GetAwaiter().GetResult();
            }
            if (!await _roleManager.RoleExistsAsync(Constants.FrontDeskUser))
            {
                _roleManager.CreateAsync(new IdentityRole(Constants.FrontDeskUser)).GetAwaiter().GetResult();
            }
            if (!await _roleManager.RoleExistsAsync(Constants.CustomerUser))
            {
                _roleManager.CreateAsync(new IdentityRole(Constants.CustomerUser)).GetAwaiter().GetResult();
            }

            //Verify if a Manager exists. We need to change the email and phone number of the admin user before run this the first time.
            var roleManager = _db.Roles.FirstOrDefaultAsync(r => r.Name == Constants.ManagerUser).GetAwaiter().GetResult();
            if (roleManager != null && !_db.UserRoles.Any(ur => ur.RoleId == roleManager.Id))
            {
                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin@gmail.com",
                    Email = "admin@gmail.com",
                    Name = "Andres Avilan",
                    EmailConfirmed = true,
                    PhoneNumber = "1112223333"
                }, "Admin123!").GetAwaiter().GetResult();

                IdentityUser user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "admin@gmail.com");

                await _userManager.AddToRoleAsync(user, Constants.ManagerUser);
            }
        }
    }
}
