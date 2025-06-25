using System.Diagnostics;
using FleaMarket.FrontEnd.Models;
using FleaMarket.FrontEnd.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FleaMarket.FrontEnd.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
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

            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id && !i.IsArchived && !i.IsSold);
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

            TempData["Message"] = "Reservation recorded.";
            return RedirectToAction(nameof(Index));
        }
    }
}
