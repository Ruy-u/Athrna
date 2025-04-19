using Microsoft.AspNetCore.Mvc;
using Athrna.Models;
using Athrna.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
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
        public IActionResult Register()
        {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Additional validation checks
            if (!IsValidUsername(model.Username))
            {
                ModelState.AddModelError("Username", "Username format is invalid. Use only letters, numbers, underscores and hyphens.");
                return View(model);
            }

            // Check if username is already taken
            if (await _context.Client.AnyAsync(c => c.Username.ToLower() == model.Username.ToLower()))
            {
                ModelState.AddModelError("Username", "Username is already taken");
                return View(model);
            }

            // Check if email is already taken
            if (await _context.Client.AnyAsync(c => c.Email.ToLower() == model.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "Email is already registered");
                return View(model);
            }

            // Check password strength
            if (!IsStrongPassword(model.Password))
            {
                ModelState.AddModelError("Password", "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character");
                return View(model);
            }

            var client = new Client
            {
                Username = model.Username,
                Email = model.Email,
                // Store the password directly without hashing
                EncryptedPassword = model.Password
            };

            _context.Client.Add(client);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful! You can now log in.";
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
    }
}