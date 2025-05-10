using Athrna.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Athrna.Data;
using System.Diagnostics;

namespace Athrna.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Load all sites from the database to display on the map
                var sites = await _context.Site
                    .Include(s => s.City)
                    .Include(s => s.CulturalInfo)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Location,
                        s.SiteType,
                        s.Description,
                        s.ImagePath,
                        CityName = s.City.Name,
                        CityId = s.CityId,
                        CulturalInfo = s.CulturalInfo != null ? new
                        {
                            s.CulturalInfo.EstablishedDate,
                            s.CulturalInfo.Summary
                        } : null
                    })
                    .ToListAsync();

                // Pass the sites data to the view
                ViewBag.MapSites = sites;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sites for map on homepage");
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null, string message = null)
        {
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            if (statusCode.HasValue)
            {
                errorViewModel.StatusCode = statusCode.Value;
                errorViewModel.ErrorMessage = message ?? GetDefaultErrorMessage(statusCode.Value);
            }
            else
            {
                // Get details from IExceptionHandlerPathFeature if available
                var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
                if (exceptionFeature != null)
                {
                    errorViewModel.ExceptionPath = exceptionFeature.Path;
                    errorViewModel.ExceptionType = exceptionFeature.Error.GetType().Name;
                    errorViewModel.ErrorMessage = exceptionFeature.Error.Message;

                    // Log the exception
                    _logger.LogError(exceptionFeature.Error,
                        "Error occurred on path {Path} with exception {ExceptionType}",
                        exceptionFeature.Path, errorViewModel.ExceptionType);
                }
            }

            // Determine if this is a development environment
            errorViewModel.ShowDetails = HttpContext.Request.Host.Host.Contains("localhost") ||
                                        HttpContext.Request.Host.Host.Equals("127.0.0.1");

            return View(errorViewModel);
        }

        private string GetDefaultErrorMessage(int statusCode)
        {
            return statusCode switch
            {
                400 => "Bad Request - The server cannot process the request due to invalid syntax.",
                401 => "Unauthorized - Authentication is required to access this resource.",
                403 => "Forbidden - You do not have permission to access this resource.",
                404 => "Not Found - The requested resource was not found on the server.",
                500 => "Internal Server Error - The server encountered an error processing your request.",
                _ => "An error occurred while processing your request."
            };
        }

        [Route("/Home/HandleError")]
        public IActionResult HandleError(int statusCode)
        {
            if (statusCode == 404)
            {
                // Log the 404 error
                _logger.LogWarning("404 error occurred. Path: {Path}", HttpContext.Request.Path);

                // Return the custom 404 view
                return View("NotFound");
            }

            // For other status codes, use the generic Error view
            var viewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = statusCode,
                ErrorMessage = GetDefaultErrorMessage(statusCode)
            };

            return View("Error", viewModel);
        }
    }
}