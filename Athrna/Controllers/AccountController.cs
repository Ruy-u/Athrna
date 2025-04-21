using Microsoft.AspNetCore.Mvc;
using Athrna.Models;
using Athrna.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Athrna.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // GET: /Account/Register
        public async Task<IActionResult> Register()
        {
            // Get cities for the guide registration dropdown
            ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Add a small delay to prevent timing attacks
            await Task.Delay(200);

            var client = await _context.Client
                .FirstOrDefaultAsync(c => c.Username == model.Username);

            if (client == null)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View(model);
            }

            // Direct password comparison instead of hash verification
            if (client.EncryptedPassword == model.Password)
            {
                // Create claims for the client
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, client.Username),
                    new Claim(ClaimTypes.NameIdentifier, client.Id.ToString()),
                    new Claim(ClaimTypes.Email, client.Email)
                };

                // Check if client is an administrator
                var isAdmin = await _context.Administrator.AnyAsync(a => a.ClientId == client.Id);
                if (isAdmin)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Add a success log entry
                _logger.LogInformation($"User {model.Username} logged in successfully");

                // Check if user is admin and redirect accordingly
                if (isAdmin)
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Invalid username or password");
            return View(model);
        }
        // POST: /Account/LoginAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginAjax(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid form submission" });
            }

            // Add a small delay to prevent timing attacks
            await Task.Delay(200);

            var client = await _context.Client
                .FirstOrDefaultAsync(c => c.Username == model.Username);

            if (client == null)
            {
                return Json(new { success = false, message = "Invalid username or password" });
            }

            // Direct password comparison instead of hash verification
            if (client.EncryptedPassword == model.Password)
            {
                // Create claims for the client
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, client.Username),
            new Claim(ClaimTypes.NameIdentifier, client.Id.ToString()),
            new Claim(ClaimTypes.Email, client.Email)
        };

                // Check if client is an administrator
                var isAdmin = await _context.Administrator.AnyAsync(a => a.ClientId == client.Id);
                if (isAdmin)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Add a success log entry
                _logger.LogInformation($"User {model.Username} logged in successfully via AJAX");

                // Return different redirect URL based on user role
                string redirectUrl = isAdmin ? "/Admin" : "/";

                return Json(new { success = true, redirectUrl });
            }

            return Json(new { success = false, message = "Invalid username or password" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model,
            string GuideFullName, string NationalId, int? GuideCityId, string LicenseNumber, bool RegisterAsGuide = false)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            // Additional validation checks
            if (!IsValidUsername(model.Username))
            {
                ModelState.AddModelError("Username", "Username format is invalid. Use only letters, numbers, underscores and hyphens.");
                ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            // Check if username is already taken
            if (await _context.Client.AnyAsync(c => c.Username.ToLower() == model.Username.ToLower()))
            {
                ModelState.AddModelError("Username", "Username is already taken");
                ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            // Check if email is already taken
            if (await _context.Client.AnyAsync(c => c.Email.ToLower() == model.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "Email is already registered");
                ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            // Check password strength
            if (!IsStrongPassword(model.Password))
            {
                ModelState.AddModelError("Password", "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character");
                ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            // Validate guide registration fields if registering as guide
            if (RegisterAsGuide)
            {
                if (string.IsNullOrWhiteSpace(GuideFullName))
                {
                    ModelState.AddModelError("GuideFullName", "Full name is required for guide registration");
                    ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(NationalId))
                {
                    ModelState.AddModelError("NationalId", "National ID / Iqama number is required for guide registration");
                    ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
                    return View(model);
                }

                if (!GuideCityId.HasValue)
                {
                    ModelState.AddModelError("GuideCityId", "Please select your primary city");
                    ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(LicenseNumber))
                {
                    ModelState.AddModelError("LicenseNumber", "Tourism license number is required for guide registration");
                    ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
                    return View(model);
                }

                // Basic license number format validation (example: TR-1234)
                if (!Regex.IsMatch(LicenseNumber, @"^TR-\d{4}$"))
                {
                    ModelState.AddModelError("LicenseNumber", "Invalid license number format. It should be in the format: TR-XXXX (where X is a digit)");
                    ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
                    return View(model);
                }

                // Check if city exists
                var cityExists = await _context.City.AnyAsync(c => c.Id == GuideCityId);
                if (!cityExists)
                {
                    ModelState.AddModelError("GuideCityId", "Selected city is invalid");
                    ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
                    return View(model);
                }

                // Verify license number against "valid" licenses
                // In a real application, this would check against a real database of valid licenses
                // For demo purposes, we'll use a fixed set of valid numbers
                string[] validLicenses = { "TR-1234", "TR-5678", "TR-9999" };
                if (!validLicenses.Contains(LicenseNumber))
                {
                    ModelState.AddModelError("LicenseNumber", "The license number is not recognized. Please verify and try again.");
                    ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
                    return View(model);
                }
            }

            // Create client account for regular registration
            var client = new Client
            {
                Username = model.Username,
                Email = model.Email,
                // Store the password directly without hashing
                EncryptedPassword = model.Password
            };

            _context.Client.Add(client);
            await _context.SaveChangesAsync();

            // If registering as a guide, create a guide application
            if (RegisterAsGuide)
            {
                var guideApplication = new GuideApplication
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = model.Password,
                    CityId = GuideCityId.Value,
                    FullName = GuideFullName,
                    NationalId = NationalId,
                    LicenseNumber = LicenseNumber,
                    Status = ApplicationStatus.Pending,
                    SubmissionDate = DateTime.UtcNow
                };

                _context.GuideApplication.Add(guideApplication);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registration successful! Your guide application has been submitted and is pending review.";
            }
            else
            {
                TempData["SuccessMessage"] = "Registration successful! You can now log in.";
            }

            return RedirectToAction("Login");
        }

        // Add this new method for password strength validation
        private bool IsStrongPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            // Check length (at least 8 characters)
            if (password.Length < 8)
                return false;

            // Check for at least one lowercase letter
            if (!Regex.IsMatch(password, "[a-z]"))
                return false;

            // Check for at least one uppercase letter
            if (!Regex.IsMatch(password, "[A-Z]"))
                return false;

            // Check for at least one digit
            if (!Regex.IsMatch(password, "[0-9]"))
                return false;

            // Check for at least one special character
            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
                return false;

            return true;
        }

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity.Name;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User {Username} logged out", username);
            return RedirectToAction("Index", "Home");
        }

        // Rest of existing methods...
        private bool IsValidUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return false;

            // Check length (3-50 characters)
            if (username.Length < 3 || username.Length > 50)
                return false;

            // Check characters (alphanumeric, underscore, hyphen)
            Regex regex = new Regex(@"^[a-zA-Z0-9_-]+$");
            return regex.IsMatch(username);
        }

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(PasswordResetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = await _context.Client.FirstOrDefaultAsync(c => c.Email == model.Email);
            if (client == null)
            {
                // Don't reveal that the user does not exist
                // Instead, show a success message as if the reset email was sent
                TempData["SuccessMessage"] = "If your email is registered, you will receive a password reset link shortly.";
                return RedirectToAction("Login");
            }

            // Generate a reset code (in a real app, this would be a secure token)
            string resetCode = Guid.NewGuid().ToString();

            // Store the reset information in TempData (in a real app, you'd store this in the database with an expiration)
            TempData["ResetCode"] = resetCode;
            TempData["ResetEmail"] = model.Email;

            // In a real application, you would:
            // 1. Store this code in the database with an expiration time
            // 2. Send an email with a link containing this code

            // For this demo, we'll show a success message and redirect to the reset page
            TempData["SuccessMessage"] = "Password reset instructions have been sent to your email.";

            // Instead of redirecting to the login page, in a real app you would send an email
            // For demo purposes, we'll redirect directly to the reset page
            return RedirectToAction("ResetPassword", new { email = model.Email, code = resetCode });
        }

        // GET: /Account/ResetPassword
        public IActionResult ResetPassword(string email, string code)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
            {
                TempData["ErrorMessage"] = "Invalid password reset link.";
                return RedirectToAction("Login");
            }

            var model = new PasswordResetViewModel
            {
                Email = email,
                ResetCode = code
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(PasswordResetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify reset code (in a real app, you'd verify against a stored token)
            string savedCode = TempData["ResetCode"]?.ToString();
            string savedEmail = TempData["ResetEmail"]?.ToString();

            if (model.ResetCode != savedCode || model.Email != savedEmail)
            {
                ModelState.AddModelError("", "Invalid or expired password reset link.");
                return View(model);
            }

            var client = await _context.Client.FirstOrDefaultAsync(c => c.Email == model.Email);
            if (client == null)
            {
                ModelState.AddModelError("", "Invalid email address.");
                return View(model);
            }

            // Update password
            client.EncryptedPassword = model.NewPassword; // In a real app, hash this!
            _context.Update(client);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your password has been reset successfully. You can now log in with your new password.";
            return RedirectToAction("Login");
        }
    }
}