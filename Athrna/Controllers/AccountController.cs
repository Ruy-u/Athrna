using Microsoft.AspNetCore.Mvc;
using Athrna.Models;

namespace Athrna.Controllers
{
    public class AccountController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid input" });
            }

            // Here you would typically authenticate the user against your database
            // This is just a placeholder implementation
            if (model.Username == "admin" && model.Password == "password")
            {
                // Set authentication cookie or session state here
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Invalid username or password" });
        }

        public IActionResult Logout()
        {
            // Clear authentication cookie or session state here
            return RedirectToAction("Index", "Home");
        }
    }
}