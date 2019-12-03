using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MenuItemsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        [BindProperty]
        public MenuItemViewModel MenuItemVM { get; set; }

        public MenuItemsController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
            MenuItemVM = new MenuItemViewModel()
            {
                Categories = _db.Categories,
                MenuItem = new Models.MenuItem()
            };
        }


        public async Task<IActionResult> Index()
        {
            var menuItems = await _db.MenuItems.Include(c => c.Category).Include(c => c.SubCategory).ToListAsync();
            return View(menuItems);
        }

        //GET
        public IActionResult Create()
        {
            return View(MenuItemVM);
        }

        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePOST()
        {
            //Added in the course but is false, The category and subcategory Id's are already fetched into the VM.
            //MenuItemVM.MenuItem.SubCategoryId = int.Parse(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid)
            {
                return View(MenuItemVM);
            }

            _db.MenuItems.Add(MenuItemVM.MenuItem);
            await _db.SaveChangesAsync();

            //Save image
            /*The name of the file should be unique. Two files can not share the same name, even if it is the same image.
             * Rename the image based on the Id of the Object, in this case, menu item. Keeping the extension.
             * The Path saved into the DB will be the path of the images in the project. So we can see all images at the same folder.
            */

            var menuItem = await _db.MenuItems.FindAsync(MenuItemVM.MenuItem.Id);

            string webRootPath = _webHostEnvironment.WebRootPath;
            var uploads = Path.Combine(webRootPath, "images");
            var files = HttpContext.Request.Form.Files;
            string extension = null;

            if (files.Any())
            {
                //At least a file has been uploaded.
                extension = Path.GetExtension(files[0].FileName);
                using (var fileStream = new FileStream(Path.Combine(uploads, menuItem.Id + extension), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }
            }
            else
            {
                //No file was uploaded, use default image file.
                var uploadFile = Path.Combine(uploads, Constants.DefaultFoodImage);
                extension = ".png";
                System.IO.File.Copy(uploadFile, Path.Combine(uploads, menuItem.Id + extension));
            }
            menuItem.ImagePath = @"\images\" + menuItem.Id + extension;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id.HasValue)
            {
                MenuItemVM.MenuItem = await _db.MenuItems.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == id);
                MenuItemVM.SubCategories = await _db.SubCategories.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

                if (MenuItemVM.MenuItem != null)
                {
                    return View(MenuItemVM);
                }
            }

            return NotFound();
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPOST(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            //Added in the course but is false, The category and subcategory Id's are already fetched into the VM.
            //MenuItemVM.MenuItem.SubCategoryId = int.Parse(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid)
            {
                MenuItemVM.Categories = await _db.Categories.ToListAsync();
                MenuItemVM.SubCategories = await _db.SubCategories.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();
                return View(MenuItemVM);
            }

            //Save image
            /*The name of the file should be unique. Two files can not share the same name, even if it is the same image.
             * Rename the image based on the Id of the Object, in this case, menu item. Keeping the extension.
             * The Path saved into the DB will be the path of the images in the project. So we can see all images at the same folder.
            */

            var menuItem = await _db.MenuItems.FindAsync(MenuItemVM.MenuItem.Id);

            string webRootPath = _webHostEnvironment.WebRootPath;
            var uploads = Path.Combine(webRootPath, "images");
            var files = HttpContext.Request.Form.Files;
            string extension = ".png";

            //A new image was uploaded? if no file was uploaded, keep the existing image.
            if (files.Any())
            {
                //At least a new file has been uploaded.
                //Delete original file
                var imagePath = Path.Combine(webRootPath, menuItem.ImagePath.TrimStart('\\'));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                //Upload new file
                extension = Path.GetExtension(files[0].FileName);
                using (var fileStream = new FileStream(Path.Combine(uploads, menuItem.Id + extension), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }
                menuItem.ImagePath = @"\images\" + menuItem.Id + extension;
            }

            //Other properties
            menuItem.Name = MenuItemVM.MenuItem.Name;
            menuItem.Description = MenuItemVM.MenuItem.Description;
            menuItem.Price = MenuItemVM.MenuItem.Price;
            menuItem.Spicyness = MenuItemVM.MenuItem.Spicyness;
            menuItem.CategoryId = MenuItemVM.MenuItem.CategoryId;
            menuItem.SubCategoryId = MenuItemVM.MenuItem.SubCategoryId;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET
        public async Task<IActionResult> Details(int? id)
        {
            if (id.HasValue)
            {
                MenuItemVM.MenuItem = await _db.MenuItems.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == id);
                MenuItemVM.SubCategories = await _db.SubCategories.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

                if (MenuItemVM.MenuItem != null)
                {
                    return View(MenuItemVM);
                }
            }

            return NotFound();
        }

        //GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id.HasValue)
            {
                MenuItemVM.MenuItem = await _db.MenuItems.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == id);
                MenuItemVM.SubCategories = await _db.SubCategories.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

                if (MenuItemVM.MenuItem != null)
                {
                    return View(MenuItemVM);
                }
            }

            return NotFound();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            //Added in the course but is false, The category and subcategory Id's are already fetched into the VM.
            //MenuItemVM.MenuItem.SubCategoryId = int.Parse(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid)
            {
                MenuItemVM.Categories = await _db.Categories.ToListAsync();
                MenuItemVM.SubCategories = await _db.SubCategories.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();
                return View(MenuItemVM);
            }

            var menuItem = await _db.MenuItems.FindAsync(MenuItemVM.MenuItem.Id);

            //If the menu item has a image path, we should delete the image.
            if (menuItem != null && !string.IsNullOrWhiteSpace(menuItem.ImagePath))
            {
                string webRootPath = _webHostEnvironment.WebRootPath;

                //Delete original file
                var imagePath = Path.Combine(webRootPath, menuItem.ImagePath.TrimStart('\\'));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _db.MenuItems.Remove(menuItem);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}