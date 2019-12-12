using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Stripe;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ShoppingCartsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        [BindProperty]
        public OrderShoppingCartsViewModel viewModel { get; set; }

        public ShoppingCartsController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            viewModel = new OrderShoppingCartsViewModel()
            {
                OrderInfo = new Models.OrderInfo(),
                ShoppingCarts = new List<Models.ShoppingCart>()
            };

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

        public async Task<IActionResult> Summary()
        {
            viewModel = new OrderShoppingCartsViewModel()
            {
                OrderInfo = new Models.OrderInfo(),
                ShoppingCarts = new List<Models.ShoppingCart>()
            };

            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ApplicationUser applicationUser = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == claim.Value);
            var shoppingCarts = _db.ShoppingCarts.Where(s => s.ApplicationUserId == claim.Value);

            if (shoppingCarts != null)
            {
                viewModel.ShoppingCarts = shoppingCarts.ToList();
            }

            foreach (var shoppingCart in viewModel.ShoppingCarts)
            {
                shoppingCart.MenuItem = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == shoppingCart.MenuItemId);
                viewModel.OrderInfo.OrderSubTotal += shoppingCart.MenuItem.Price * shoppingCart.Count;
            }

            viewModel.OrderInfo.OrderTotal = viewModel.OrderInfo.OrderSubTotal;
            viewModel.OrderInfo.PickUpName = applicationUser.Name;
            viewModel.OrderInfo.PhoneNumber = applicationUser.PhoneNumber;
            viewModel.OrderInfo.PickUpTime = DateTime.Now;

            if (HttpContext.Session.GetString(Utility.Constants.sessionCouponCode) != null)
            {
                viewModel.OrderInfo.CouponCode = HttpContext.Session.GetString(Utility.Constants.sessionCouponCode);
                var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Name.ToLower() == viewModel.OrderInfo.CouponCode.ToLower());
                viewModel.OrderInfo.OrderTotal = Utility.StaticMethods.DiscountedPrice(coupon, viewModel.OrderInfo.OrderSubTotal);
            }

            return View(viewModel);
        }

        [HttpPost, ActionName("Summary")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> SummaryPOST(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            viewModel.ShoppingCarts = await _db.ShoppingCarts.Where(s => s.ApplicationUserId == claim.Value).ToListAsync();

            viewModel.OrderInfo.PaymentStatus = Utility.Constants.PaymentStatusPending;
            viewModel.OrderInfo.OrderDate = DateTime.Now;
            viewModel.OrderInfo.ApplicationUserId = claim.Value;
            viewModel.OrderInfo.Status = Utility.Constants.PaymentStatusPending;
            viewModel.OrderInfo.PickUpTime = DateTime.Parse(viewModel.OrderInfo.PickUpDate.ToShortDateString() + " " + viewModel.OrderInfo.PickUpTime.ToShortTimeString());
            
            //Save the Order Info in order to have the Id to be placed and linked to the Order Details
            _db.OrderInfos.Add(viewModel.OrderInfo);
            await _db.SaveChangesAsync();

            OrderDetails orderDetails = null;
            foreach (var shoppingCart in viewModel.ShoppingCarts)
            {
                shoppingCart.MenuItem = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == shoppingCart.MenuItemId);
                orderDetails = new OrderDetails()
                {
                    MenuItemId = shoppingCart.MenuItemId,
                    OrderInfoId = viewModel.OrderInfo.Id,
                    Description = shoppingCart.MenuItem.Description,
                    Name = shoppingCart.MenuItem.Name,
                    Price = shoppingCart.MenuItem.Price,
                    Count = shoppingCart.Count
                };
                viewModel.OrderInfo.OrderSubTotal += orderDetails.Price * orderDetails.Count;
                _db.OrderDetails.Add(orderDetails);
            }


            if (HttpContext.Session.GetString(Utility.Constants.sessionCouponCode) != null)
            {
                viewModel.OrderInfo.CouponCode = HttpContext.Session.GetString(Utility.Constants.sessionCouponCode);
                var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Name.ToLower() == viewModel.OrderInfo.CouponCode.ToLower());
                viewModel.OrderInfo.OrderTotal = Utility.StaticMethods.DiscountedPrice(coupon, viewModel.OrderInfo.OrderSubTotal);
            }
            else
            {
                viewModel.OrderInfo.OrderTotal = viewModel.OrderInfo.OrderSubTotal;
            }

            viewModel.OrderInfo.CouponCodeDiscount = viewModel.OrderInfo.OrderSubTotal - viewModel.OrderInfo.OrderTotal;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //Clean Session and shopping cart
            _db.ShoppingCarts.RemoveRange(viewModel.ShoppingCarts);
            HttpContext.Session.SetInt32(Utility.Constants.sessionShoppingCartCounts, 0);

            await _db.SaveChangesAsync();

            //Stripe Section
            //The stripe token is a string token sent from Stripe. A call made by the button on the view.
            //Go to the dashboard of Stripe, select Logs on the left section and check the response body from the log.
            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(viewModel.OrderInfo.OrderTotal * 100),
                Currency = "usd",
                Description = "Order ID : " + viewModel.OrderInfo.Id,
                Source = stripeToken

            };
            var service = new ChargeService();
            Charge charge = service.Create(options);

            if (charge.BalanceTransactionId == null)
            {
                viewModel.OrderInfo.PaymentStatus = Utility.Constants.PaymentStatusRejected;
            }
            else
            {
                viewModel.OrderInfo.TransactionId = charge.BalanceTransactionId;
            }

            if (charge.Status.ToLower() == "succeeded")
            {
                var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == viewModel.OrderInfo.ApplicationUserId);
                var listOrderDetails = _db.OrderDetails.Where(o => o.OrderInfoId == viewModel.OrderInfo.Id).ToList();
                StringBuilder body = new StringBuilder();
                body.Append($"Hi <strong>{user.Name}</strong>.<br/><p>Your order has been submitted successfully (Id <strong>{viewModel.OrderInfo.Id}</strong>).</p><p>The following order items were received:</p><ul>");
                foreach (var orderDetail in listOrderDetails)
                {
                    body.Append($"<li>{orderDetail.Name}<br/>Quantity: {orderDetail.Count}</li>");
                }
                body.Append("</ul>");
                body.Append("<p>You will be receiving a email when your order will start to prepare.</p>");

                //Send email to the user
                await _emailSender.SendEmailAsync
                    (
                        user.Email,
                        $"Spice Restaurant - Order Created with Id {viewModel.OrderInfo.Id.ToString()}",
                        body.ToString()
                    );

                viewModel.OrderInfo.PaymentStatus = Utility.Constants.PaymentStatusApproved;
                viewModel.OrderInfo.Status = Utility.Constants.OrderStatusSubmitted;
            }
            else
            {
                viewModel.OrderInfo.PaymentStatus = Utility.Constants.PaymentStatusRejected;
            }

            await _db.SaveChangesAsync();

            //return RedirectToAction("Index", "Home");
            return RedirectToAction("Confirm", "Orders", new { id = viewModel.OrderInfo.Id });
        }

        public IActionResult AddCoupon()
        {
            if (viewModel.OrderInfo.CouponCode == null)
            {
                viewModel.OrderInfo.CouponCode = string.Empty;
            }

            var coupon = _db.Coupons.FirstOrDefault(c => c.Name.ToLower() == viewModel.OrderInfo.CouponCode.Trim().ToLower());
            var couponName = coupon != null ? coupon.Name : viewModel.OrderInfo.CouponCode.Trim();
            HttpContext.Session.SetString(Utility.Constants.sessionCouponCode, couponName);
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