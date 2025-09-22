using System.Diagnostics;
using FleaMarket.FrontEnd.Models;
using FleaMarket.FrontEnd.Data;
using FleaMarket.FrontEnd.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FleaMarket.FrontEnd.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return View("Welcome");
            }
            var query = _context.Items
                .Include(i => i.Owner)
                .Include(i => i.Images)
                .Where(i => !i.IsArchived && !i.IsSold);

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
                Search = search
            };

            return View(model);
        }

        public async Task<IActionResult> Browse(string? search, string? category)
        {
            var query = _context.Items
                .Include(i => i.Owner)
                .Include(i => i.Images)
                .Where(i => !i.IsArchived && !i.IsSold);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i => i.Name.Contains(search) || (i.Description != null && i.Description.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(i => i.Category == category);
            }

            var items = await query
                .OrderBy(i => i.Price == null ? 0 : 1)
                .ThenBy(i => i.Name)
                .ToListAsync();

            // Get category counts for all available items (not filtered by current category selection)
            var allAvailableItems = await _context.Items
                .Where(i => !i.IsArchived && !i.IsSold && !string.IsNullOrEmpty(i.Category))
                .ToListAsync();

            var categories = allAvailableItems
                .GroupBy(i => i.Category!)
                .ToDictionary(g => g.Key, g => g.Count());

            var model = new ItemsIndexViewModel
            {
                Items = items,
                Search = search,
                Category = category,
                Categories = categories
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int id)
        {
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
            if (userId == null)
            {
                return Challenge();
            }

            var item = await _context.Items
                .Include(i => i.Owner)
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsArchived && !i.IsSold);
            if (item == null)
            {
                return NotFound();
            }

            item.IsReserved = true;

            var reservation = new Reservation
            {
                ItemId = item.Id,
                BuyerId = userId
            };
            _context.Reservations.Add(reservation);

            await _context.SaveChangesAsync();

            if (item.Owner?.Email != null)
            {
                var buyer = await _context.Users.FindAsync(userId);
                var body = $"Your item '{item.Name}' has been reserved by {buyer?.Email}.";
                await _emailService.SendEmailAsync(item.Owner.Email, "Item reserved", body);
            }

            TempData["Message"] = "Reservation recorded.";
            return RedirectToAction("Index", "Reservations");
        }
    }
}
