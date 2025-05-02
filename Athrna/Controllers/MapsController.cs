using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Athrna.Controllers
{
    public class MapsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MapsController> _logger;

        public MapsController(IConfiguration configuration, ILogger<MapsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // GET: /Maps/GetApiKey
        [HttpGet]
        public IActionResult GetApiKey()
        {
            try
            {
                // Get the API key from configuration
                var apiKey = _configuration["GoogleMaps:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Google Maps API key not found in configuration");
                    return Json(new { error = "API key not configured", key = "DEMO_KEY" });
                }

                // Return the API key in a secure way
                return Json(new { key = apiKey });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Google Maps API key");
                return StatusCode(500, new { error = "Error retrieving API key", key = "DEMO_KEY" });
            }
        }

        // GET: /Maps/UpdateSiteCoordinates
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IActionResult UpdateSiteCoordinates()
        {
            // This is an admin-only page to help update site coordinates
            return View();
        }
    }
}