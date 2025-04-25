using Microsoft.Extensions.Hosting;

namespace Athrna.Services
{
    public static class EmailTemplateSetupHelper
    {
        public static async Task EnsureEmailTemplateExists(IHostEnvironment environment)
        {
            string templateDir = Path.Combine(environment.ContentRootPath, "EmailTemplates");
            string templatePath = Path.Combine(templateDir, "PasswordReset.html");

            // Create directory if it doesn't exist
            if (!Directory.Exists(templateDir))
            {
                Directory.CreateDirectory(templateDir);
            }

            // Only create the template file if it doesn't already exist
            if (!File.Exists(templatePath))
            {
                string template = GetPasswordResetEmailTemplate();
                await File.WriteAllTextAsync(templatePath, template);
            }
        }

        private static string GetPasswordResetEmailTemplate()
        {
            return @"<!DOCTYPE html>
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
        }
    }
}