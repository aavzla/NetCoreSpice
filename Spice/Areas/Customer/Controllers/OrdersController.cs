using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private int PageSize = 2;

        public OrdersController(ApplicationDbContext db)
        {
            _db = db;
        }

        [Authorize]
        public async Task<IActionResult> Confirm(int id)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderDetailsViewModel viewModel = new OrderDetailsViewModel()
            {
                /* We look the Order by Id but also by ApplicationUserId. Why?
                 * Because an authorize user could type another Id in the URL to look for another order placed by someone else.
                 * We don't want to allow that.
                */
                OrderInfo = await _db.OrderInfos.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == claim.Value),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderInfoId == id).ToListAsync()
            };

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int productPage = 1)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel viewModel = new OrderListViewModel()
            {
                OrderDetailsViewModels = new List<OrderDetailsViewModel>()
            };

            List<OrderInfo> listOrderInfos = await _db.OrderInfos.Include(o => o.ApplicationUser)
                                                                    .Where(o => o.ApplicationUserId == claim.Value)
                                                                    .ToListAsync();

            OrderDetailsViewModel individualOrderDetailsVM = null;
            foreach (OrderInfo orderInfo in listOrderInfos)
            {
                individualOrderDetailsVM = new OrderDetailsViewModel()
                {
                    OrderInfo = orderInfo,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderInfoId == orderInfo.Id).ToListAsync()
                };
                viewModel.OrderDetailsViewModels.Add(individualOrderDetailsVM);
            }

            var count = viewModel.OrderDetailsViewModels.Count;
            viewModel.OrderDetailsViewModels = viewModel.OrderDetailsViewModels.OrderByDescending(o => o.OrderInfo.Id)
                                                        .Skip((productPage - 1) * PageSize)
                                                        .Take(PageSize).ToList();

            viewModel.PagingInfo = new PagingInfo()
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItem = count,
                urlParam = "/Customer/Orders/OrderHistory?productPage=:"
            };

            return View(viewModel);
        }

        public async Task<IActionResult> GetOrderDetails(int id)
        {
            OrderDetailsViewModel viewModel = new OrderDetailsViewModel()
            {
                OrderInfo = await _db.OrderInfos.FirstOrDefaultAsync(o => o.Id == id),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderInfoId == id).ToListAsync()
            };
            viewModel.OrderInfo.ApplicationUser = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == viewModel.OrderInfo.ApplicationUserId);

            return PartialView("_IndividualOrderDetailsPartial", viewModel);
        }

        public async Task<IActionResult> GetOrderStatus(int id)
        {
            var orderInfo = await _db.OrderInfos.FirstOrDefaultAsync(o => o.Id == id);
            return PartialView("_OrderStatusPartial", orderInfo != null ? orderInfo.Status : string.Empty);
        }

        [Authorize(Roles = Utility.Constants.KitchenUser + "," + Utility.Constants.ManagerUser)]
        public async Task<IActionResult> ManageOrder()
        {
            List<OrderDetailsViewModel> orderDetailsViewModels = new List<OrderDetailsViewModel>();

            List<OrderInfo> listOrderInfos = await _db.OrderInfos.Where(o =>
                                                                    o.Status == Utility.Constants.OrderStatusSubmitted
                                                                    || o.Status == Utility.Constants.OrderStatusInProcess)
                                                                 .OrderByDescending(o => o.PickUpTime).ToListAsync();

            OrderDetailsViewModel individualOrderDetailsVM = null;
            foreach (OrderInfo orderInfo in listOrderInfos)
            {
                individualOrderDetailsVM = new OrderDetailsViewModel()
                {
                    OrderInfo = orderInfo,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderInfoId == orderInfo.Id).ToListAsync()
                };
                orderDetailsViewModels.Add(individualOrderDetailsVM);
            }

            return View(orderDetailsViewModels.OrderBy(o => o.OrderInfo.PickUpTime).ToList());
        }

        [Authorize(Roles = Utility.Constants.KitchenUser + "," + Utility.Constants.ManagerUser)]
        public async Task<IActionResult> OrderPrepare(int id)
        {
            OrderInfo orderInfo = await _db.OrderInfos.FindAsync(id);
            if (orderInfo != null)
            {
                orderInfo.Status = Utility.Constants.OrderStatusInProcess;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageOrder));
        }

        [Authorize(Roles = Utility.Constants.KitchenUser + "," + Utility.Constants.ManagerUser)]
        public async Task<IActionResult> OrderReady(int id)
        {
            OrderInfo orderInfo = await _db.OrderInfos.FindAsync(id);
            if (orderInfo != null)
            {
                orderInfo.Status = Utility.Constants.OrderStatusReady;
                await _db.SaveChangesAsync();
            }

            //Send email for Pick-up

            return RedirectToAction(nameof(ManageOrder));
        }

        [Authorize(Roles = Utility.Constants.ManagerUser)]
        public async Task<IActionResult> OrderCancel(int id)
        {
            OrderInfo orderInfo = await _db.OrderInfos.FindAsync(id);
            if (orderInfo != null)
            {
                orderInfo.Status = Utility.Constants.OrderStatusCancelled;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageOrder));
        }

        [Authorize(Roles = Utility.Constants.FrontDeskUser + "," + Utility.Constants.ManagerUser)]
        [HttpGet]
        public async Task<IActionResult> OrderPickUp(int productPage = 1, string searchName = null, string searchPhone = null, string searchEmail = null)
        {
            OrderListViewModel viewModel = new OrderListViewModel()
            {
                OrderDetailsViewModels = new List<OrderDetailsViewModel>()
            };

            StringBuilder param = new StringBuilder();
            param.Append("/Customer/Orders/OrderPickUp?productPage=:");
            
            List<OrderInfo> listOrderInfos = await _db.OrderInfos.Include(o => o.ApplicationUser)
                                                                    .Where(o => o.Status == Utility.Constants.OrderStatusReady)
                                                                    .ToListAsync();

            // If we search by a filter, we filter the ready orders for pick-up by them.
            // If we filter by name or phone, we filter them by the name or phone provided for pick up or the name or phone of the user.
            // (In case the user decide to pick it up after provide other user or phone for pick up.)
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                param.Append($"&{nameof(searchName)}={searchName}");
                listOrderInfos = listOrderInfos.Where(o => !string.IsNullOrWhiteSpace(o.PickUpName) ?
                                                            o.PickUpName.Contains(searchName, StringComparison.InvariantCultureIgnoreCase)
                                                            : o.ApplicationUser.Name.Contains(searchName, StringComparison.InvariantCultureIgnoreCase)
                                                    ).ToList();
            }
            if (!string.IsNullOrWhiteSpace(searchPhone))
            {
                param.Append($"&{nameof(searchPhone)}={searchPhone}");
                listOrderInfos = listOrderInfos.Where(o => !string.IsNullOrWhiteSpace(o.PhoneNumber) ?
                                                            o.PhoneNumber.Contains(searchPhone, StringComparison.InvariantCultureIgnoreCase)
                                                            : o.ApplicationUser.PhoneNumber.Contains(searchPhone, StringComparison.InvariantCultureIgnoreCase)
                                                    ).ToList();
            }
            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                param.Append($"&{nameof(searchEmail)}={searchEmail}");
                listOrderInfos = listOrderInfos.Where(o => o.ApplicationUser.Email.Contains(searchEmail, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }
            //Order by the next pick up time, even if it has passed already and not been pick up.
            listOrderInfos = listOrderInfos.OrderBy(o => o.PickUpTime).ToList();

            OrderDetailsViewModel individualOrderDetailsVM = null;
            foreach (OrderInfo orderInfo in listOrderInfos)
            {
                individualOrderDetailsVM = new OrderDetailsViewModel()
                {
                    OrderInfo = orderInfo,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderInfoId == orderInfo.Id).ToListAsync()
                };
                viewModel.OrderDetailsViewModels.Add(individualOrderDetailsVM);
            }

            var count = viewModel.OrderDetailsViewModels.Count;
            viewModel.OrderDetailsViewModels = viewModel.OrderDetailsViewModels.OrderByDescending(o => o.OrderInfo.Id)
                                                        .Skip((productPage - 1) * PageSize)
                                                        .Take(PageSize).ToList();

            viewModel.PagingInfo = new PagingInfo()
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItem = count,
                urlParam = param.ToString()
            };

            return View(viewModel);
        }

        [Authorize(Roles = Utility.Constants.FrontDeskUser + "," + Utility.Constants.ManagerUser)]
        [HttpPost]
        public async Task<IActionResult> OrderPickUp(int id)
        {
            OrderInfo orderInfo = await _db.OrderInfos.FindAsync(id);
            if (orderInfo != null)
            {
                orderInfo.Status = Utility.Constants.OrderStatusCompleted;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(OrderPickUp));
        }
    }
}