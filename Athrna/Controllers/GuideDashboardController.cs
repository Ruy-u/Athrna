using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

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
                    // Add additional properties as needed
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
        public async Task<IActionResult> Profile()
        {
            try
            {
                // Get current guide ID
                int guideId = await GetCurrentGuideId();

                // Get guide with city information
                var guide = await _context.Guide
                    .Include(g => g.City)
                    .FirstOrDefaultAsync(g => g.Id == guideId);

                if (guide == null)
                {
                    _logger.LogWarning("Guide not found with ID: {GuideId}", guideId);
                    return NotFound("Guide profile not found. Please contact support.");
                }

                // Return guide model directly to the view
                return View(guide);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guide profile");
                TempData["ErrorMessage"] = "An error occurred while loading your profile. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        // POST: GuideDashboard/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Guide model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("Profile", model);
                }

                // Get current guide ID
                int guideId = await GetCurrentGuideId();

                // Get existing guide
                var guide = await _context.Guide
                    .Include(g => g.City)
                    .FirstOrDefaultAsync(g => g.Id == guideId);

                if (guide == null)
                {
                    _logger.LogWarning("Guide not found with ID: {GuideId} during profile update", guideId);
                    return NotFound("Guide profile not found. Please contact support.");
                }

                // Update fields that should be updatable
                guide.FullName = model.FullName;
                // Note: Email is not updated as it's tied to authentication
                // Note: Password changes should be handled separately with confirmation

                // Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("Guide profile updated successfully: {GuideId}", guideId);
                TempData["SuccessMessage"] = "Your profile has been updated successfully.";

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating guide profile");
                TempData["ErrorMessage"] = "An error occurred while updating your profile. Please try again later.";
                return View("Profile", model);
            }
        }

        // GET: GuideDashboard/Messages
        public async Task<IActionResult> Messages()
        {
            try
            {
                int guideId = await GetCurrentGuideId();

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

        // POST: GuideDashboard/UpdateAvailability
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
    }
}