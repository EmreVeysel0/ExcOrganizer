using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExcOrganizer.Data;
using ExcOrganizer.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ExcOrganizer.Controllers
{
    public class TripsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TripsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Trips
        public async Task<IActionResult> Index()
        {
            return View(await _context.Trips.ToListAsync());
        }

        // GET: Trips/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips.FirstOrDefaultAsync(m => m.Id == id);
            if (trip == null) return NotFound();

            // Проверяваме дали потребителят вече е резервирал
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.AlreadyBooked = _context.Bookings
                    .Any(b => b.TripId == id && b.UserId == userId);
            }

            return View(trip);
        }

        // POST: Trips/Book/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            if (trip.Seats <= 0)
            {
                TempData["Error"] = "Няма свободни места за тази екскурзия.";
                return RedirectToAction("Details", new { id });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Проверка дали вече е резервирал
            bool alreadyBooked = _context.Bookings
                .Any(b => b.TripId == id && b.UserId == userId);

            if (alreadyBooked)
            {
                TempData["Error"] = "Вече си резервирал тази екскурзия.";
                return RedirectToAction("Details", new { id });
            }

            // Намаляме местата и записваме резервацията
            trip.Seats--;

            _context.Bookings.Add(new Booking
            {
                UserId = userId,
                TripId = id,
                BookingDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Успешно резервира място за \"" + trip.Title + "\"!";
            return RedirectToAction("Details", new { id });
        }

        // GET: Trips/Create
        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Trips/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Destination,Description,Price,StartDate,EndDate,Seats")] Trip trip)
        {
            if (string.IsNullOrWhiteSpace(trip.Title) || string.IsNullOrWhiteSpace(trip.Destination))
            {
                ViewBag.Error = "Попълни поне Заглавие и Дестинация.";
                return View(trip);
            }

            _context.Trips.Add(trip);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Trips/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            return View(trip);
        }

        // POST: Trips/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Destination,Description,Price,StartDate,EndDate,Seats")] Trip trip)
        {
            if (id != trip.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(trip);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TripExists(trip.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(trip);
        }

        // GET: Trips/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips.FirstOrDefaultAsync(m => m.Id == id);
            if (trip == null) return NotFound();

            return View(trip);
        }

        // POST: Trips/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip != null)
            {
                _context.Trips.Remove(trip);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TripExists(int id)
        {
            return _context.Trips.Any(e => e.Id == id);
        }
    }
}