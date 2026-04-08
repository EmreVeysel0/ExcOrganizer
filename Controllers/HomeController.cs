using ExcOrganizer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExcOrganizer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public IActionResult About()
        {
            return View();
        }

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var trips = await _context.Trips
                .OrderBy(t => t.StartDate)
                .Take(6)
                .ToListAsync();

            return View(trips);
        }
    }
}