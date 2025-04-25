using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Athrna.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            // Safely get port, with fallback value of 587 if the setting is missing
            int smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);

            var smtpClient = new SmtpClient(_configuration["Email:SmtpServer"])
            {
                Port = smtpPort,  // Use the safely retrieved port value
                Credentials = new NetworkCredential(_configuration["Email:Username"], _configuration["Email:Password"]),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Email:From"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(email);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log error for troubleshooting
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
    }


}
