using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

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
            try
            {
                // Dashboard is accessible to all admin levels
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

                // Pass current admin role level to view
                ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();

                return View(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard.");
                TempData["ErrorMessage"] = "An error occurred loading the dashboard.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/Sites
        [Authorize(Policy = "ContentManagement")]
        public async Task<IActionResult> Sites()
        {
            var sites = await _context.Site
                .Include(s => s.City)
                .Include(s => s.CulturalInfo)
                .ToListAsync();

            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            return View(sites);
        }

        // GET: Admin/Cities
        [Authorize(Policy = "ContentManagement")]
        public async Task<IActionResult> Cities()
        {
            var cities = await _context.City
                .Include(c => c.Sites)
                .ToListAsync();

            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            return View(cities);
        }

        // GET: Admin/Users
        [Authorize(Policy = "UserManagement")]
        public async Task<IActionResult> Users()
        {
            var users = await _context.Client
                .Include(c => c.Administrator)
                .ToListAsync();

            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            return View(users);
        }

        // GET: Admin/EditSite/5
        public async Task<IActionResult> EditSite(int? id)
        {
            // Content management (level 3 or higher)
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);

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
            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            return View(site);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSite(int id, IFormCollection form, IFormFile imageFile)
        {
            // Content management (level 3 or higher)
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);

            // Log what we received
            _logger.LogInformation("EditSite POST received for ID: {0}", id);
            foreach (var key in form.Keys)
            {
                _logger.LogInformation("Form field: {0} = {1}", key, form[key]);
            }

            try
            {
                // Get existing site with all related data
                var site = await _context.Site
                    .Include(s => s.CulturalInfo)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (site == null)
                {
                    _logger.LogWarning("Site not found with ID: {0}", id);
                    return NotFound();
                }

                // Update basic site properties directly from form
                site.Name = form["Name"];
                site.CityId = int.Parse(form["CityId"]);
                site.SiteType = form["SiteType"];
                site.Location = form["Location"];
                site.Description = form["Description"];

                // Process cultural info
                var culturalInfoSummary = form["CulturalInfo.Summary"].ToString();
                int culturalInfoDate = 0;
                if (int.TryParse(form["CulturalInfo.EstablishedDate"], out culturalInfoDate))
                {
                    _logger.LogInformation("Parsed cultural info date: {0}", culturalInfoDate);
                }
                else
                {
                    _logger.LogWarning("Failed to parse cultural info date: {0}", form["CulturalInfo.EstablishedDate"]);
                }

                // Update or create CulturalInfo
                if (site.CulturalInfo != null)
                {
                    _logger.LogInformation("Updating existing CulturalInfo with ID: {0}", site.CulturalInfo.Id);
                    site.CulturalInfo.Summary = culturalInfoSummary;
                    site.CulturalInfo.EstablishedDate = culturalInfoDate;
                }
                else
                {
                    _logger.LogInformation("Creating new CulturalInfo for site ID: {0}", site.Id);
                    site.CulturalInfo = new CulturalInfo
                    {
                        SiteId = site.Id,
                        Summary = culturalInfoSummary,
                        EstablishedDate = culturalInfoDate
                    };
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ViewBag.Cities = await _context.City.ToListAsync();
                        return View(site);
                    }

                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        ViewBag.Cities = await _context.City.ToListAsync();
                        return View(site);
                    }

                    // Generate unique filename
                    string uniqueFileName = $"{id}_{Guid.NewGuid().ToString("N")}{extension}";
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "sites");

                    // Create directory if it doesn't exist
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    if (!string.IsNullOrEmpty(site.ImagePath) &&
        site.ImagePath.StartsWith("/images/sites/") &&
        System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", site.ImagePath.TrimStart('/'))))
                    {
                        try
                        {
                            System.IO.File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", site.ImagePath.TrimStart('/')));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error deleting previous image: {0}", site.ImagePath);
                        }
                    }

                    // Save the new file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    // Update the image path
                    site.ImagePath = "/images/sites/" + uniqueFileName;
                    _logger.LogInformation("New image saved at: {0}", site.ImagePath);
                }

                // Save all changes
                await _context.SaveChangesAsync();
                _logger.LogInformation("Site updated successfully: ID {0}", site.Id);

                TempData["SuccessMessage"] = "Site updated successfully!";
                return RedirectToAction(nameof(Sites));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site with ID {0}: {1}", id, ex.Message);
                TempData["ErrorMessage"] = $"Error updating site: {ex.Message}";

                // Re-create the site object for the view
                var site = new Site { Id = id };

                try
                {
                    // Try to populate with form values
                    site.Name = form["Name"];
                    site.CityId = int.Parse(form["CityId"]);
                    site.SiteType = form["SiteType"];
                    site.Location = form["Location"];
                    site.Description = form["Description"];

                    site.CulturalInfo = new CulturalInfo
                    {
                        Summary = form["CulturalInfo.Summary"],
                        EstablishedDate = int.Parse(form["CulturalInfo.EstablishedDate"])
                    };
                }
                catch
                {
                    // Ignore errors while rebuilding the model
                }

                ViewBag.Cities = await _context.City.ToListAsync();
                return View(site);
            }
        }

        // GET: Admin/CreateSite
        public async Task<IActionResult> CreateSite()
        {
            ViewBag.Cities = await _context.City.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSite(
            string Name,
            int CityId,
            string SiteType,
            string Location,
            string Description,
            string CulturalSummary,
            int? EstablishedDate,
            IFormFile imageFile,
            int? AdminRoleLevel)
        {
            // Use AdminRoleLevel from form if provided
            if (AdminRoleLevel.HasValue)
            {
                TempData["AdminRoleLevel"] = AdminRoleLevel.Value;
            }

            // Content management (level 3 or higher)
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);

            // Log incoming data
            _logger.LogInformation("Creating site with Name: {Name}, CityId: {CityId}, SiteType: {SiteType}",
                Name, CityId, SiteType);
            _logger.LogInformation("Cultural info: Summary length: {SummaryLength}, EstablishedDate: {EstablishedDate}",
                CulturalSummary?.Length ?? 0, EstablishedDate);

            try
            {
                // Create and validate Site object manually
                if (string.IsNullOrEmpty(Name) || CityId <= 0 || string.IsNullOrEmpty(Location) || string.IsNullOrEmpty(Description))
                {
                    _logger.LogWarning("Missing required fields");

                    if (string.IsNullOrEmpty(Name))
                        ModelState.AddModelError("Name", "Site name is required.");

                    if (CityId <= 0)
                        ModelState.AddModelError("CityId", "Please select a valid city.");

                    if (string.IsNullOrEmpty(Location))
                        ModelState.AddModelError("Location", "Location is required.");

                    if (string.IsNullOrEmpty(Description))
                        ModelState.AddModelError("Description", "Description is required.");

                    ViewBag.Cities = await _context.City.ToListAsync();

                    // Create a model to return to the view
                    var siteModel = new Site
                    {
                        Name = Name,
                        CityId = CityId,
                        SiteType = SiteType,
                        Location = Location,
                        Description = Description,
                        CulturalInfo = new CulturalInfo
                        {
                            Summary = CulturalSummary,
                            EstablishedDate = EstablishedDate ?? 0
                        }
                    };

                    return View(siteModel);
                }

                // All validation passed - create the site directly
                var site = new Site
                {
                    Name = Name,
                    CityId = CityId,
                    SiteType = SiteType ?? "",
                    Location = Location,
                    Description = Description,
                    // Set default image placeholder
                    ImagePath = "/api/placeholder/400/300"
                };

                // Create the site first without cultural info
                _context.Site.Add(site);
                var result = await _context.SaveChangesAsync();
                _logger.LogInformation("Site created, ID: {Id}, Result: {Result}", site.Id, result);

                // Handle image upload if provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                    if (allowedExtensions.Contains(extension) && imageFile.Length <= 5 * 1024 * 1024)
                    {
                        try
                        {
                            string uniqueFileName = $"{site.Id}_{Guid.NewGuid().ToString("N")}{extension}";
                            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "sites");

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
                            site.ImagePath = "/images/sites/" + uniqueFileName;

                            // Save the updated image path
                            _context.Update(site);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("Image saved successfully: {Path}", site.ImagePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error saving image file");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Invalid image - extension: {Extension}, size: {Size}",
                            extension, imageFile.Length);
                    }
                }

                // Add cultural info if provided
                if (!string.IsNullOrEmpty(CulturalSummary) || EstablishedDate.HasValue)
                {
                    try
                    {
                        var culturalInfo = new CulturalInfo
                        {
                            SiteId = site.Id,
                            Summary = CulturalSummary ?? "",
                            EstablishedDate = EstablishedDate ?? 0
                        };

                        _context.CulturalInfo.Add(culturalInfo);
                        result = await _context.SaveChangesAsync();

                        _logger.LogInformation("Cultural info added, Result: {Result}", result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding cultural info");
                    }
                }

                // Success - redirect to sites list
                TempData["SuccessMessage"] = "New site created successfully!";
                return RedirectToAction(nameof(Sites));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site: {Error}", ex.Message);
                ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");

                ViewBag.Cities = await _context.City.ToListAsync();

                // Create a model to return to the view
                var siteModel = new Site
                {
                    Name = Name,
                    CityId = CityId,
                    SiteType = SiteType,
                    Location = Location,
                    Description = Description,
                    CulturalInfo = new CulturalInfo
                    {
                        Summary = CulturalSummary,
                        EstablishedDate = EstablishedDate ?? 0
                    }
                };

                return View(siteModel);
            }
        }
        // GET: Admin/DeleteSite/5
        public async Task<IActionResult> DeleteSite(int? id)
        {
            // Content management (level 3 or higher requires delete permission)
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);
            if (id == null)
            {
                return NotFound();
            }

            try
            {
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
                ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
                return View(site);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving site with ID {Id} for deletion", id);
                TempData["ErrorMessage"] = "An error occurred while retrieving the site.";
                return RedirectToAction(nameof(Sites));
            }
        }

        // POST: Admin/DeleteSite/5
        [HttpPost, ActionName("DeleteSite")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSiteConfirmed(int id)
        {
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);
            try
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
                    // Delete the site image file if it exists and is not a placeholder
                    if (!string.IsNullOrEmpty(site.ImagePath) &&
                        site.ImagePath.StartsWith("/images/sites/") &&
                        !site.ImagePath.Contains("placeholder"))
                    {
                        var imagePath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            site.ImagePath.TrimStart('/'));

                        if (System.IO.File.Exists(imagePath))
                        {
                            try
                            {
                                System.IO.File.Delete(imagePath);
                            }
                            catch (Exception ex)
                            {
                                // Log error but continue with deletion
                                _logger.LogError(ex, "Error deleting site image file: {Path}", imagePath);
                            }
                        }
                    }

                    _context.Site.Remove(site);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Site deleted successfully!";
                }

                return RedirectToAction(nameof(Sites));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site with ID {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the site. Please try again.";
                return RedirectToAction(nameof(Sites));
            }
        }
        // GET: Admin/EditCity/5
        public async Task<IActionResult> EditCity(int? id)
        {
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);
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
            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            return View(city);
        }

        // POST: Admin/EditCity/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCity(int id, string Name)
        {
            // Content management (level 3 or higher)
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);

            if (string.IsNullOrEmpty(Name))
            {
                ModelState.AddModelError("Name", "City name is required");
                var city = await _context.City.FindAsync(id);
                return View(city);
            }

            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                _logger.LogInformation("Updating city ID {Id} with name: {Name}", id, Name);

                // Check if city exists
                var city = await _context.City.FindAsync(id);
                if (city == null)
                {
                    _logger.LogWarning("City not found with ID: {Id}", id);
                    return NotFound();
                }

                // Check if new name conflicts with existing city (other than this one)
                var existingCity = await _context.City.FirstOrDefaultAsync(c =>
                    c.Id != id && c.Name.ToLower() == Name.ToLower());

                if (existingCity != null)
                {
                    ModelState.AddModelError("Name", "A city with this name already exists");
                    return View(city);
                }

                // Update city name
                city.Name = Name;

                _context.Update(city);
                await _context.SaveChangesAsync();

                _logger.LogInformation("City updated successfully");
                TempData["SuccessMessage"] = "City updated successfully!";

                return RedirectToAction(nameof(Cities));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!CityExists(id))
                {
                    _logger.LogWarning("City not found with ID: {Id}", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating city: {Id}", id);
                    ModelState.AddModelError("", "The city was modified by another user. Please try again.");
                    var city = await _context.City.FindAsync(id);
                    return View(city);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating city: {Error}", ex.Message);
                ModelState.AddModelError("", $"An error occurred while updating the city: {ex.Message}");
                var city = await _context.City.FindAsync(id);
                return View(city);
            }
        }

        // GET: Admin/GuideApplications
        [Authorize(Policy = "ContentManagement")]
        public async Task<IActionResult> GuideApplications()
        {
            var applications = await _context.GuideApplication
                .Include(g => g.City)
                .OrderByDescending(g => g.SubmissionDate)
                .ToListAsync();

            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            return View(applications);
        }

        // GET: Admin/GuideApplicationDetail/5
        public async Task<IActionResult> GuideApplicationDetail(int? id)
        {
            // Guide management (level 3 or higher)
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);

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
            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            return View(application);
        }

        // POST: Admin/ApproveGuideApplication/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveGuideApplication(int id)
        {
            // Guide management (level 3 or higher)
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);
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
            // Guide management (level 3 or higher)
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);

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
            // Guide management (level 3 or higher)
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);

            var guides = await _context.Guide
                .Include(g => g.City)
                .OrderBy(g => g.FullName)
                .ToListAsync();

            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            return View(guides);
        }

        // GET: Admin/EditGuide/5
        public async Task<IActionResult> EditGuide(int? id)
        {
            // Guide management (level 3 or higher)
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);

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
            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
            return View(guide);
        }

        // POST: Admin/EditGuide/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGuide(int id, Guide guide)
        {
            // Guide management (level 3 or higher)
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);

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
            // Guide management (level 3 or higher)
            if (!await HasRequiredRoleLevel(3))
                return Unauthorized(3);

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

        // CITY MANAGEMENT FUNCTIONS FOR ADMINCONTROLLER

        // GET: Admin/CreateCity
        public IActionResult CreateCity()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCity(string Name, int? AdminRoleLevel)
        {
            // Use AdminRoleLevel from form if provided
            if (AdminRoleLevel.HasValue)
            {
                TempData["AdminRoleLevel"] = AdminRoleLevel.Value;
            }

            if (string.IsNullOrEmpty(Name))
            {
                ModelState.AddModelError("Name", "City name is required");
                return View();
            }

            try
            {
                _logger.LogInformation("Creating new city: {Name}", Name);

                // Check if city with same name already exists
                var existingCity = await _context.City.FirstOrDefaultAsync(c => c.Name.ToLower() == Name.ToLower());
                if (existingCity != null)
                {
                    ModelState.AddModelError("Name", "A city with this name already exists");
                    return View();
                }

                var city = new City
                {
                    Name = Name
                };

                _context.City.Add(city);
                await _context.SaveChangesAsync();

                _logger.LogInformation("City created successfully with ID: {Id}", city.Id);
                TempData["SuccessMessage"] = "City created successfully!";

                return RedirectToAction(nameof(Cities));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating city: {Error}", ex.Message);
                ModelState.AddModelError("", $"An error occurred while creating the city: {ex.Message}");
                return View();
            }
        }

        // GET: Admin/DeleteCity/5
        public async Task<IActionResult> DeleteCity(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
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

                // Check if there are guides associated with this city
                var hasGuides = await _context.Guide.AnyAsync(g => g.CityId == id);
                if (hasGuides)
                {
                    TempData["ErrorMessage"] = "Cannot delete this city because there are guides associated with it. Reassign the guides first.";
                    return RedirectToAction(nameof(Cities));
                }

                return View(city);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving city for deletion: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while retrieving the city.";
                return RedirectToAction(nameof(Cities));
            }
        }

        // POST: Admin/DeleteCity/5
        [HttpPost, ActionName("DeleteCity")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCityConfirmed(int id)
        {
            try
            {
                _logger.LogInformation("Attempting to delete city ID: {Id}", id);

                // Check again if there are sites or guides associated with this city
                var hasSites = await _context.Site.AnyAsync(s => s.CityId == id);
                if (hasSites)
                {
                    _logger.LogWarning("Cannot delete city {Id} - has associated sites", id);
                    TempData["ErrorMessage"] = "Cannot delete this city because there are sites associated with it. Delete the sites first.";
                    return RedirectToAction(nameof(Cities));
                }

                var hasGuides = await _context.Guide.AnyAsync(g => g.CityId == id);
                if (hasGuides)
                {
                    _logger.LogWarning("Cannot delete city {Id} - has associated guides", id);
                    TempData["ErrorMessage"] = "Cannot delete this city because there are guides associated with it. Reassign the guides first.";
                    return RedirectToAction(nameof(Cities));
                }

                // Get the city
                var city = await _context.City.FindAsync(id);
                if (city == null)
                {
                    _logger.LogWarning("City not found with ID: {Id}", id);
                    return NotFound();
                }

                // Delete the city
                _context.City.Remove(city);
                await _context.SaveChangesAsync();

                _logger.LogInformation("City deleted successfully: {Id}", id);
                TempData["SuccessMessage"] = "City deleted successfully!";

                return RedirectToAction(nameof(Cities));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting city: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the city. Please try again.";
                return RedirectToAction(nameof(Cities));
            }
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

        // GET: Admin/Ratings
        [Authorize(Policy = "UserManagement")]
        public async Task<IActionResult> Ratings()
        {
            var ratings = await _context.Rating
                .Include(r => r.Client)
                .Include(r => r.Site)
                    .ThenInclude(s => s.City)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            return View(ratings);
        }

        // POST: Admin/DeleteRating/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRating(int id)
        {
            // User management (level 4 or higher)
            if (!await HasRequiredRoleLevel(4))
                return Unauthorized(4);
            try
            {
                _logger.LogInformation("Attempting to delete rating with ID: {Id}", id);

                var rating = await _context.Rating.FindAsync(id);
                if (rating == null)
                {
                    _logger.LogWarning("Rating not found with ID: {Id}", id);
                    return NotFound();
                }

                _context.Rating.Remove(rating);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Rating deleted successfully: {Id}", id);
                TempData["SuccessMessage"] = "Rating deleted successfully!";

                return RedirectToAction(nameof(Ratings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting rating with ID {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the rating. Please try again.";
                return RedirectToAction(nameof(Ratings));
            }
        }

        // POST: Admin/BanUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(int id, string banReason)
        {
            // User management (level 4 or higher)
            if (!await HasRequiredRoleLevel(4))
                return Unauthorized(4);
            try
            {
                _logger.LogInformation("Attempting to ban user with ID: {Id}", id);

                var client = await _context.Client.FindAsync(id);
                if (client == null)
                {
                    _logger.LogWarning("User not found with ID: {Id}", id);
                    return NotFound();
                }

                // Don't allow banning of admin accounts
                var isAdmin = await _context.Administrator.AnyAsync(a => a.ClientId == client.Id);
                if (isAdmin)
                {
                    TempData["ErrorMessage"] = "Cannot ban administrator accounts.";
                    return RedirectToAction(nameof(Users));
                }

                // Set ban properties
                client.IsBanned = true;
                client.BanReason = banReason;
                client.BannedAt = DateTime.UtcNow;

                _context.Update(client);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User banned successfully: {Id}, Username: {Username}", client.Id, client.Username);
                TempData["SuccessMessage"] = $"User '{client.Username}' has been banned successfully.";

                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning user with ID {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while banning the user. Please try again.";
                return RedirectToAction(nameof(Users));
            }
        }

        // POST: Admin/UnbanUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanUser(int id)
        {
            // User management (level 4 or higher)
            if (!await HasRequiredRoleLevel(4))
                return Unauthorized(4);
            try
            {
                _logger.LogInformation("Attempting to unban user with ID: {Id}", id);

                var client = await _context.Client.FindAsync(id);
                if (client == null)
                {
                    _logger.LogWarning("User not found with ID: {Id}", id);
                    return NotFound();
                }

                // Clear ban properties
                client.IsBanned = false;
                client.BanReason = null;
                client.BannedAt = null;

                _context.Update(client);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User unbanned successfully: {Id}, Username: {Username}", client.Id, client.Username);
                TempData["SuccessMessage"] = $"User '{client.Username}' has been unbanned successfully.";

                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unbanning user with ID {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while unbanning the user. Please try again.";
                return RedirectToAction(nameof(Users));
            }
        }
        // GET: Admin/ManageAdmins
        [Authorize(Policy = "AdminManagement")]
        public async Task<IActionResult> ManageAdmins()
        {
            var admins = await _context.Administrator
                .Include(a => a.Client)
                .ToListAsync();

            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            return View(admins);
        }

        // GET: Admin/EditAdmin/5
        public async Task<IActionResult> EditAdmin(int? id)
        {
            // Admin management (only level 1 can edit admins)
            if (!await HasRequiredRoleLevel(1))
                return Unauthorized(1);

            if (id == null)
            {
                return NotFound();
            }

            var admin = await _context.Administrator
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (admin == null)
            {
                return NotFound();
            }

            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            return View(admin);
        }

        // POST: Admin/EditAdmin/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdmin(int id, int roleLevel)
        {
            // Admin management (only level 1 can edit admins)
            if (!await HasRequiredRoleLevel(1))
                return Unauthorized(1);

            if (id <= 0 || roleLevel < 1 || roleLevel > 5)
            {
                return BadRequest();
            }

            var admin = await _context.Administrator.FindAsync(id);
            if (admin == null)
            {
                return NotFound();
            }

            // Prevent an admin from lowering their own role
            if (admin.ClientId == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))
            {
                TempData["ErrorMessage"] = "You cannot modify your own admin role.";
                return RedirectToAction(nameof(ManageAdmins));
            }

            // Update admin role
            admin.RoleLevel = roleLevel;
            _context.Update(admin);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Admin role updated successfully.";
            return RedirectToAction(nameof(ManageAdmins));
        }

        // GET: Admin/ToggleAdmin/5
        [Authorize(Policy = "AdminManagement")]
        public async Task<IActionResult> ToggleAdmin(int id, int roleLevel = 5)
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
                // Prevent an admin from removing their own admin status
                if (client.Id == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))
                {
                    TempData["ErrorMessage"] = "You cannot remove your own admin status.";
                    return RedirectToAction(nameof(Users));
                }

                // Remove admin privileges
                _context.Administrator.Remove(client.Administrator);
            }
            else
            {
                // Add admin privileges with specified role level
                // Ensure role level is within valid range
                if (roleLevel < 1 || roleLevel > 5)
                {
                    roleLevel = 5; // Default to lowest permission if invalid
                }

                var administrator = new Administrator
                {
                    ClientId = client.Id,
                    RoleLevel = roleLevel
                };
                _context.Administrator.Add(administrator);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            try
            {
                // Get current admin role level 
                int adminRoleLevel = await GetCurrentAdminRoleLevel();

                return Json(new
                {
                    isAdmin = true,
                    adminRoleLevel = adminRoleLevel,
                    username = User.Identity.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user info");
                return Json(new
                {
                    isAdmin = false,
                    error = "Failed to retrieve user information"
                });
            }
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
        private async Task<int> GetCurrentAdminRoleLevel()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Administrator"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var admin = await _context.Administrator
                    .FirstOrDefaultAsync(a => a.ClientId == userId);

                return admin?.RoleLevel ?? 5; // Default to lowest permission if not found
            }
            return 5; // Default to lowest permission level
        }

        // Check if user has required role level
        private async Task<bool> HasRequiredRoleLevel(int requiredLevel)
        {
            var currentLevel = await GetCurrentAdminRoleLevel();
            return currentLevel <= requiredLevel; // Lower numbers have higher permissions
        }

        // Unauthorized action result
        private IActionResult Unauthorized(int requiredLevel)
        {
            TempData["ErrorMessage"] = $"You need admin role level {requiredLevel} or higher to access this feature.";
            return RedirectToAction("Index");
        }
    
    public async Task<IActionResult> MapSiteIds()
        {
            var sites = await _context.Site.Include(s => s.City).ToListAsync();
            return View(sites);
        }
    }
}