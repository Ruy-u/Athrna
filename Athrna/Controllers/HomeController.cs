using Athrna.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Athrna.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
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