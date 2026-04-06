using Microsoft.AspNetCore.Mvc;
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
        private readonly IWebHostEnvironment _env;

        public TripsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Trips.Include(t => t.Images).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.Images)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trip == null) return NotFound();

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.AlreadyBooked = _context.Bookings
                    .Any(b => b.TripId == id && b.UserId == userId);
            }

            return View(trip);
        }

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

            bool alreadyBooked = _context.Bookings
                .Any(b => b.TripId == id && b.UserId == userId);

            if (alreadyBooked)
            {
                TempData["Error"] = "Вече си резервирал тази екскурзия.";
                return RedirectToAction("Details", new { id });
            }

            trip.Seats--;

            _context.Bookings.Add(new Booking
            {
                UserId = userId,
                TripId = id,
                BookingDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Успешно резервира място!";
            return RedirectToAction("MyTrips");
        }

        [Authorize]
        public async Task<IActionResult> MyTrips()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var bookings = await _context.Bookings
                .Include(b => b.Trip)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = await _context.Bookings
                .Include(b => b.Trip)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Резервацията не беше намерена.";
                return RedirectToAction("MyTrips");
            }

            // Връщаме мястото
            booking.Trip.Seats++;

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Резервацията беше успешно отменена.";
            return RedirectToAction("MyTrips");
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,Title,Destination,Description,Price,StartDate,EndDate,Seats")] Trip trip,
            List<IFormFile>? imageFiles)
        {
            if (string.IsNullOrWhiteSpace(trip.Title) || string.IsNullOrWhiteSpace(trip.Destination))
            {
                ViewBag.Error = "Попълни поне Заглавие и Дестинация.";
                return View(trip);
            }

            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            if (imageFiles != null && imageFiles.Count > 0)
            {
                var webRoot = _env.WebRootPath
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var uploadsFolder = Path.Combine(webRoot, "uploads", "trips");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var file in imageFiles)
                {
                    if (file.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        _context.TripImages.Add(new TripImage
                        {
                            TripId = trip.Id,
                            ImagePath = "/uploads/trips/" + fileName
                        });
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.Images)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null) return NotFound();

            return View(trip);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Title,Destination,Description,Price,StartDate,EndDate,Seats")] Trip trip,
            List<IFormFile>? imageFiles)
        {
            if (id != trip.Id) return NotFound();

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

            if (imageFiles != null && imageFiles.Count > 0)
            {
                var webRoot = _env.WebRootPath
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var uploadsFolder = Path.Combine(webRoot, "uploads", "trips");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var file in imageFiles)
                {
                    if (file.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        _context.TripImages.Add(new TripImage
                        {
                            TripId = trip.Id,
                            ImagePath = "/uploads/trips/" + fileName
                        });

                        await _context.SaveChangesAsync();
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteImage(int imageId, int tripId)
        {
            var image = await _context.TripImages.FindAsync(imageId);
            if (image != null)
            {
                var webRoot = _env.WebRootPath
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var filePath = Path.Combine(webRoot, image.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                _context.TripImages.Remove(image);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Edit", new { id = tripId });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips.FirstOrDefaultAsync(m => m.Id == id);
            if (trip == null) return NotFound();

            return View(trip);
        }

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