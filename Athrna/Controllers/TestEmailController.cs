using Microsoft.AspNetCore.Mvc;
using Athrna.Services;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Athrna.Controllers
{
    public class TestEmailController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TestEmailController> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        public TestEmailController(
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<TestEmailController> logger,
            IHostEnvironment hostEnvironment)
        {
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        // GET: /TestEmail
        public IActionResult Index()
        {
            // Show current email configuration (masked password)
            var emailConfig = new
            {
                SmtpServer = _configuration["Email:SmtpServer"] ?? "Not configured",
                SmtpPort = _configuration.GetValue<int?>("Email:SmtpPort") ?? 0,
                Username = _configuration["Email:Username"] ?? "Not configured",
                PasswordConfigured = !string.IsNullOrEmpty(_configuration["Email:Password"]),
                FromEmail = _configuration["Email:From"] ?? "Not configured"
            };

            ViewBag.EmailConfig = emailConfig;

            // Check if template directory exists
            string templateDir = Path.Combine(_hostEnvironment.ContentRootPath, "EmailTemplates");
            ViewBag.TemplateDirectoryExists = Directory.Exists(templateDir);

            // Check if template files exist
            string passwordResetPath = Path.Combine(templateDir, "PasswordReset.html");
            string verificationPath = Path.Combine(templateDir, "EmailVerification.html");

            ViewBag.PasswordResetTemplateExists = System.IO.File.Exists(passwordResetPath);
            ViewBag.VerificationTemplateExists = System.IO.File.Exists(verificationPath);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendTestEmail(string recipientEmail)
        {
            if (string.IsNullOrEmpty(recipientEmail))
            {
                TempData["ErrorMessage"] = "Please provide a recipient email address.";
                return RedirectToAction("Index");
            }

            try
            {
                // Build a test email
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine("<!DOCTYPE html>");
                emailBody.AppendLine("<html>");
                emailBody.AppendLine("<head>");
                emailBody.AppendLine("    <meta charset=\"UTF-8\">");
                emailBody.AppendLine("    <title>Test Email from Athrna</title>");
                emailBody.AppendLine("</head>");
                emailBody.AppendLine("<body>");
                emailBody.AppendLine("    <div style=\"font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;\">");
                emailBody.AppendLine("        <h1 style=\"color: #1a3b29;\">Test Email from Athrna</h1>");
                emailBody.AppendLine("        <p>This is a test email to verify your SMTP configuration.</p>");
                emailBody.AppendLine("        <p>If you're seeing this email, your email service is configured correctly!</p>");
                emailBody.AppendLine("        <p>Date and time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</p>");
                emailBody.AppendLine("    </div>");
                emailBody.AppendLine("</body>");
                emailBody.AppendLine("</html>");

                // Send the test email
                _logger.LogInformation("Sending test email to {Email}", recipientEmail);
                bool result = await _emailService.SendEmailAsync(
                    recipientEmail,
                    "Athrna - Test Email",
                    emailBody.ToString());

                if (result)
                {
                    TempData["SuccessMessage"] = "Test email sent successfully! Check your inbox.";
                    _logger.LogInformation("Test email sent successfully to {Email}", recipientEmail);
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to send test email. Check the logs for details.";
                    _logger.LogError("Test email sending failed to {Email}", recipientEmail);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error sending test email: " + ex.Message;
                _logger.LogError(ex, "Exception occurred while sending test email to {Email}", recipientEmail);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult CreateTemplates()
        {
            try
            {
                // Ensure the template directory exists
                string templateDir = Path.Combine(_hostEnvironment.ContentRootPath, "EmailTemplates");
                if (!Directory.Exists(templateDir))
                {
                    Directory.CreateDirectory(templateDir);
                    _logger.LogInformation("Created template directory at {Path}", templateDir);
                }

                // Create password reset template if it doesn't exist
                string passwordResetPath = Path.Combine(templateDir, "PasswordReset.html");
                if (!System.IO.File.Exists(passwordResetPath))
                {
                    string passwordResetTemplate = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Password Reset Request</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
        }
        .container {
            background: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 5px;
            padding: 20px;
            margin: 20px 0;
        }
        .header {
            background-color: #1a3b29;
            color: white;
            padding: 15px;
            text-align: center;
            border-radius: 5px 5px 0 0;
            margin-top: -20px;
            margin-left: -20px;
            margin-right: -20px;
        }
        .logo {
            font-size: 24px;
            font-weight: bold;
        }
        .button {
            display: inline-block;
            background-color: #1a3b29;
            color: white;
            text-decoration: none;
            padding: 10px 20px;
            border-radius: 5px;
            margin: 20px 0;
        }
        .footer {
            margin-top: 30px;
            font-size: 12px;
            color: #6c757d;
            text-align: center;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <div class=""logo"">Athrna</div>
        </div>
        
        <h2>Password Reset Request</h2>
        
        <p>Hello,</p>
        
        <p>We received a request to reset your password for your Athrna account. To reset your password, please click the button below:</p>
        
        <div style=""text-align: center;"">
            <a href=""{ResetLink}"" class=""button"">Reset Your Password</a>
        </div>
        
        <p>If the button above doesn't work, copy and paste this URL into your browser:</p>
        <p style=""word-break: break-all;"">{ResetLink}</p>
        
        <p>This link will expire in 24 hours. If you did not request a password reset, please ignore this email or contact support if you have concerns.</p>
        
        <p>Thank you,<br>The Athrna Team</p>
        
        <div class=""footer"">
            <p>This is an automated message, please do not reply to this email.</p>
            <p>&copy; 2025 Athrna - Saudi Historical Sites. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
                    System.IO.File.WriteAllText(passwordResetPath, passwordResetTemplate);
                    _logger.LogInformation("Created password reset template at {Path}", passwordResetPath);
                }

                // Create email verification template if it doesn't exist
                string verificationPath = Path.Combine(templateDir, "EmailVerification.html");
                if (!System.IO.File.Exists(verificationPath))
                {
                    string verificationTemplate = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Verify Your Athrna Account</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
        }
        .container {
            background: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 5px;
            padding: 20px;
            margin: 20px 0;
        }
        .header {
            background-color: #1a3b29;
            color: white;
            padding: 15px;
            text-align: center;
            border-radius: 5px 5px 0 0;
            margin-top: -20px;
            margin-left: -20px;
            margin-right: -20px;
        }
        .logo {
            font-size: 24px;
            font-weight: bold;
        }
        .button {
            display: inline-block;
            background-color: #1a3b29;
            color: white;
            text-decoration: none;
            padding: 10px 20px;
            border-radius: 5px;
            margin: 20px 0;
        }
        .footer {
            margin-top: 30px;
            font-size: 12px;
            color: #6c757d;
            text-align: center;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <div class=""logo"">Athrna</div>
        </div>
        
        <h2>Welcome to Athrna</h2>
        
        <p>Hello {Username},</p>
        
        <p>Thank you for registering with Athrna. To complete your registration and verify your email address, please click the button below:</p>
        
        <div style=""text-align: center;"">
            <a href=""{VerificationLink}"" class=""button"">Verify Your Email</a>
        </div>
        
        <p>If the button above doesn't work, copy and paste this URL into your browser:</p>
        <p style=""word-break: break-all;"">{VerificationLink}</p>
        
        <p>This link will expire in 24 hours. If you did not create an account with Athrna, please ignore this email.</p>
        
        <p>Thank you,<br>The Athrna Team</p>
        
        <div class=""footer"">
            <p>This is an automated message, please do not reply to this email.</p>
            <p>&copy; 2025 Athrna - Saudi Historical Sites. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
                    System.IO.File.WriteAllText(verificationPath, verificationTemplate);
                    _logger.LogInformation("Created email verification template at {Path}", verificationPath);
                }

                TempData["SuccessMessage"] = "Email templates created successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating templates: " + ex.Message;
                _logger.LogError(ex, "Exception occurred while creating email templates");
            }

            return RedirectToAction("Index");
        }
    }
}