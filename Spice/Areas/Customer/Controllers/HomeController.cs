using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;

namespace Spice.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            LandingPageViewModel landingPageViewModel = new LandingPageViewModel()
            {
                MenuItems = await _db.MenuItems.Include(m => m.Category).Include(m => m.SubCategory).ToListAsync(),
                Categories = await _db.Categories.ToListAsync(),
                Coupons = await _db.Coupons.ToListAsync()
            };

            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                var shoppingCartCounts = _db.ShoppingCarts.Count(s => s.ApplicationUserId == claim.Value);
                //Session Shopping Cart Counts == sscc
                HttpContext.Session.SetInt32(Utility.Constants.sessionShoppingCartCounts, shoppingCartCounts);
            }

            return View(landingPageViewModel);
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            if (id != 0)
            {
                var menuItem = await _db.MenuItems.Include(m => m.Category)
                                                .Include(m => m.SubCategory)
                                                .Where(m => m.Id == id)
                                                .FirstOrDefaultAsync();

                if (menuItem != null)
                {
                    ShoppingCart shoppingCart = new ShoppingCart()
                    {
                        MenuItem = menuItem,
                        MenuItemId = menuItem.Id
                    };
                    return View(shoppingCart);
                }
            }

            return NotFound();
        }

        [HttpPost, Authorize]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Details(ShoppingCart cart)
        {
            //This is set up to zero, because it will take the same value as MenuItem.Id
            cart.Id = 0;

            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                cart.ApplicationUserId = claim.Value;

                ShoppingCart shoppingCart = await _db.ShoppingCarts
                                                        .Where(s => s.ApplicationUserId == cart.ApplicationUserId
                                                                    && s.MenuItemId == cart.MenuItemId)
                                                        .FirstOrDefaultAsync();

                if (shoppingCart != null)
                {
                    shoppingCart.Count += cart.Count;
                }
                else
                {
                    await _db.ShoppingCarts.AddAsync(cart);
                }
                await _db.SaveChangesAsync();

                //Sessions. To keep track of shopping carts in the session of the user.
                var shoppingCartCounts = _db.ShoppingCarts.Count(s => s.ApplicationUserId == cart.ApplicationUserId);
                //Session Shopping Cart Counts == sscc
                HttpContext.Session.SetInt32(Utility.Constants.sessionShoppingCartCounts, shoppingCartCounts);

                return RedirectToAction(nameof(Index));
            }
            return View(cart);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
