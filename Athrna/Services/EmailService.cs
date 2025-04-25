using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Athrna.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string message)
        {
            try
            {
                // Get SMTP settings from configuration
                var smtpServer = _configuration["Email:SmtpServer"];
                var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
                var username = _configuration["Email:Username"];
                var password = _configuration["Email:Password"];
                var fromEmail = _configuration["Email:From"];

                // Log configuration values (without password)
                _logger.LogInformation("Email sending attempt with config: Server={Server}, Port={Port}, Username={Username}, FromEmail={FromEmail}",
                    smtpServer, smtpPort, username, fromEmail);

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(username) ||
                    string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogError("Email configuration is incomplete: Server={IsServerEmpty}, Username={IsUsernameEmpty}, Password={IsPasswordEmpty}, FromEmail={IsFromEmailEmpty}",
                        string.IsNullOrEmpty(smtpServer), string.IsNullOrEmpty(username), string.IsNullOrEmpty(password), string.IsNullOrEmpty(fromEmail));
                    return false;
                }

                // Create mail message
                _logger.LogInformation("Creating mail message to {ToEmail} with subject '{Subject}'", toEmail, subject);
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                // Configure SmtpClient
                _logger.LogInformation("Configuring SMTP client for {Server}:{Port}", smtpServer, smtpPort);
                var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                // Send email
                _logger.LogInformation("Attempting to send email via SMTP...");
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}: {ErrorMessage}", toEmail, ex.Message);

                // Log more detailed exceptions for specific issues
                if (ex is SmtpException smtpEx)
                {
                    _logger.LogError("SMTP Status Code: {StatusCode}", smtpEx.StatusCode);
                    _logger.LogError("SMTP Error Message: {ErrorMessage}", smtpEx.Message);

                    if (smtpEx.InnerException != null)
                    {
                        _logger.LogError("Inner exception: {InnerException}", smtpEx.InnerException.Message);
                    }
                }

                return false;
            }
        }
    }
}