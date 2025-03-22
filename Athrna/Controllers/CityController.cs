using Microsoft.AspNetCore.Mvc;
using Athrna.Models;

namespace Athrna.Controllers
{
    public class CityController : Controller
    {
        // This action will handle the exploration of cities by ID
        public IActionResult Explore(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index", "Home");
            }

            // Validate that the city ID is allowed
            string[] validCities = new[] { "madinah", "riyadh", "alula" };
            if (!validCities.Contains(id.ToLower()))
            {
                return NotFound();
            }

            // Create a model for the city (you'd typically get this from a database)
            var cityModel = new CityViewModel
            {
                Id = id.ToLower(),
                Name = char.ToUpper(id[0]) + id.Substring(1)
            };

            return View(cityModel);
        }
    }
}