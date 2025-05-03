using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Athrna.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Athrna.Controllers
{
    public class CulturalInfoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CulturalInfoController> _logger;
        private readonly IWebHostEnvironment _hostEnvironment;

        public CulturalInfoController(
            ApplicationDbContext context,
            ILogger<CulturalInfoController> logger,
            IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        // GET: CulturalInfo/GetAudio
        [HttpGet]
        public async Task<IActionResult> GetAudio(int siteId, string siteName)
        {
            try
            {
                // Log the request
                _logger.LogInformation("Audio request received for site ID: {SiteId}, Name: {SiteName}", siteId, siteName);

                // Get cultural info for the site
                var culturalInfo = await _context.CulturalInfo
                    .Include(c => c.Site)
                    .FirstOrDefaultAsync(c => c.SiteId == siteId);

                if (culturalInfo == null)
                {
                    _logger.LogWarning("Cultural info not found for site ID: {SiteId}", siteId);
                    return NotFound(new { error = "Cultural information not found" });
                }

                // Check if audio file already exists
                string fileName = $"site_{siteId}_audio.mp3";
                string audioDirectory = Path.Combine(_hostEnvironment.WebRootPath, "audio", "cultural");
                string filePath = Path.Combine(audioDirectory, fileName);
                string fileUrl = $"/audio/cultural/{fileName}";

                // Ensure directory exists
                if (!Directory.Exists(audioDirectory))
                {
                    Directory.CreateDirectory(audioDirectory);
                }

                // If file doesn't exist, create it
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogInformation("Audio file does not exist, will generate: {FilePath}", filePath);

                    // In a real application, here you would generate the audio file
                    // using a text-to-speech service like Google Cloud Text-to-Speech,
                    // Amazon Polly, or another service.

                    // For this demonstration, we'll simulate audio generation by
                    // copying a sample audio file that would be included with the application

                    string sampleAudioPath = Path.Combine(_hostEnvironment.WebRootPath, "audio", "samples", "sample_audio.mp3");

                    if (System.IO.File.Exists(sampleAudioPath))
                    {
                        System.IO.File.Copy(sampleAudioPath, filePath, true);
                        _logger.LogInformation("Created audio file at: {FilePath}", filePath);
                    }
                    else
                    {
                        // If sample audio doesn't exist, log error
                        _logger.LogError("Sample audio file not found at: {SamplePath}", sampleAudioPath);

                        // Create a directory for samples if it doesn't exist
                        string sampleDirectory = Path.Combine(_hostEnvironment.WebRootPath, "audio", "samples");
                        if (!Directory.Exists(sampleDirectory))
                        {
                            Directory.CreateDirectory(sampleDirectory);
                            _logger.LogInformation("Created sample audio directory: {SampleDir}", sampleDirectory);
                        }

                        // Return a fallback audio URL
                        return Json(new { audioUrl = "/audio/samples/fallback_audio.mp3" });
                    }
                }
                else
                {
                    _logger.LogInformation("Audio file already exists: {FilePath}", filePath);
                }

                // Track audio file access
                await TrackAudioAccess(siteId);

                // Return the URL to the audio file
                return Json(new { audioUrl = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating audio for site ID: {SiteId}", siteId);
                return StatusCode(500, new { error = "An error occurred while generating audio" });
            }
        }

        private async Task TrackAudioAccess(int siteId)
        {
            try
            {
                // Here you could implement logic to track audio accesses,
                // such as updating a counter in the database or logging to analytics

                // Example: update site stats in database
                var site = await _context.Site.FindAsync(siteId);
                if (site != null)
                {
                    // If you had an AudioPlays property on the Site model, you could update it:
                    // site.AudioPlays++;
                    // await _context.SaveChangesAsync();

                    // For now, just log it
                    _logger.LogInformation("Tracked audio access for site ID: {SiteId}", siteId);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw, as this is a non-critical operation
                _logger.LogError(ex, "Error tracking audio access for site ID: {SiteId}", siteId);
            }
        }
    }
}