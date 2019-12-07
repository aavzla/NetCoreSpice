using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models.ViewModels;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ShoppingCartsController : Controller
    {
        private readonly ApplicationDbContext _db;

        [BindProperty]
        public OrderShoppingCartsViewModel viewModel { get; set; }

        public ShoppingCartsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            viewModel = new OrderShoppingCartsViewModel()
            {
                OrderInfo = new Models.OrderInfo(),
                ShoppingCarts = new List<Models.ShoppingCart>()
            };

            //?
            viewModel.OrderInfo.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var shoppingCarts = _db.ShoppingCarts.Where(s => s.ApplicationUserId == claim.Value);

            if (shoppingCarts != null)
            {
                viewModel.ShoppingCarts = shoppingCarts.ToList();
            }

            foreach (var shoppingCart in viewModel.ShoppingCarts)
            {
                shoppingCart.MenuItem = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == shoppingCart.MenuItemId);
                viewModel.OrderInfo.OrderSubTotal += shoppingCart.MenuItem.Price * shoppingCart.Count;

                shoppingCart.MenuItem.Description = Utility.StaticMethods.ConvertToRawHtml(shoppingCart.MenuItem.Description);
                if (shoppingCart.MenuItem.Description.Length > 100)
                {
                    var first100CharsDescription = shoppingCart.MenuItem.Description.Substring(0, 99);
                    var lastSpaceIndex = first100CharsDescription.LastIndexOf(' ');
                    shoppingCart.MenuItem.Description = first100CharsDescription.Substring(0, lastSpaceIndex + 1) + "...";
                }
            }

            viewModel.OrderInfo.OrderTotal = viewModel.OrderInfo.OrderSubTotal;

            if (HttpContext.Session.GetString(Utility.Constants.sessionCouponCode) != null)
            {
                viewModel.OrderInfo.CouponCode = HttpContext.Session.GetString(Utility.Constants.sessionCouponCode);
                var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Name.ToLower() == viewModel.OrderInfo.CouponCode.ToLower());
                viewModel.OrderInfo.OrderTotal = Utility.StaticMethods.DiscountedPrice(coupon, viewModel.OrderInfo.OrderSubTotal);
            }

            return View(viewModel);
        }

        public IActionResult AddCoupon()
        {
            if (viewModel.OrderInfo.CouponCode == null)
            {
                viewModel.OrderInfo.CouponCode = string.Empty;
            }

            HttpContext.Session.SetString(Utility.Constants.sessionCouponCode, viewModel.OrderInfo.CouponCode);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.SetString(Utility.Constants.sessionCouponCode, string.Empty);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int shoppingCartId)
        {
            var shoppingCart = await _db.ShoppingCarts.FirstOrDefaultAsync(s => s.Id == shoppingCartId);
            if (shoppingCart != null)
            {
                shoppingCart.Count += 1;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int shoppingCartId)
        {
            var shoppingCart = await _db.ShoppingCarts.FirstOrDefaultAsync(s => s.Id == shoppingCartId);
            if (shoppingCart != null)
            {
                if (shoppingCart.Count == 1)
                {
                    _db.ShoppingCarts.Remove(shoppingCart);
                    await _db.SaveChangesAsync();

                    var shoppingCartCounts = await _db.ShoppingCarts.CountAsync(s => s.ApplicationUserId == shoppingCart.ApplicationUserId);
                    //Session Shopping Cart Counts == sscc
                    HttpContext.Session.SetInt32("sscc", shoppingCartCounts);
                }
                else
                {
                    shoppingCart.Count -= 1;
                    await _db.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int shoppingCartId)
        {
            var shoppingCart = await _db.ShoppingCarts.FirstOrDefaultAsync(s => s.Id == shoppingCartId);
            if (shoppingCart != null)
            {
                _db.ShoppingCarts.Remove(shoppingCart);
                await _db.SaveChangesAsync();

                var shoppingCartCounts = await _db.ShoppingCarts.CountAsync(s => s.ApplicationUserId == shoppingCart.ApplicationUserId);
                //Session Shopping Cart Counts == sscc
                HttpContext.Session.SetInt32("sscc", shoppingCartCounts);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}