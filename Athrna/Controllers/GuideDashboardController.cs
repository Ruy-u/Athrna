using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace Athrna.Controllers
{
    [Authorize(Roles = "Guide")]
    public class GuideDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GuideDashboardController> _logger;

        public GuideDashboardController(
            ApplicationDbContext context,
            ILogger<GuideDashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: GuideDashboard/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get current guide ID
                int guideId = await GetCurrentGuideId();

                // Get guide profile
                var guide = await _context.Guide
                    .Include(g => g.City)
                    .FirstOrDefaultAsync(g => g.Id == guideId);

                if (guide == null)
                {
                    return NotFound();
                }

                // Get recent bookings
                var recentBookings = await _context.Booking
                    .Include(b => b.Client)
                    .Include(b => b.Site)
                    .Where(b => b.GuideId == guideId)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(5)
                    .ToListAsync();

                // Get count of pending bookings
                int pendingBookings = await _context.Booking
                    .CountAsync(b => b.GuideId == guideId && b.Status == "Pending");

                // Get count of completed tours
                int completedTours = await _context.Booking
                    .CountAsync(b => b.GuideId == guideId && b.Status == "Completed");

                // Get count of current bookings (Pending or Confirmed, not Completed or Cancelled)
                int currentBookings = await _context.Booking
                    .CountAsync(b => b.GuideId == guideId &&
                                (b.Status == "Pending" || b.Status == "Confirmed"));

                // Get unread messages count
                int unreadMessages = await _context.Messages
                    .CountAsync(m => m.RecipientId == guideId
                             && m.RecipientType == "Guide"
                             && !m.IsRead);

                // Create dashboard view model
                var viewModel = new GuideDashboardViewModel
                {
                    Guide = guide,
                    RecentBookings = recentBookings,
                    UnreadMessages = unreadMessages,
                    PendingBookings = pendingBookings,
                    CompletedTours = completedTours,
                    CurrentBookings = currentBookings,
                    // Remove the AverageRating property since we're removing guide reviews
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guide dashboard for guide ID: {GuideId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier));

                return RedirectToAction("Error", "Home", new { message = "Error loading dashboard." });
            }
        }

        // GET: GuideDashboard/Profile
        // GET: GuideDashboard/Profile
        public async Task<IActionResult> Profile()
        {
            try
            {
                // Get current guide ID
                int guideId = await GetCurrentGuideId();

                // Populate unread messages count in ViewBag
                await PopulateUnreadMessagesCount(guideId);

                // Get guide with city information
                var guide = await _context.Guide
                    .Include(g => g.City)
                    .FirstOrDefaultAsync(g => g.Id == guideId);

                if (guide == null)
                {
                    _logger.LogWarning("Guide not found with ID: {GuideId}", guideId);
                    return NotFound("Guide profile not found. Please contact support.");
                }

                // Make sure we're not returning null
                return View(guide);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guide profile");
                TempData["ErrorMessage"] = "An error occurred while loading your profile. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Guide model, string username, string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                // Get current guide ID
                int guideId = await GetCurrentGuideId();

                // Get existing guide with City included
                var guide = await _context.Guide
                    .Include(g => g.City)
                    .FirstOrDefaultAsync(g => g.Id == guideId);

                if (guide == null)
                {
                    _logger.LogWarning("Guide not found with ID: {GuideId} during profile update", guideId);
                    return NotFound("Guide profile not found. Please contact support.");
                }

                // Keep the original values we don't want to update
                model.Email = guide.Email;
                model.CityId = guide.CityId;
                model.City = guide.City;

                // Get the client associated with this guide via email
                var client = await _context.Client.FirstOrDefaultAsync(c => c.Email == guide.Email);
                if (client == null)
                {
                    _logger.LogWarning("Client not found for guide with email: {Email}", guide.Email);
                    ModelState.AddModelError("", "Associated user account not found.");
                    return View("Profile", guide);
                }

                // Also get the guide application if it exists
                var guideApplication = await _context.GuideApplication
                    .FirstOrDefaultAsync(ga => ga.Email == guide.Email);

                bool changesMade = false;
                bool usernameChanged = false;

                // Update the username if provided
                if (!string.IsNullOrEmpty(username) && username != client.Username)
                {
                    // Check if username is already taken (by someone else)
                    var existingUser = await _context.Client.FirstOrDefaultAsync(c =>
                        c.Username.ToLower() == username.ToLower() && c.Id != client.Id);

                    if (existingUser != null)
                    {
                        ModelState.AddModelError("username", "This username is already taken");
                        return View("Profile", guide);
                    }

                    // Update username in Client table
                    client.Username = username;

                    // Also update in GuideApplication table if it exists
                    if (guideApplication != null)
                    {
                        guideApplication.Username = username;
                        _context.Update(guideApplication);
                    }

                    changesMade = true;
                    usernameChanged = true;
                }

                // Handle password change if provided
                if (!string.IsNullOrEmpty(newPassword))
                {
                    // Validate current password
                    if (string.IsNullOrEmpty(currentPassword))
                    {
                        ModelState.AddModelError("currentPassword", "Current password is required to set a new password");
                        return View("Profile", guide);
                    }

                    // Check if current password matches
                    if (client.EncryptedPassword != currentPassword)
                    {
                        ModelState.AddModelError("currentPassword", "Current password is incorrect");
                        return View("Profile", guide);
                    }

                    // Confirm passwords match
                    if (newPassword != confirmPassword)
                    {
                        ModelState.AddModelError("confirmPassword", "New password and confirmation do not match");
                        return View("Profile", guide);
                    }

                    // Validate password strength
                    if (!IsStrongPassword(newPassword))
                    {
                        ModelState.AddModelError("newPassword", "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character");
                        return View("Profile", guide);
                    }

                    // Update password in Client table
                    client.EncryptedPassword = newPassword;

                    // Also update in Guide table
                    guide.Password = newPassword;
                    _context.Update(guide);

                    // Also update in GuideApplication table if it exists
                    if (guideApplication != null)
                    {
                        guideApplication.Password = newPassword;
                        _context.Update(guideApplication);
                    }

                    changesMade = true;
                }

                // Save changes if any were made
                if (changesMade)
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Guide account updated successfully: {GuideId}", guideId);
                    TempData["SuccessMessage"] = "Your account has been updated successfully.";

                    // If username changed, we need to update authentication cookie
                    if (usernameChanged)
                    {
                        // Get existing claims
                        var identity = (ClaimsIdentity)User.Identity;
                        var existingClaims = identity.Claims.ToList();

                        // Sign out current user
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                        // Create new claims identity with updated username
                        var newClaims = existingClaims.Where(c => c.Type != ClaimTypes.Name).ToList();
                        newClaims.Add(new Claim(ClaimTypes.Name, username));
                        var newIdentity = new ClaimsIdentity(newClaims, CookieAuthenticationDefaults.AuthenticationScheme);

                        // Sign in with new claims
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(newIdentity),
                            authProperties);

                        _logger.LogInformation("User authentication cookie updated with new username: {Username}", username);
                    }
                }
                else
                {
                    TempData["InfoMessage"] = "No changes were made to your account.";
                }

                // Return to profile page
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating guide profile");
                TempData["ErrorMessage"] = "An error occurred while updating your profile. Please try again later.";
                return View("Profile", model);
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
        // GET: GuideDashboard/Messages
        public async Task<IActionResult> Messages()
        {
            try
            {
                int guideId = await GetCurrentGuideId();

                // Populate unread messages count in ViewBag (will be used in the view)
                await PopulateUnreadMessagesCount(guideId);

                // Get all message threads for the guide
                var messages = await _context.Messages
                    .Include(m => m.Client)  // Include Client information
                    .Where(m => (m.SenderId == guideId && m.SenderType == "Guide") ||
                               (m.RecipientId == guideId && m.RecipientType == "Guide"))
                    .OrderByDescending(m => m.SentAt)
                    .ToListAsync();

                // Group by conversation
                var conversations = messages
                    .GroupBy(m => m.SenderType == "Guide" ? m.RecipientId : m.SenderId)
                    .Select(g => new ConversationViewModel
                    {
                        ClientId = g.Key,
                        // Safely get the client name by checking if Client is null first
                        ClientName = g.FirstOrDefault(m => m.Client != null)?.Client?.Username ??
                                   (g.Any(m => m.SenderType == "Client" && m.SenderId == g.Key) ?
                                   "Client #" + g.Key : "Unknown Client"),
                        LastMessage = g.OrderByDescending(m => m.SentAt).First(),
                        UnreadCount = g.Count(m => m.RecipientId == guideId &&
                                             m.RecipientType == "Guide" &&
                                             !m.IsRead)
                    })
                    .ToList();

                _logger.LogInformation("Guide {GuideId} viewed messages list with {ConversationCount} conversations",
                    guideId, conversations.Count);

                return View(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading messages for guide ID: {GuideId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier));

                TempData["ErrorMessage"] = "An error occurred while loading your messages.";
                return RedirectToAction("Index");
            }
        }


        // GET: GuideDashboard/Conversation/5
        public async Task<IActionResult> Conversation(int id)
        {
            try
            {
                int guideId = await GetCurrentGuideId();

                // Populate unread messages count in ViewBag
                await PopulateUnreadMessagesCount(guideId);

                var guide = await _context.Guide.FindAsync(guideId);

                // Get the client with better error handling
                var client = await _context.Client.FindAsync(id);
                if (client == null)
                {
                    _logger.LogWarning("Client not found for ID: {ClientId}", id);
                    TempData["ErrorMessage"] = "Client not found.";
                    return RedirectToAction("Messages");
                }

                // Get all messages between guide and client
                var messages = await _context.Messages
                    .Where(m => (m.SenderId == guideId && m.SenderType == "Guide" &&
                               m.RecipientId == id && m.RecipientType == "Client") ||
                               (m.SenderId == id && m.SenderType == "Client" &&
                               m.RecipientId == guideId && m.RecipientType == "Guide"))
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                // Mark unread messages as read
                var unreadMessages = messages.Where(m => m.RecipientId == guideId &&
                                         m.RecipientType == "Guide" &&
                                         !m.IsRead).ToList();

                if (unreadMessages.Any())
                {
                    foreach (var message in unreadMessages)
                    {
                        message.IsRead = true;
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Marked {Count} messages as read in conversation between guide {GuideId} and client {ClientId}",
                        unreadMessages.Count, guideId, id);
                }

                var viewModel = new ConversationDetailViewModel
                {
                    ClientId = id,
                    ClientName = client.Username,  // Use the actual client username
                    Messages = messages,
                    GuideId = guideId,
                    GuideName = guide?.FullName ?? "Guide"
                };

                _logger.LogInformation("Guide {GuideId} viewed conversation with client {ClientId}", guideId, id);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation with client ID: {ClientId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the conversation.";
                return RedirectToAction("Messages");
            }
        }

        // In GuideDashboardController.cs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int recipientId, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest("Message cannot be empty");
            }

            int guideId = await GetCurrentGuideId();
            var guide = await _context.Guide.FindAsync(guideId);
            var client = await _context.Client.FindAsync(recipientId);

            if (guide == null || client == null)
            {
                return NotFound("Guide or client not found");
            }

            // Create new message
            var newMessage = new Message
            {
                SenderId = guideId,
                SenderType = "Guide",
                RecipientId = recipientId,
                RecipientType = "Client",
                Content = message,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                Guide = guide,
                Client = client
            };

            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync();

            // Return to conversation
            return RedirectToAction("Conversation", new { id = recipientId });
        }

        // GET: GuideDashboard/Bookings
        public async Task<IActionResult> Bookings()
        {
            int guideId = await GetCurrentGuideId();

            // Populate unread messages count in ViewBag
            await PopulateUnreadMessagesCount(guideId);

            var bookings = await _context.Booking
                .Include(b => b.Client)
                .Include(b => b.Site)
                .Where(b => b.GuideId == guideId)
                .OrderByDescending(b => b.TourDateTime)
                .ToListAsync();

            return View(bookings);
        }

        // GET: GuideDashboard/ViewBooking/5
        public async Task<IActionResult> ViewBooking(int id)
        {
            try
            {
                int guideId = await GetCurrentGuideId();

                // Populate unread messages count in ViewBag
                await PopulateUnreadMessagesCount(guideId);

                // Get booking with related data
                var booking = await _context.Booking
                    .Include(b => b.Client)
                    .Include(b => b.Site)
                        .ThenInclude(s => s.City)
                    .FirstOrDefaultAsync(b => b.Id == id && b.GuideId == guideId);

                if (booking == null)
                {
                    _logger.LogWarning("Booking not found or does not belong to the current guide. Booking ID: {BookingId}, Guide ID: {GuideId}",
                        id, guideId);
                    return NotFound();
                }

                _logger.LogInformation("Guide {GuideId} viewed booking {BookingId}", guideId, id);
                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving booking details for ID: {BookingId}", id);
                TempData["ErrorMessage"] = "An error occurred while retrieving booking details.";
                return RedirectToAction("Bookings");
            }
        }

        // GET: GuideDashboard/UpdateStatus/5?status=Confirmed
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var validStatuses = new[] { "Pending", "Confirmed", "Completed", "Cancelled" };

            if (!validStatuses.Contains(status))
            {
                return BadRequest("Invalid status");
            }

            int guideId = await GetCurrentGuideId();

            // Get the booking
            var booking = await _context.Booking
                .FirstOrDefaultAsync(b => b.Id == id && b.GuideId == guideId);

            if (booking == null)
            {
                return NotFound();
            }

            // Update status
            booking.Status = status;

            await _context.SaveChangesAsync();

            return RedirectToAction("Bookings");
        }

        // GET: GuideDashboard/Availability
        public async Task<IActionResult> Availability()
        {
            int guideId = await GetCurrentGuideId();

            // Populate unread messages count in ViewBag
            await PopulateUnreadMessagesCount(guideId);

            // Get guide's availability
            var availability = await _context.GuideAvailabilities
                .Where(a => a.GuideId == guideId)
                .OrderBy(a => a.DayOfWeek)
                .ThenBy(a => a.StartTime)
                .ToListAsync();

            if (!availability.Any())
            {
                // Create default availability (9 AM to 5 PM, all days)
                for (int i = 0; i < 7; i++)
                {
                    var day = (DayOfWeek)i;
                    availability.Add(new GuideAvailability
                    {
                        GuideId = guideId,
                        DayOfWeek = day,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        IsAvailable = true
                    });
                }

                _context.GuideAvailabilities.AddRange(availability);
                await _context.SaveChangesAsync();
            }

            return View(availability);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvailability(List<GuideAvailability> availability)
        {
            int guideId = await GetCurrentGuideId();

            // Get existing availability
            var existingAvailability = await _context.GuideAvailabilities
                .Where(a => a.GuideId == guideId)
                .ToListAsync();

            // Remove existing entries
            _context.GuideAvailabilities.RemoveRange(existingAvailability);

            // Add new entries
            foreach (var item in availability)
            {
                item.GuideId = guideId;
                _context.GuideAvailabilities.Add(item);
            }

            await _context.SaveChangesAsync();

            // Add success message
            TempData["SuccessMessage"] = "Your availability has been updated successfully.";

            return RedirectToAction("Availability");
        }

        // Helper method to get current guide ID
        private async Task<int> GetCurrentGuideId()
        {
            try
            {
                // Get user email from claims
                var emailClaim = User.FindFirst(ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogWarning("Email claim not found for user: {Username}", User.Identity.Name);
                    throw new ApplicationException("Email claim not found");
                }

                string email = emailClaim.Value;
                _logger.LogInformation("Looking up guide for email: {Email}", email);

                // Get guide by email
                var guide = await _context.Guide
                    .FirstOrDefaultAsync(g => g.Email == email);

                if (guide == null)
                {
                    _logger.LogWarning("No guide found for email: {Email}", email);
                    throw new ApplicationException("Guide not found for email: " + email);
                }

                _logger.LogInformation("Found guide: {GuideName} (ID: {GuideId})", guide.FullName, guide.Id);
                return guide.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current guide ID");
                throw;
            }
        }
        // Helper method to populate unread messages count in ViewBag
        private async Task PopulateUnreadMessagesCount(int guideId)
        {
            // Get unread messages count
            int unreadMessages = await _context.Messages
                .CountAsync(m => m.RecipientId == guideId
                         && m.RecipientType == "Guide"
                         && !m.IsRead);

            ViewBag.UnreadMessages = unreadMessages;
        }
    }
}