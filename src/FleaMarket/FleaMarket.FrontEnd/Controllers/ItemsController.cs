using FleaMarket.FrontEnd.Data;
using FleaMarket.FrontEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleaMarket.FrontEnd.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ItemsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var item = new Item
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.IsFree ? null : model.Price,
                OwnerId = _userManager.GetUserId(User)
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            if (model.Images != null && model.Images.Count > 0)
            {
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadDir);

                foreach (var image in model.Images)
                {
                    if (image.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                        var filePath = Path.Combine(uploadDir, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await image.CopyToAsync(stream);

                        item.Images.Add(new ItemImage
                        {
                            FileName = fileName
                        });
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, bool showArchived = false)
        {
            var userId = _userManager.GetUserId(User);
            var query = _context.Items
                .Include(i => i.Images)
                .Where(i => i.OwnerId == userId);

            if (!showArchived)
            {
                query = query.Where(i => !i.IsArchived);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i => i.Name.Contains(search) || (i.Description != null && i.Description.Contains(search)));
            }

            var items = await query
                .OrderBy(i => i.Price == null ? 0 : 1)
                .ThenBy(i => i.Name)
                .ToListAsync();

            var model = new ItemsIndexViewModel
            {
                Items = items,
                Search = search,
                ShowArchived = showArchived
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.Items
                .Include(i => i.Images)
                .FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId);
            if (item == null)
            {
                return NotFound();
            }

            var model = new ItemEditViewModel
            {
                Name = item.Name,
                Description = item.Description,
                IsFree = item.Price == null,
                Price = item.Price,
                ExistingImages = item.Images.ToList()
            };

            ViewData["ItemId"] = id;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ItemEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ItemId"] = id;
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            var item = await _context.Items
                .Include(i => i.Images)
                .FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId);
            if (item == null)
            {
                return NotFound();
            }

            item.Name = model.Name;
            item.Description = model.Description;
            item.Price = model.IsFree ? null : model.Price;

            if (model.RemoveImageIds?.Count > 0)
            {
                var toRemove = item.Images.Where(img => model.RemoveImageIds.Contains(img.Id)).ToList();
                _context.ItemImages.RemoveRange(toRemove);
            }

            if (model.Images != null && model.Images.Count > 0)
            {
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadDir);

                foreach (var image in model.Images)
                {
                    if (image.Length > 0)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                        var filePath = Path.Combine(uploadDir, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await image.CopyToAsync(stream);
                        item.Images.Add(new ItemImage { FileName = fileName });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId);
            if (item != null)
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId);
            if (item != null)
            {
                item.IsArchived = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkSold(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId);
            if (item != null)
            {
                item.IsSold = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAvailable(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId);
            if (item != null)
            {
                item.IsSold = false;
                item.IsReserved = false;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
