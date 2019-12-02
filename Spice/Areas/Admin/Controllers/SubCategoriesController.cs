using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SubCategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;

        //This was in the course, but we use it only to add a message into the viewModel, so a local variable is best.
        //[TempData]
        //public string StatusMessage { get; set; }

        public SubCategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        //GET
        public async Task<IActionResult> Index()
        {
            var subCategories = await _db.SubCategories.Include(s => s.Category).ToListAsync();
            return View(subCategories);
        }

        //GET
        public async Task<IActionResult> Create()
        {
            SubCategoriesCreateViewModel viewModel = new SubCategoriesCreateViewModel()
            {
                Categories = await _db.Categories.ToListAsync(),
                SubCategory = new Models.SubCategory(),
                SubCategoryNameList = await _db.SubCategories.OrderBy(s => s.Name).Select(sb => sb.Name).Distinct().ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategoriesCreateViewModel viewModel)
        {
            string statusMessage = null;
            if (ModelState.IsValid)
            {
                var subCategoriesInCategory = _db.SubCategories.Include(s => s.Category).Where(sc => sc.Name == viewModel.SubCategory.Name && sc.CategoryId == viewModel.SubCategory.CategoryId);
                if (subCategoriesInCategory.Any())
                {
                    //Error
                    statusMessage = $"Error : Sub Category {viewModel.SubCategory.Name} exists under {subCategoriesInCategory.First().Category.Name} Category. Please verify.";
                }
                else
                {
                    _db.SubCategories.Add(viewModel.SubCategory);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            SubCategoriesCreateViewModel vm = new SubCategoriesCreateViewModel()
            {
                Categories = await _db.Categories.ToListAsync(),
                SubCategory = viewModel.SubCategory,
                SubCategoryNameList = await _db.SubCategories.OrderBy(s => s.Name).Select(sb => sb.Name).Distinct().ToListAsync(),
                StatusMessage = statusMessage
            };

            return View(vm);
        }
        
        // JSON
        public async Task<IActionResult> GetSubCategory(int id)
        {
            List<SubCategory> subCategories = await _db.SubCategories.Where(s => s.CategoryId == id).ToListAsync();
            return Json(new SelectList(subCategories, "Id", "Name"));
        }

        //GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id.HasValue)
            {
                var subCategory = await _db.SubCategories.SingleOrDefaultAsync(s => s.Id == id);

                if (subCategory != null)
                {
                    SubCategoriesCreateViewModel viewModel = new SubCategoriesCreateViewModel()
                    {
                        Categories = await _db.Categories.ToListAsync(),
                        SubCategory = subCategory,
                        SubCategoryNameList = await _db.SubCategories.OrderBy(s => s.Name).Select(sb => sb.Name).Distinct().ToListAsync()
                    };

                    return View(viewModel);
                }
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubCategoriesCreateViewModel viewModel)
        {
            string statusMessage = null;
            if (ModelState.IsValid)
            {
                var subCategoriesInCategory = _db.SubCategories.Include(s => s.Category).Where(sc => sc.Name == viewModel.SubCategory.Name && sc.CategoryId == viewModel.SubCategory.CategoryId);
                if (subCategoriesInCategory.Any())
                {
                    //Error
                    statusMessage = $"Error : Sub Category {viewModel.SubCategory.Name} exists under {subCategoriesInCategory.First().Category.Name} Category. Please verify.";
                }
                else
                {
                    var subCategory = await _db.SubCategories.FindAsync(viewModel.SubCategory.Id);
                    subCategory.Name = viewModel.SubCategory.Name;

                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            SubCategoriesCreateViewModel vm = new SubCategoriesCreateViewModel()
            {
                Categories = await _db.Categories.ToListAsync(),
                SubCategory = viewModel.SubCategory,
                SubCategoryNameList = await _db.SubCategories.OrderBy(s => s.Name).Select(sb => sb.Name).Distinct().ToListAsync(),
                StatusMessage = statusMessage
            };

            return View(vm);
        }

        //GET
        public async Task<IActionResult> Details(int? id)
        {
            if (id.HasValue)
            {
                var subCategory = await _db.SubCategories.Include(s=> s.Category).SingleOrDefaultAsync(s => s.Id == id);

                if (subCategory != null)
                {
                    return View(subCategory);
                }
            }

            return NotFound();
        }

        //GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id.HasValue)
            {
                var subCategory = await _db.SubCategories.Include(s => s.Category).SingleOrDefaultAsync(s => s.Id == id);

                if (subCategory != null)
                {
                    return View(subCategory);
                }
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (ModelState.IsValid)
            {
                var subCategory = await _db.SubCategories.FindAsync(id);
                if (subCategory != null)
                {
                    _db.SubCategories.Remove(subCategory);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            return NotFound();
        }
    }
}