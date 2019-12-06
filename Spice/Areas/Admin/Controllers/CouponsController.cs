using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Utility.Constants.ManagerUser)]
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CouponsController(ApplicationDbContext db)
        {
            _db = db;
        }


        public async Task<IActionResult> Index()
        {
            return View(await _db.Coupons.ToListAsync());
        }

        //GET
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                if (files.Any())
                {
                    byte[] picture = null;
                    using (var fileStream = files[0].OpenReadStream())
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            fileStream.CopyTo(memoryStream);
                            picture = memoryStream.ToArray();
                        }
                    }
                    coupon.Picture = picture;
                }
                _db.Coupons.Add(coupon);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(coupon);
        }

        //GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id.HasValue)
            {
                Coupon coupon = await _db.Coupons.FindAsync(id);
                if (coupon != null)
                {
                    return View(coupon);
                }
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Coupon coupon)
        {
            if (!ModelState.IsValid)
            {
                return View(coupon);
            }

            if (coupon.Id == 0)
            {
                return NotFound();
            }

            var couponFromDB = await _db.Coupons.FindAsync(coupon.Id);

            //Save the image in the DB

            var files = HttpContext.Request.Form.Files;

            //A new image was uploaded? if no file was uploaded, keep the existing image.
            if (files.Any())
            {
                //Upload new file
                byte[] picture = null;
                using (var fileStream = files[0].OpenReadStream())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        fileStream.CopyTo(memoryStream);
                        picture = memoryStream.ToArray();
                    }
                }
                couponFromDB.Picture = picture;
            }

            //Other properties
            couponFromDB.Name = coupon.Name;
            couponFromDB.CouponType = coupon.CouponType;
            couponFromDB.Discount = coupon.Discount;
            couponFromDB.MinimumAmount = coupon.MinimumAmount;
            couponFromDB.IsActive = coupon.IsActive;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET
        public async Task<IActionResult> Details(int? id)
        {
            if (id.HasValue)
            {
                Coupon coupon = await _db.Coupons.FindAsync(id);
                if (coupon != null)
                {
                    return View(coupon);
                }
            }

            return NotFound();
        }

        //GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id.HasValue)
            {
                Coupon coupon = await _db.Coupons.FindAsync(id);
                if (coupon != null)
                {
                    return View(coupon);
                }
            }

            return NotFound();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            if (id.HasValue)
            {
                var coupon = await _db.Coupons.FindAsync(id);

                if (coupon != null)
                {
                    _db.Coupons.Remove(coupon);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            return NotFound();
        }
    }
}