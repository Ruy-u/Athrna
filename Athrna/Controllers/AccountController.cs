using Microsoft.AspNetCore.Mvc;
using Athrna.Models;
using Athrna.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Athrna.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
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
                    IsPersistent = model.RememberMe
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

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

            // Check if username is already taken
            if (await _context.Client.AnyAsync(c => c.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username is already taken");
                return View(model);
            }

            // Check if email is already taken
            if (await _context.Client.AnyAsync(c => c.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email is already registered");
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

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
    }
}