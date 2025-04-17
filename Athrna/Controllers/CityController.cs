using Microsoft.AspNetCore.Mvc;
using Athrna.Models;
using Athrna.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Athrna.Controllers
{
    public class CityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CityController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: City
        public async Task<IActionResult> Index()
        {
            var cities = await _context.City.ToListAsync();
            return View(cities);
        }

        // GET: City/Explore/madinah
        public async Task<IActionResult> Explore(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index", "Home");
            }

            // Normalize the city name
            id = id.ToLower();

            // Get city from database by normalized name
            var city = await _context.City
                .FirstOrDefaultAsync(c => c.Name.ToLower() == id);

            if (city == null)
            {
                return NotFound();
            }

            // Get city's historical sites
            var sites = await _context.Site
                .Where(s => s.CityId == city.Id)
                .Include(s => s.CulturalInfo)
                .ToListAsync();

            // Get guides for this city
            var guides = await _context.Guide
                .Where(g => g.CityId == city.Id)
                .ToListAsync();

            // Create a view model for the city
            var viewModel = new CityDetailsViewModel
            {
                City = city,
                Sites = sites,
                Guides = guides
            };

            return View(viewModel);
        }

        // GET: City/Site/5
        public async Task<IActionResult> Site(int id)
        {
            var site = await _context.Site
                .Include(s => s.City)
                .Include(s => s.CulturalInfo)
                .Include(s => s.Ratings)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (site == null)
            {
                return NotFound();
            }

            return View(site);
        }

        // POST: City/Bookmark/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bookmark(int id)
        {
            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            // Check if bookmark already exists
            var existingBookmark = await _context.Bookmark
                .FirstOrDefaultAsync(b => b.ClientId == userId && b.SiteId == id);

            if (existingBookmark == null)
            {
                // Create new bookmark
                var bookmark = new Bookmark
                {
                    ClientId = userId,
                    SiteId = id
                };

                _context.Bookmark.Add(bookmark);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Site bookmarked successfully!";
            }
            else
            {
                // Remove existing bookmark
                _context.Bookmark.Remove(existingBookmark);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Bookmark removed!";
            }

            // Redirect back to site details
            return RedirectToAction("Site", new { id });
        }

        // POST: City/Rate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int siteId, int value, string review)
        {
            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            // Check if rating already exists
            var existingRating = await _context.Rating
                .FirstOrDefaultAsync(r => r.ClientId == userId && r.SiteId == siteId);

            if (existingRating != null)
            {
                // Update existing rating
                existingRating.Value = value;
                existingRating.Review = review;
                _context.Rating.Update(existingRating);
            }
            else
            {
                // Create new rating
                var rating = new Rating
                {
                    ClientId = userId,
                    SiteId = siteId,
                    Value = value,
                    Review = review
                };

                _context.Rating.Add(rating);
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Rating submitted successfully!";

            // Redirect back to site details
            return RedirectToAction("Site", new { id = siteId });
        }
    }
}