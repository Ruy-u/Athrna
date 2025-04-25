using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using System.Text;

namespace Athrna.Services
{
    public class PasswordResetTokenService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ILogger<PasswordResetTokenService> _logger;
        private const string Purpose = "PasswordReset";

        public PasswordResetTokenService(IDataProtectionProvider dataProtectionProvider, ILogger<PasswordResetTokenService> logger)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _logger = logger;
        }

        public string GenerateToken(string email)
        {
            try
            {
                // Create a protector
                var protector = _dataProtectionProvider.CreateProtector(Purpose);

                // Current time for token expiration (24 hours from now)
                var expirationTime = DateTime.UtcNow.AddHours(24).Ticks;

                // Create token payload (email|expirationTime)
                string payload = $"{email}|{expirationTime}";

                // Protect and Base64 encode the payload
                string protectedPayload = protector.Protect(payload);
                string token = Convert.ToBase64String(Encoding.UTF8.GetBytes(protectedPayload));

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset token for {Email}", email);
                throw;
            }
        }

        public (bool IsValid, string Email) ValidateToken(string token)
        {
            try
            {
                // Decode the token from Base64
                var protectedPayload = Encoding.UTF8.GetString(Convert.FromBase64String(token));

                // Create a protector with the same purpose
                var protector = _dataProtectionProvider.CreateProtector(Purpose);

                // Unprotect the payload
                var payload = protector.Unprotect(protectedPayload);

                // Split payload to get email and expiration time
                var parts = payload.Split('|');
                if (parts.Length != 2)
                {
                    return (false, string.Empty);
                }

                var email = parts[0];
                var expirationTicks = long.Parse(parts[1]);
                var expirationTime = new DateTime(expirationTicks);

                // Check if token is still valid (not expired)
                if (DateTime.UtcNow > expirationTime)
                {
                    _logger.LogWarning("Password reset token has expired for {Email}", email);
                    return (false, string.Empty);
                }

                return (true, email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password reset token");
                return (false, string.Empty);
            }
        }
    }
}