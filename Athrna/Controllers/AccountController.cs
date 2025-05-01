using Microsoft.AspNetCore.Mvc;
using Athrna.Models;
using Athrna.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Athrna.Services;

namespace Athrna.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailService _emailService;
        private readonly PasswordResetTokenService _tokenService;
        private readonly IHostEnvironment _hostEnvironment;

        public AccountController(
            ApplicationDbContext context,
            ILogger<AccountController> logger,
            IEmailService emailService,
            PasswordResetTokenService tokenService,
            IHostEnvironment hostEnvironment)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _tokenService = tokenService;
            _hostEnvironment = hostEnvironment;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // Update the login method to include RoleLevel in the claims

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

            // Check if the account is banned
            if (client.IsBanned)
            {
                _logger.LogWarning("Login attempt for banned account: {Username}", model.Username);
                ModelState.AddModelError("", "Your account has been suspended. Please contact support for assistance.");
                return View(model);
            }

            // Direct password comparison instead of hash verification
            if (client.EncryptedPassword == model.Password)
            {
                // Check if email is verified
                if (!client.IsEmailVerified)
                {
                    _logger.LogWarning("Login attempt with unverified email for user: {Username}", model.Username);
                    ModelState.AddModelError("", "Please verify your email address before logging in. " +
                        $"<a href='{Url.Action("ResendVerificationEmail", new { email = client.Email })}'>Resend verification email</a>");
                    return View(model);
                }

                // Create claims for the client
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, client.Username),
            new Claim(ClaimTypes.NameIdentifier, client.Id.ToString()),
            new Claim(ClaimTypes.Email, client.Email)
        };

                // Check if client is an administrator
                var admin = await _context.Administrator.FirstOrDefaultAsync(a => a.ClientId == client.Id);
                if (admin != null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Administrator"));

                    // Add admin role level as a claim
                    claims.Add(new Claim("AdminRoleLevel", admin.RoleLevel.ToString()));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Set up auth properties with proper expiration based on Remember Me
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    // If RememberMe is true, use a 30-day expiration, otherwise use the default (2 hours from configuration)
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null,
                    // Only use sliding expiration for "Remember Me" (refreshes the cookie on activity)
                    AllowRefresh = model.RememberMe
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Add a success log entry
                _logger.LogInformation($"User {model.Username} logged in successfully. Remember Me: {model.RememberMe}");

                if (admin != null)
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    // Check if user is a guide
                    var guide = await _context.Guide.FirstOrDefaultAsync(g => g.Email == client.Email);
                    if (guide != null)
                    {
                        // Add Guide role claim
                        claims.Add(new Claim(ClaimTypes.Role, "Guide"));

                        return RedirectToAction("Index", "GuideDashboard");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
            }

            ModelState.AddModelError("", "Invalid username or password");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginAjax(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid form submission" });
            }

            await Task.Delay(200); // Prevent timing attacks

            var client = await _context.Client.FirstOrDefaultAsync(c => c.Username == model.Username);
            if (client == null)
            {
                return Json(new { success = false, message = "Invalid username or password" });
            }

            if (client.IsBanned)
            {
                _logger.LogWarning("AJAX login attempt for banned account: {Username}", model.Username);
                return Json(new
                {
                    success = false,
                    message = "Your account has been suspended. Please contact support for assistance.",
                    isBanned = true
                });
            }

            if (client.EncryptedPassword == model.Password)
            {
                if (!client.IsEmailVerified)
                {
                    _logger.LogWarning("AJAX login attempt with unverified email for user: {Username}", model.Username);
                    var verificationUrl = Url.Action("ResendVerificationEmail", "Account", new { email = client.Email });
                    return Json(new
                    {
                        success = false,
                        message = "Please verify your email address before logging in.",
                        requireVerification = true,
                        email = client.Email,
                        verificationUrl
                    });
                }

                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, client.Username),
            new Claim(ClaimTypes.NameIdentifier, client.Id.ToString()),
            new Claim(ClaimTypes.Email, client.Email)
        };

                var isAdmin = false;
                var isGuide = false;
                string redirectUrl = "/";

                // Check if user is an admin
                var admin = await _context.Administrator.FirstOrDefaultAsync(a => a.ClientId == client.Id);
                if (admin != null)
                {
                    isAdmin = true;
                    claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
                    claims.Add(new Claim("AdminRoleLevel", admin.RoleLevel.ToString()));
                    _logger.LogInformation("Admin user {Username} logged in with role level {RoleLevel}", client.Username, admin.RoleLevel);
                }

                // Check if user is a guide
                isGuide = await _context.Guide.AnyAsync(g => g.Email == client.Email);
                if (isGuide)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Guide"));
                }

                // Determine redirect URL
                if (isAdmin)
                    redirectUrl = "/Admin";
                else if (isGuide)
                    redirectUrl = "/GuideDashboard";

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null,
                    AllowRefresh = model.RememberMe
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User {Username} logged in successfully via AJAX. Remember Me: {RememberMe}", model.Username, model.RememberMe);

                return Json(new
                {
                    success = true,
                    redirectUrl,
                    isAdmin,
                    isGuide,
                    adminRoleLevel = admin?.RoleLevel ?? 0
                });
            }

            return Json(new { success = false, message = "Invalid username or password" });
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
        public async Task<IActionResult> Register(RegisterViewModel model,
            string GuideFullName, string NationalId, int? GuideCityId, string LicenseNumber, bool RegisterAsGuide = false)
        {
            try
            {
                // Log registration attempt
                _logger.LogInformation("Registration attempt for username: {Username}, Email: {Email}, RegisterAsGuide: {RegisterAsGuide}",
                    model.Username, model.Email, RegisterAsGuide);

                // Get cities for the dropdown
                ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();

                // Clear ModelState errors for guide fields if not registering as guide
                if (!RegisterAsGuide)
                {
                    ModelState.Remove("GuideFullName");
                    ModelState.Remove("NationalId");
                    ModelState.Remove("GuideCityId");
                    ModelState.Remove("LicenseNumber");
                }

                if (!ModelState.IsValid)
                {
                    // Log validation errors
                    var validationErrors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                    _logger.LogWarning("Model validation failed: {Errors}", validationErrors);

                    return View(model);
                }

                // Additional validation checks
                if (!IsValidUsername(model.Username))
                {
                    _logger.LogWarning("Invalid username format: {Username}", model.Username);
                    ModelState.AddModelError("Username", "Username format is invalid. Use only letters, numbers, underscores and hyphens.");
                    return View(model);
                }

                // Check if username is already taken
                if (await _context.Client.AnyAsync(c => c.Username.ToLower() == model.Username.ToLower()))
                {
                    _logger.LogWarning("Username already taken: {Username}", model.Username);
                    ModelState.AddModelError("Username", "Username is already taken");
                    return View(model);
                }

                // Check if email is already taken
                if (await _context.Client.AnyAsync(c => c.Email.ToLower() == model.Email.ToLower()))
                {
                    _logger.LogWarning("Email already registered: {Email}", model.Email);
                    ModelState.AddModelError("Email", "Email is already registered");
                    return View(model);
                }

                // Check password strength
                if (!IsStrongPassword(model.Password))
                {
                    _logger.LogWarning("Password not strong enough for user: {Username}", model.Username);
                    ModelState.AddModelError("Password", "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character");
                    return View(model);
                }

                // Validate guide registration fields ONLY if registering as guide
                if (RegisterAsGuide)
                {
                    _logger.LogInformation("Attempting guide registration for: {Username}", model.Username);

                    // Guide validation code...
                    // (Keeping the existing validation code here)
                    if (string.IsNullOrWhiteSpace(GuideFullName))
                    {
                        ModelState.AddModelError("GuideFullName", "Full name is required for guide registration");
                        return View(model);
                    }

                    if (string.IsNullOrWhiteSpace(NationalId))
                    {
                        ModelState.AddModelError("NationalId", "National ID / Iqama number is required for guide registration");
                        return View(model);
                    }

                    if (!GuideCityId.HasValue)
                    {
                        ModelState.AddModelError("GuideCityId", "Please select your primary city");
                        return View(model);
                    }

                    if (string.IsNullOrWhiteSpace(LicenseNumber))
                    {
                        ModelState.AddModelError("LicenseNumber", "Tourism license number is required for guide registration");
                        return View(model);
                    }

                    // Basic license number format validation (example: TR-1234)
                    if (!Regex.IsMatch(LicenseNumber, @"^TR-\d{4}$"))
                    {
                        ModelState.AddModelError("LicenseNumber", "Invalid license number format. It should be in the format: TR-XXXX (where X is a digit)");
                        return View(model);
                    }

                    // Check if city exists
                    var cityExists = await _context.City.AnyAsync(c => c.Id == GuideCityId);
                    if (!cityExists)
                    {
                        ModelState.AddModelError("GuideCityId", "Selected city is invalid");
                        return View(model);
                    }

                    // Verify license number against "valid" licenses
                    string[] validLicenses = { "TR-1234", "TR-5678", "TR-9999" };
                    if (!validLicenses.Contains(LicenseNumber))
                    {
                        ModelState.AddModelError("LicenseNumber", "The license number is not recognized. Please verify and try again.");
                        return View(model);
                    }
                }

                // All validation passed, create client account
                _logger.LogInformation("Creating new client account for: {Username}", model.Username);

                var client = new Client
                {
                    Username = model.Username,
                    Email = model.Email,
                    // Store the password directly without hashing
                    EncryptedPassword = model.Password,
                    // Set email as not verified initially
                    IsEmailVerified = false
                };

                _context.Client.Add(client);
                var saveResult = await _context.SaveChangesAsync();

                _logger.LogInformation("Client saved to database. Result: {Result}", saveResult);

                // If registering as a guide, create a guide application
                if (RegisterAsGuide)
                {
                    _logger.LogInformation("Creating guide application for: {Username}", model.Username);

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
                        SubmissionDate = DateTime.UtcNow,
                        RejectionReason = "" // Initialize with empty string to prevent null error
                    };

                    _context.GuideApplication.Add(guideApplication);
                    await _context.SaveChangesAsync();

                    ViewBag.SuccessMessage = "Registration successful! Your guide application has been submitted and is pending review. Please check your email to verify your account.";
                    _logger.LogInformation("Guide application created successfully for: {Username}", model.Username);
                }
                else
                {
                    ViewBag.SuccessMessage = "Registration successful! Please check your email to verify your account.";
                    _logger.LogInformation("Standard registration completed for: {Username}", model.Username);
                }

                // Send verification email
                try
                {
                    await SendVerificationEmail(client);
                    _logger.LogInformation("Verification email sent to {Email} for new registration", client.Email);
                }
                catch (Exception ex)
                {
                    // Log error but don't prevent registration completion
                    _logger.LogError(ex, "Failed to send verification email to {Email}", client.Email);
                    ViewBag.EmailWarning = "Registration was successful, but we encountered an issue sending the verification email. Please contact support if you don't receive an email within 24 hours.";
                }

                // Clear form fields after successful registration
                ModelState.Clear();
                return View(new RegisterViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration process for user: {Username}", model.Username);
                ModelState.AddModelError("", "An unexpected error occurred during registration. Please try again.");
                return View(model);
            }
        }

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

            // Clear authentication cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Add script to clear localStorage on the client side
            TempData["ClearLocalStorage"] = true;

            _logger.LogInformation("User {Username} logged out", username);
            return RedirectToAction("Index", "Home");
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

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Log validation errors
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation errors: {Errors}", errors);
                return View(model);
            }

            // Always show success message regardless of whether the email exists
            // This is a security measure to prevent user enumeration
            TempData["InfoMessage"] = "If your email is registered, you will receive a password reset link shortly.";
            _logger.LogInformation("Setting TempData InfoMessage about password reset");

            var client = await _context.Client.FirstOrDefaultAsync(c => c.Email == model.Email);
            if (client == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", model.Email);
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            _logger.LogInformation("Found client with ID {ClientId} for email {Email}", client.Id, model.Email);

            try
            {
                // Generate reset token
                _logger.LogInformation("Generating reset token for {Email}", model.Email);
                string token = _tokenService.GenerateToken(model.Email);

                // Create reset URL
                var resetUrl = Url.Action("ResetPassword", "Account",
                    new { email = model.Email, token = token },
                    protocol: HttpContext.Request.Scheme);
                _logger.LogInformation("Generated reset URL: {ResetUrl}", resetUrl);

                // Read email template
                string templatePath = Path.Combine(_hostEnvironment.ContentRootPath, "EmailTemplates", "PasswordReset.html");
                _logger.LogInformation("Looking for template at: {TemplatePath}", templatePath);

                string emailTemplate;

                if (System.IO.File.Exists(templatePath))
                {
                    _logger.LogInformation("Email template found, reading content");
                    emailTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
                }
                else
                {
                    _logger.LogWarning("Email template not found at {TemplatePath}, using fallback template", templatePath);
                    // Fallback to a simple email if template doesn't exist
                    emailTemplate = GetPasswordResetEmailTemplate();
                }

                // Replace placeholder with actual reset link
                _logger.LogInformation("Replacing placeholders in email template");
                string emailBody = emailTemplate.Replace("{ResetLink}", resetUrl);

                // Send email
                _logger.LogInformation("Attempting to send email to {Email}", model.Email);
                bool emailSent = await _emailService.SendEmailAsync(
                    model.Email,
                    "Athrna - Password Reset Request",
                    emailBody);

                if (!emailSent)
                {
                    _logger.LogError("Failed to send password reset email to {Email}", model.Email);
                    ModelState.AddModelError("", "Failed to send password reset email. Please try again later.");
                    return View(model);
                }

                _logger.LogInformation("Password reset link sent successfully to {Email}", model.Email);
                return RedirectToAction("ForgotPasswordConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing password reset for {Email}: {ErrorMessage}", model.Email, ex.Message);
                ModelState.AddModelError("", "An error occurred. Please try again later.");
                return View(model);
            }
        }

        // GET: /Account/ForgotPasswordConfirmation
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/ResetPassword
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Error", "Home");
            }

            var model = new PasswordResetViewModel
            {
                Email = email,
                Token = token
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

            // Log the reset attempt
            _logger.LogInformation("Password reset attempt for email: {Email}", model.Email);

            // Validate token
            var (isValid, email) = _tokenService.ValidateToken(model.Token);

            if (!isValid || email != model.Email)
            {
                _logger.LogWarning("Invalid or expired password reset token for {Email}", model.Email);
                ModelState.AddModelError("", "Invalid or expired password reset link. Please request a new one.");
                return View(model);
            }

            var client = await _context.Client.FirstOrDefaultAsync(c => c.Email == model.Email);
            if (client == null)
            {
                _logger.LogWarning("Password reset attempted for non-existent email: {Email}", model.Email);
                ModelState.AddModelError("", "Invalid email address.");
                return View(model);
            }

            // Validate password strength
            if (!IsStrongPassword(model.NewPassword))
            {
                ModelState.AddModelError("NewPassword", "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character");
                return View(model);
            }

            try
            {
                // Update password
                client.EncryptedPassword = model.NewPassword; // In a real app, this would be hashed!
                _context.Update(client);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password successfully reset for {Email}", model.Email);
                TempData["SuccessMessage"] = "Your password has been reset successfully. You can now log in with your new password.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred while resetting your password. Please try again later.");
                return View(model);
            }
        }

        // Utility methods
        private string GetPasswordResetEmailTemplate()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Password Reset Request</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 5px; padding: 20px; }
        .button { display: inline-block; background-color: #1a3b29; color: white; text-decoration: none; padding: 10px 20px; border-radius: 5px; }
    </style>
</head>
<body>
    <div class='container'>
        <h2>Password Reset Request</h2>
        <p>Hello,</p>
        <p>We received a request to reset your password for your Athrna account. To reset your password, please click the link below:</p>
        <p><a href='{ResetLink}' class='button'>Reset Your Password</a></p>
        <p>If the button above doesn't work, copy and paste this URL into your browser:</p>
        <p>{ResetLink}</p>
        <p>This link will expire in 24 hours. If you did not request a password reset, please ignore this email.</p>
        <p>Thank you,<br>The Athrna Team</p>
    </div>
</body>
</html>";
        }

        private async Task SendVerificationEmail(Client client)
        {
            try
            {
                // Generate verification token
                string token = _tokenService.GenerateToken(client.Email);

                // Create verification URL
                var verificationUrl = Url.Action("VerifyEmail", "Account",
                    new { email = client.Email, token = token },
                    protocol: HttpContext.Request.Scheme);

                // Read email template
                string templatePath = Path.Combine(_hostEnvironment.ContentRootPath, "EmailTemplates", "EmailVerification.html");
                string emailTemplate;

                if (System.IO.File.Exists(templatePath))
                {
                    emailTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
                }
                else
                {
                    // Fallback to a generated template
                    emailTemplate = EmailVerificationHelper.GenerateVerificationEmailTemplate("{VerificationLink}", "{Username}", "{AdditionalMessage}");
                }

                // Check if user is a guide applicant to add custom message
                string additionalMessage = "";
                var guideApplication = await _context.GuideApplication
                    .FirstOrDefaultAsync(g => g.Email == client.Email);

                if (guideApplication != null)
                {
                    additionalMessage = "<p><strong>Your guide application has been submitted and is pending review.</strong></p>" +
                        "<p>Please note that your guide status will be reviewed by our administrators. " +
                        "You will receive a separate email notification when your application is approved or if additional information is needed.</p>";
                }

                // Replace placeholders with actual values
                emailTemplate = emailTemplate
                    .Replace("{VerificationLink}", verificationUrl)
                    .Replace("{Username}", client.Username)
                    .Replace("{AdditionalMessage}", additionalMessage);

                // Send email
                bool emailSent = await _emailService.SendEmailAsync(
                    client.Email,
                    "Athrna - Verify Your Email Address",
                    emailTemplate);

                if (!emailSent)
                {
                    _logger.LogError("Failed to send verification email to {Email}", client.Email);
                    throw new Exception("Failed to send verification email");
                }

                _logger.LogInformation("Verification email sent to {Email}", client.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email to {Email}", client.Email);
                throw; // Re-throw to let caller handle it
            }
        }

        // GET: /Account/VerifyEmail
        public async Task<IActionResult> VerifyEmail(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Empty email or token in VerifyEmail request");
                return RedirectToAction("Error", "Home", new { message = "Invalid verification link." });
            }

            // Validate token
            var (isValid, tokenEmail) = _tokenService.ValidateToken(token);

            if (!isValid || tokenEmail != email)
            {
                _logger.LogWarning("Invalid or expired email verification token for {Email}", email);
                ViewBag.ErrorMessage = "Invalid or expired verification link. Please request a new one.";
                return View("VerificationFailed");
            }

            var client = await _context.Client.FirstOrDefaultAsync(c => c.Email == email);
            if (client == null)
            {
                _logger.LogWarning("Email verification attempted for non-existent email: {Email}", email);
                ViewBag.ErrorMessage = "Account not found. Please register again.";
                return View("VerificationFailed");
            }

            // Check if already verified
            if (client.IsEmailVerified)
            {
                _logger.LogInformation("Email already verified for {Email}", email);
                ViewBag.Message = "Your email has already been verified. You can log in to your account.";
                return View("EmailVerificationSuccess");
            }

            // Mark user as verified in the database
            client.IsEmailVerified = true;
            _context.Update(client);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email successfully verified for {Email}", email);
            return View("EmailVerificationSuccess");
        }

        // GET: /Account/RegisterConfirmation
        public IActionResult RegisterConfirmation(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index", "Home");
            }

            return View(email);
        }

        // GET: /Account/ResendVerificationEmail
        [HttpGet]
        [ActionName("ResendVerificationEmail")]
        public IActionResult ResendVerificationEmail_Get(string email = null)
        {
            ViewBag.Email = email;
            return View("ResendVerificationEmail");
        }

        // POST: /Account/ResendVerificationEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("ResendVerificationEmail")]
        public async Task<IActionResult> ResendVerificationEmail_Post(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Email is required");
                return View("ResendVerificationEmail");
            }

            TempData["InfoMessage"] = "If your email is registered, you will receive a verification link shortly.";

            var client = await _context.Client.FirstOrDefaultAsync(c => c.Email == email);
            if (client == null)
            {
                _logger.LogWarning("Verification email requested for non-existent email: {Email}", email);
                return RedirectToAction("Login");
            }

            if (client.IsEmailVerified)
            {
                TempData["InfoMessage"] = "Your email is already verified. You can login.";
                return RedirectToAction("Login");
            }

            try
            {
                await SendVerificationEmail(client);
                _logger.LogInformation("Verification email resent to {Email}", client.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend verification email to {Email}", client.Email);
            }

            return RedirectToAction("Login");
        }
    }
}

