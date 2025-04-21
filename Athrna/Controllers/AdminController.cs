using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;

namespace Athrna.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin
        public async Task<IActionResult> Index()
        {
            var statistics = new AdminDashboardViewModel
            {
                TotalSites = await _context.Site.CountAsync(),
                TotalUsers = await _context.Client.CountAsync(),
                TotalBookmarks = await _context.Bookmark.CountAsync(),
                TotalRatings = await _context.Rating.CountAsync(),
                RecentUsers = await _context.Client.OrderByDescending(c => c.Id).Take(5).ToListAsync(),
                RecentRatings = await _context.Rating
                    .Include(r => r.Client)
                    .Include(r => r.Site)
                    .OrderByDescending(r => r.Id)
                    .Take(5)
                    .ToListAsync()
            };

            return View(statistics);
        }

        // GET: Admin/Sites
        public async Task<IActionResult> Sites()
        {
            var sites = await _context.Site
                .Include(s => s.City)
                .Include(s => s.CulturalInfo)
                .ToListAsync();

            return View(sites);
        }

        // GET: Admin/Cities
        public async Task<IActionResult> Cities()
        {
            var cities = await _context.City
                .Include(c => c.Sites)
                .ToListAsync();

            return View(cities);
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _context.Client
                .Include(c => c.Administrator)
                .ToListAsync();

            return View(users);
        }

        // GET: Admin/EditSite/5
        public async Task<IActionResult> EditSite(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var site = await _context.Site
                .Include(s => s.City)
                .Include(s => s.CulturalInfo)
                .Include(s => s.Bookmarks)
                .Include(s => s.Ratings)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (site == null)
            {
                return NotFound();
            }

            ViewBag.Cities = await _context.City.ToListAsync();
            return View(site);
        }

        // POST: Admin/EditSite/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSite(int id, Site site, IFormFile imageFile)
        {
            if (id != site.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing site to preserve data not included in the form
                    var existingSite = await _context.Site
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (existingSite == null)
                    {
                        return NotFound();
                    }

                    // Handle image upload if a new file is provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Define allowed file extensions
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("imageFile", "Only image files (jpg, jpeg, png, gif) are allowed.");
                            ViewBag.Cities = await _context.City.ToListAsync();
                            return View(site);
                        }

                        // Check file size (limit to 5MB)
                        if (imageFile.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("imageFile", "The file size cannot exceed 5MB.");
                            ViewBag.Cities = await _context.City.ToListAsync();
                            return View(site);
                        }

                        // Generate a unique filename
                        string uniqueFileName = $"{id}_{Guid.NewGuid().ToString("N")}{extension}";
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "sites");

                        // Create directory if it doesn't exist
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Delete old image if it exists in our uploads folder
                        if (!string.IsNullOrEmpty(existingSite.ImagePath) &&
                            existingSite.ImagePath.StartsWith("/uploads/sites/") &&
                            System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingSite.ImagePath.TrimStart('/'))))
                        {
                            System.IO.File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingSite.ImagePath.TrimStart('/')));
                        }

                        // Save the new file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        // Update the image path in the site model
                        site.ImagePath = "/uploads/sites/" + uniqueFileName;
                    }
                    else
                    {
                        // Keep the existing image if no new one is uploaded
                        site.ImagePath = existingSite.ImagePath;
                    }

                    // Update the site entity
                    _context.Update(site);
                    await _context.SaveChangesAsync();

                    // Update cultural info if it exists
                    if (site.CulturalInfo != null)
                    {
                        var culturalInfo = await _context.CulturalInfo
                            .FirstOrDefaultAsync(c => c.SiteId == site.Id);

                        if (culturalInfo != null)
                        {
                            culturalInfo.Summary = site.CulturalInfo.Summary;
                            culturalInfo.EstablishedDate = site.CulturalInfo.EstablishedDate;
                            _context.Update(culturalInfo);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            // Create cultural info if it doesn't exist
                            var newCulturalInfo = new CulturalInfo
                            {
                                SiteId = site.Id,
                                Summary = site.CulturalInfo.Summary,
                                EstablishedDate = site.CulturalInfo.EstablishedDate
                            };
                            _context.CulturalInfo.Add(newCulturalInfo);
                            await _context.SaveChangesAsync();
                        }
                    }

                    TempData["SuccessMessage"] = "Site updated successfully!";
                    return RedirectToAction(nameof(Sites));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SiteExists(site.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Cities = await _context.City.ToListAsync();
            return View(site);
        }

        // GET: Admin/CreateSite
        public async Task<IActionResult> CreateSite()
        {
            ViewBag.Cities = await _context.City.ToListAsync();
            return View();
        }

        // POST: Admin/CreateSite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSite(Site site, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Define allowed file extensions
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("imageFile", "Only image files (jpg, jpeg, png, gif) are allowed.");
                        ViewBag.Cities = await _context.City.ToListAsync();
                        return View(site);
                    }

                    // Check file size (limit to 5MB)
                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("imageFile", "The file size cannot exceed 5MB.");
                        ViewBag.Cities = await _context.City.ToListAsync();
                        return View(site);
                    }

                    // Add site to database first to get ID
                    _context.Site.Add(site);
                    await _context.SaveChangesAsync();

                    // Generate a unique filename using the site ID
                    string uniqueFileName = $"{site.Id}_{Guid.NewGuid().ToString("N")}{extension}";
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "sites");

                    // Create directory if it doesn't exist
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save the file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    // Update the image path in the site model
                    site.ImagePath = "/uploads/sites/" + uniqueFileName;
                    _context.Update(site);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // If no image is uploaded, just add the site
                    _context.Site.Add(site);
                    await _context.SaveChangesAsync();

                    // Set a default placeholder image
                    site.ImagePath = "/api/placeholder/400/300";
                    _context.Update(site);
                    await _context.SaveChangesAsync();
                }

                // Create cultural info for the site
                if (site.CulturalInfo != null)
                {
                    var culturalInfo = new CulturalInfo
                    {
                        SiteId = site.Id,
                        Summary = site.CulturalInfo.Summary,
                        EstablishedDate = site.CulturalInfo.EstablishedDate
                    };

                    _context.CulturalInfo.Add(culturalInfo);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "New site created successfully!";
                return RedirectToAction(nameof(Sites));
            }

            ViewBag.Cities = await _context.City.ToListAsync();
            return View(site);
        }

        // GET: Admin/DeleteSite/5
        public async Task<IActionResult> DeleteSite(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var site = await _context.Site
                .Include(s => s.City)
                .Include(s => s.Bookmarks)
                .Include(s => s.Ratings)
                .Include(s => s.CulturalInfo)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (site == null)
            {
                return NotFound();
            }

            return View(site);
        }

        // POST: Admin/DeleteSite/5
        [HttpPost, ActionName("DeleteSite")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSiteConfirmed(int id)
        {
            // First delete related cultural info
            var culturalInfo = await _context.CulturalInfo
                .FirstOrDefaultAsync(c => c.SiteId == id);

            if (culturalInfo != null)
            {
                _context.CulturalInfo.Remove(culturalInfo);
                await _context.SaveChangesAsync();
            }

            // Then delete related translations
            var translations = await _context.SiteTranslation
                .Where(t => t.SiteId == id)
                .ToListAsync();

            if (translations.Any())
            {
                _context.SiteTranslation.RemoveRange(translations);
                await _context.SaveChangesAsync();
            }

            // Delete related bookmarks
            var bookmarks = await _context.Bookmark
                .Where(b => b.SiteId == id)
                .ToListAsync();

            if (bookmarks.Any())
            {
                _context.Bookmark.RemoveRange(bookmarks);
                await _context.SaveChangesAsync();
            }

            // Delete related ratings
            var ratings = await _context.Rating
                .Where(r => r.SiteId == id)
                .ToListAsync();

            if (ratings.Any())
            {
                _context.Rating.RemoveRange(ratings);
                await _context.SaveChangesAsync();
            }

            // Finally delete the site
            var site = await _context.Site.FindAsync(id);
            if (site != null)
            {
                _context.Site.Remove(site);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Sites));
        }

        // GET: Admin/EditCity/5
        public async Task<IActionResult> EditCity(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var city = await _context.City
                .Include(c => c.Sites)
                .Include(c => c.Guides)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (city == null)
            {
                return NotFound();
            }

            return View(city);
        }

        // POST: Admin/EditCity/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCity(int id, City city)
        {
            if (id != city.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(city);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CityExists(city.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Cities));
            }
            return View(city);
        }


        // GET: Admin/GuideApplications
        public async Task<IActionResult> GuideApplications()
        {
            try
            {
                var applications = await _context.GuideApplication
                    .Include(g => g.City)
                    .OrderByDescending(g => g.SubmissionDate)
                    .ToListAsync();

                return View(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guide applications");

                TempData["ErrorMessage"] = "An error occurred while loading guide applications: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: Admin/GuideApplicationDetail/5
        public async Task<IActionResult> GuideApplicationDetail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.GuideApplication
                .Include(g => g.City)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        // POST: Admin/ApproveGuideApplication/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveGuideApplication(int id)
        {
            try
            {
                var application = await _context.GuideApplication
                    .Include(g => g.City)
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (application == null)
                {
                    return NotFound();
                }

                if (application.Status != ApplicationStatus.Pending)
                {
                    TempData["ErrorMessage"] = "This application has already been processed.";
                    return RedirectToAction(nameof(GuideApplications));
                }

                // Create a new Guide entry with proper null handling
                var guide = new Guide
                {
                    Email = application.Email ?? "",
                    FullName = application.FullName ?? "",
                    NationalId = application.NationalId ?? "",
                    Password = application.Password ?? "", // In a real app, this would be hashed
                    CityId = application.CityId
                };

                _context.Guide.Add(guide);

                // Update application status
                application.Status = ApplicationStatus.Approved;
                application.ReviewDate = DateTime.UtcNow;
                if (application.RejectionReason == null)
                {
                    application.RejectionReason = "";
                }
                _context.Update(application);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Guide application approved successfully!";
                return RedirectToAction(nameof(GuideApplications));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving guide application with ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while approving the guide application. Please try again.";
                return RedirectToAction(nameof(GuideApplications));
            }
        }

        // POST: Admin/RejectGuideApplication/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectGuideApplication(int id, string rejectionReason)
        {
            var application = await _context.GuideApplication
                .FirstOrDefaultAsync(g => g.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != ApplicationStatus.Pending)
            {
                TempData["ErrorMessage"] = "This application has already been processed.";
                return RedirectToAction(nameof(GuideApplications));
            }

            // Update application status
            application.Status = ApplicationStatus.Rejected;
            application.RejectionReason = rejectionReason; // This can be null if no reason provided
            application.ReviewDate = DateTime.UtcNow;

            _context.Update(application);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Guide application rejected successfully.";
            return RedirectToAction(nameof(GuideApplications));
        }

        // GET: Admin/ManageGuides
        public async Task<IActionResult> ManageGuides()
        {
            var guides = await _context.Guide
                .Include(g => g.City)
                .OrderBy(g => g.FullName)
                .ToListAsync();

            return View(guides);
        }

        // GET: Admin/EditGuide/5
        public async Task<IActionResult> EditGuide(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var guide = await _context.Guide
                .Include(g => g.City)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (guide == null)
            {
                return NotFound();
            }

            ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
            return View(guide);
        }

        // POST: Admin/EditGuide/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGuide(int id, Guide guide)
        {
            if (id != guide.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(guide);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Guide information updated successfully!";
                    return RedirectToAction(nameof(ManageGuides));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GuideExists(guide.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
            return View(guide);
        }

        // POST: Admin/DeleteGuide/5
        [HttpPost, ActionName("DeleteGuide")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGuideConfirmed(int id)
        {
            var guide = await _context.Guide.FindAsync(id);
            if (guide != null)
            {
                _context.Guide.Remove(guide);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Guide deleted successfully.";
            }

            return RedirectToAction(nameof(ManageGuides));
        }

        private bool GuideExists(int id)
        {
            return _context.Guide.Any(e => e.Id == id);
        }

        // GET: Admin/CreateCity
        public IActionResult CreateCity()
        {
            return View();
        }

        // POST: Admin/CreateCity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCity(City city)
        {
            if (ModelState.IsValid)
            {
                _context.City.Add(city);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Cities));
            }
            return View(city);
        }

        // GET: Admin/DeleteCity/5
        public async Task<IActionResult> DeleteCity(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var city = await _context.City
                .FirstOrDefaultAsync(c => c.Id == id);

            if (city == null)
            {
                return NotFound();
            }

            // Check if there are sites associated with this city
            var hasSites = await _context.Site.AnyAsync(s => s.CityId == id);
            if (hasSites)
            {
                TempData["ErrorMessage"] = "Cannot delete this city because there are sites associated with it. Delete the sites first.";
                return RedirectToAction(nameof(Cities));
            }

            return View(city);
        }

        // POST: Admin/DeleteCity/5
        [HttpPost, ActionName("DeleteCity")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCityConfirmed(int id)
        {
            // Check again if there are sites associated with this city
            var hasSites = await _context.Site.AnyAsync(s => s.CityId == id);
            if (hasSites)
            {
                TempData["ErrorMessage"] = "Cannot delete this city because there are sites associated with it. Delete the sites first.";
                return RedirectToAction(nameof(Cities));
            }

            // Delete related guides
            var guides = await _context.Guide
                .Where(g => g.CityId == id)
                .ToListAsync();

            if (guides.Any())
            {
                _context.Guide.RemoveRange(guides);
                await _context.SaveChangesAsync();
            }

            // Delete the city
            var city = await _context.City.FindAsync(id);
            if (city != null)
            {
                _context.City.Remove(city);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Cities));
        }

        // GET: Admin/ToggleAdmin/5
        public async Task<IActionResult> ToggleAdmin(int id)
        {
            var client = await _context.Client
                .Include(c => c.Administrator)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            if (client.Administrator != null)
            {
                // Remove admin privileges
                _context.Administrator.Remove(client.Administrator);
            }
            else
            {
                // Add admin privileges
                var administrator = new Administrator
                {
                    ClientId = client.Id
                };
                _context.Administrator.Add(administrator);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Users));
        }

        // Helper methods
        private bool SiteExists(int id)
        {
            return _context.Site.Any(e => e.Id == id);
        }

        private bool CityExists(int id)
        {
            return _context.City.Any(e => e.Id == id);
        }
    }
}