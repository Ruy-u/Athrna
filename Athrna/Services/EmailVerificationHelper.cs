using Athrna.Models;
using System.Text;

namespace Athrna.Services
{
    public static class EmailVerificationHelper
    {
        public static string GenerateVerificationEmailTemplate(string verificationLink, string username, string additionalMessage = "")
        {
            StringBuilder template = new StringBuilder();

            template.AppendLine("<!DOCTYPE html>");
            template.AppendLine("<html>");
            template.AppendLine("<head>");
            template.AppendLine("    <meta charset=\"UTF-8\">");
            template.AppendLine("    <title>Verify Your Athrna Account</title>");
            template.AppendLine("    <style>");
            template.AppendLine("        body {");
            template.AppendLine("            font-family: Arial, sans-serif;");
            template.AppendLine("            line-height: 1.6;");
            template.AppendLine("            color: #333;");
            template.AppendLine("            max-width: 600px;");
            template.AppendLine("            margin: 0 auto;");
            template.AppendLine("        }");
            template.AppendLine("        .container {");
            template.AppendLine("            background: #f8f9fa;");
            template.AppendLine("            border: 1px solid #dee2e6;");
            template.AppendLine("            border-radius: 5px;");
            template.AppendLine("            padding: 20px;");
            template.AppendLine("            margin: 20px 0;");
            template.AppendLine("        }");
            template.AppendLine("        .header {");
            template.AppendLine("            background-color: #1a3b29;");
            template.AppendLine("            color: white;");
            template.AppendLine("            padding: 15px;");
            template.AppendLine("            text-align: center;");
            template.AppendLine("            border-radius: 5px 5px 0 0;");
            template.AppendLine("            margin-top: -20px;");
            template.AppendLine("            margin-left: -20px;");
            template.AppendLine("            margin-right: -20px;");
            template.AppendLine("        }");
            template.AppendLine("        .logo {");
            template.AppendLine("            font-size: 24px;");
            template.AppendLine("            font-weight: bold;");
            template.AppendLine("        }");
            template.AppendLine("        .button {");
            template.AppendLine("            display: inline-block;");
            template.AppendLine("            background-color: #1a3b29;");
            template.AppendLine("            color: white;");
            template.AppendLine("            text-decoration: none;");
            template.AppendLine("            padding: 10px 20px;");
            template.AppendLine("            border-radius: 5px;");
            template.AppendLine("            margin: 20px 0;");
            template.AppendLine("        }");
            template.AppendLine("        .footer {");
            template.AppendLine("            margin-top: 30px;");
            template.AppendLine("            font-size: 12px;");
            template.AppendLine("            color: #6c757d;");
            template.AppendLine("            text-align: center;");
            template.AppendLine("        }");
            template.AppendLine("        .additional-message {");
            template.AppendLine("            background-color: #f0f7f4;");
            template.AppendLine("            border-left: 4px solid #1a3b29;");
            template.AppendLine("            padding: 10px 15px;");
            template.AppendLine("            margin: 15px 0;");
            template.AppendLine("        }");
            template.AppendLine("    </style>");
            template.AppendLine("</head>");
            template.AppendLine("<body>");
            template.AppendLine("    <div class=\"container\">");
            template.AppendLine("        <div class=\"header\">");
            template.AppendLine("            <div class=\"logo\">Athrna</div>");
            template.AppendLine("        </div>");
            template.AppendLine("        ");
            template.AppendLine("        <h2>Welcome to Athrna</h2>");
            template.AppendLine("        ");
            template.AppendLine($"        <p>Hello {username},</p>");
            template.AppendLine("        ");
            template.AppendLine("        <p>Thank you for registering with Athrna. To complete your registration and verify your email address, please click the button below:</p>");
            template.AppendLine("        ");
            template.AppendLine("        <div style=\"text-align: center;\">");
            template.AppendLine($"            <a href=\"{verificationLink}\" class=\"button\">Verify Your Email</a>");
            template.AppendLine("        </div>");
            template.AppendLine("        ");
            template.AppendLine("        <p>If the button above doesn't work, copy and paste this URL into your browser:</p>");
            template.AppendLine($"        <p style=\"word-break: break-all;\">{verificationLink}</p>");
            template.AppendLine("        ");

            if (!string.IsNullOrEmpty(additionalMessage))
            {
                template.AppendLine("        <div class=\"additional-message\">");
                template.AppendLine($"            {additionalMessage}");
                template.AppendLine("        </div>");
            }

            template.AppendLine("        ");
            template.AppendLine("        <p>This link will expire in 24 hours. If you did not create an account with Athrna, please ignore this email.</p>");
            template.AppendLine("        ");
            template.AppendLine("        <p>Thank you,<br>The Athrna Team</p>");
            template.AppendLine("        ");
            template.AppendLine("        <div class=\"footer\">");
            template.AppendLine("            <p>This is an automated message, please do not reply to this email.</p>");
            template.AppendLine("            <p>&copy; 2025 Athrna - Saudi Historical Sites. All rights reserved.</p>");
            template.AppendLine("        </div>");
            template.AppendLine("    </div>");
            template.AppendLine("</body>");
            template.AppendLine("</html>");

            return template.ToString();
        }

        public static async Task EnsureEmailVerificationTemplateExists(IHostEnvironment environment)
        {
            string templateDir = Path.Combine(environment.ContentRootPath, "EmailTemplates");
            string templatePath = Path.Combine(templateDir, "EmailVerification.html");

            // Create directory if it doesn't exist
            if (!Directory.Exists(templateDir))
            {
                Directory.CreateDirectory(templateDir);
            }

            // Only create the template file if it doesn't already exist
            if (!File.Exists(templatePath))
            {
                string template = GenerateVerificationEmailTemplate("{VerificationLink}", "{Username}", "{AdditionalMessage}");
                await File.WriteAllTextAsync(templatePath, template);
            }
        }
    }
}