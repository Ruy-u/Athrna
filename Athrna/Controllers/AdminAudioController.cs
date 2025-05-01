using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Athrna.Controllers
{
    [Authorize(Policy = "ContentManagement")]
    public class AdminAudioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminAudioController> _logger;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminAudioController(
            ApplicationDbContext context,
            ILogger<AdminAudioController> logger,
            IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        // GET: AdminAudio
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get all sites with cultural info
                var sitesWithCulturalInfo = await _context.Site
                    .Include(s => s.CulturalInfo)
                    .Include(s => s.City)
                    .Where(s => s.CulturalInfo != null)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                // Check which sites have audio files
                var audioList = new List<SiteAudioViewModel>();
                string audioDirectory = Path.Combine(_hostEnvironment.WebRootPath, "audio", "cultural");

                foreach (var site in sitesWithCulturalInfo)
                {
                    string fileName = $"site_{site.Id}_audio.mp3";
                    string filePath = Path.Combine(audioDirectory, fileName);
                    bool hasAudio = System.IO.File.Exists(filePath);

                    var audioLastModified = hasAudio
                        ? System.IO.File.GetLastWriteTime(filePath)
                        : (DateTime?)null;

                    audioList.Add(new SiteAudioViewModel
                    {
                        Site = site,
                        HasAudio = hasAudio,
                        AudioLastModified = audioLastModified
                    });
                }

                ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
                return View(audioList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading audio management page");
                TempData["ErrorMessage"] = "An error occurred while loading the audio management page.";
                return RedirectToAction("Index", "Admin");
            }
        }

        // GET: AdminAudio/Manage/5
        public async Task<IActionResult> Manage(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var site = await _context.Site
                .Include(s => s.CulturalInfo)
                .Include(s => s.City)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (site == null || site.CulturalInfo == null)
            {
                return NotFound();
            }

            // Check if audio exists
            string fileName = $"site_{site.Id}_audio.mp3";
            string audioDirectory = Path.Combine(_hostEnvironment.WebRootPath, "audio", "cultural");
            string filePath = Path.Combine(audioDirectory, fileName);
            bool hasAudio = System.IO.File.Exists(filePath);

            var model = new AudioManageViewModel
            {
                Site = site,
                HasAudio = hasAudio,
                AudioText = site.CulturalInfo.Summary,
                Languages = await _context.Language.ToListAsync()
            };

            ViewBag.AdminRoleLevel = await GetCurrentAdminRoleLevel();
            return View(model);
        }

        // POST: AdminAudio/UploadAudio/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAudio(int id, IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an audio file to upload.";
                return RedirectToAction(nameof(Manage), new { id });
            }

            var site = await _context.Site.FindAsync(id);
            if (site == null)
            {
                return NotFound();
            }

            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".mp3", ".wav", ".ogg" };
                var extension = Path.GetExtension(audioFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Only MP3, WAV and OGG audio files are allowed.";
                    return RedirectToAction(nameof(Manage), new { id });
                }

                // Ensure audio directory exists
                string audioDirectory = Path.Combine(_hostEnvironment.WebRootPath, "audio", "cultural");
                if (!Directory.Exists(audioDirectory))
                {
                    Directory.CreateDirectory(audioDirectory);
                }

                // Save the file as MP3 regardless of input type
                string fileName = $"site_{id}_audio.mp3";
                string filePath = Path.Combine(audioDirectory, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }

                _logger.LogInformation("Audio file uploaded for site ID {SiteId}", id);
                TempData["SuccessMessage"] = "Audio file uploaded successfully!";
                return RedirectToAction(nameof(Manage), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading audio file for site ID {SiteId}", id);
                TempData["ErrorMessage"] = "An error occurred while uploading the audio file.";
                return RedirectToAction(nameof(Manage), new { id });
            }
        }

        // POST: AdminAudio/GenerateAudio/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAudio(int id, string audioText, string languageCode)
        {
            if (string.IsNullOrEmpty(audioText))
            {
                TempData["ErrorMessage"] = "Audio text cannot be empty.";
                return RedirectToAction(nameof(Manage), new { id });
            }

            var site = await _context.Site
                .Include(s => s.CulturalInfo)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (site == null || site.CulturalInfo == null)
            {
                return NotFound();
            }

            try
            {
                // In a real application, here you would integrate with a text-to-speech service
                // For this demonstration, we'll simulate generation by copying a sample file

                _logger.LogInformation("Generating audio for site ID {SiteId} in language {Language}", id, languageCode);

                // Ensure directory exists
                string audioDirectory = Path.Combine(_hostEnvironment.WebRootPath, "audio", "cultural");
                if (!Directory.Exists(audioDirectory))
                {
                    Directory.CreateDirectory(audioDirectory);
                }

                // For demonstration, copy the sample audio file
                string fileName = $"site_{id}_audio.mp3";
                string filePath = Path.Combine(audioDirectory, fileName);
                string sampleAudioPath = Path.Combine(_hostEnvironment.WebRootPath, "audio", "samples", "sample_audio.mp3");

                if (System.IO.File.Exists(sampleAudioPath))
                {
                    System.IO.File.Copy(sampleAudioPath, filePath, true);
                    _logger.LogInformation("Audio generated (simulated) for site ID {SiteId}", id);

                    // Update cultural info text if it was modified
                    if (audioText != site.CulturalInfo.Summary)
                    {
                        site.CulturalInfo.Summary = audioText;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Updated cultural info text for site ID {SiteId}", id);
                    }

                    TempData["SuccessMessage"] = "Audio generated successfully!";
                }
                else
                {
                    _logger.LogError("Sample audio file not found at {Path}", sampleAudioPath);
                    TempData["ErrorMessage"] = "Audio generation failed. Sample audio file not found.";
                }

                return RedirectToAction(nameof(Manage), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating audio for site ID {SiteId}", id);
                TempData["ErrorMessage"] = "An error occurred while generating the audio.";
                return RedirectToAction(nameof(Manage), new { id });
            }
        }

        // POST: AdminAudio/DeleteAudio/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAudio(int id)
        {
            try
            {
                string fileName = $"site_{id}_audio.mp3";
                string audioDirectory = Path.Combine(_hostEnvironment.WebRootPath, "audio", "cultural");
                string filePath = Path.Combine(audioDirectory, fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("Audio file deleted for site ID {SiteId}", id);
                    TempData["SuccessMessage"] = "Audio file deleted successfully!";
                }
                else
                {
                    _logger.LogWarning("Attempted to delete non-existent audio file for site ID {SiteId}", id);
                    TempData["WarningMessage"] = "No audio file found to delete.";
                }

                return RedirectToAction(nameof(Manage), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audio file for site ID {SiteId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the audio file.";
                return RedirectToAction(nameof(Manage), new { id });
            }
        }

        // GET: AdminAudio/Preview/5
        public IActionResult Preview(int id)
        {
            string fileName = $"site_{id}_audio.mp3";
            string audioDirectory = Path.Combine(_hostEnvironment.WebRootPath, "audio", "cultural");
            string filePath = Path.Combine(audioDirectory, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            // Return the physical file
            return PhysicalFile(filePath, "audio/mpeg");
        }

        private async Task<int> GetCurrentAdminRoleLevel()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Administrator"))
            {
                var userId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier));
                var admin = await _context.Administrator
                    .FirstOrDefaultAsync(a => a.ClientId == userId);

                return admin?.RoleLevel ?? 5; // Default to lowest permission if not found
            }
            return 5; // Default to lowest permission level
        }
    }

    public class SiteAudioViewModel
    {
        public Site Site { get; set; }
        public bool HasAudio { get; set; }
        public DateTime? AudioLastModified { get; set; }
    }

    public class AudioManageViewModel
    {
        public Site Site { get; set; }
        public bool HasAudio { get; set; }
        public string AudioText { get; set; }
        public List<Language> Languages { get; set; }
    }
}