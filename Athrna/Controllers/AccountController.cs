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
        private readonly IPasswordHasher<Client> _passwordHasher;

        public AccountController(ApplicationDbContext context, IPasswordHasher<Client> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
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

            var result = _passwordHasher.VerifyHashedPassword(client, client.EncryptedPassword, model.Password);

            if (result == PasswordVerificationResult.Success)
            {
                // Create claims for the client
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, client.Username),
                new Claim(ClaimTypes.NameIdentifier, client.Id.ToString()),
                new Claim(ClaimTypes.Email, client.Email)
            };

                // Check if client is an administrator
                var isAdmin = await _context.Administrators.AnyAsync(a => a.ClientId == client.Id);
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

                return RedirectToAction("Index", "Home");
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
                EncryptedPassword = _passwordHasher.HashPassword(null, model.Password)
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
    }
}