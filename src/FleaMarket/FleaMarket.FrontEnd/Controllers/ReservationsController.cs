using FleaMarket.FrontEnd.Data;
using FleaMarket.FrontEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleaMarket.FrontEnd.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReservationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var reservations = await _context.Reservations
                .Include(r => r.Item)
                    .ThenInclude(i => i.Images)
                .Where(r => r.BuyerId == userId)
                .OrderByDescending(r => r.Created)
                .ToListAsync();
            return View(reservations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var reservation = await _context.Reservations
                .Include(r => r.Item)
                .FirstOrDefaultAsync(r => r.Id == id && r.BuyerId == userId);
            if (reservation != null)
            {
                reservation.Item.IsReserved = false;
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
