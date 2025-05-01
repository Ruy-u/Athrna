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
                var recentBookings = await _context.Bookings
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

        // GET: GuideDashboard/Messages
        public async Task<IActionResult> Messages()
        {
            int guideId = await GetCurrentGuideId();

            // Get all message threads for the guide
            var messages = await _context.Messages
                .Include(m => m.Client)
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
                    ClientName = g.First().SenderType == "Guide"
                        ? g.First().Client.Username
                        : g.First().Client.Username,
                    LastMessage = g.OrderByDescending(m => m.SentAt).First(),
                    UnreadCount = g.Count(m => m.RecipientId == guideId &&
                                         m.RecipientType == "Guide" &&
                                         !m.IsRead)
                })
                .ToList();

            return View(conversations);
        }

        // GET: GuideDashboard/Conversation/5
        public async Task<IActionResult> Conversation(int id)
        {
            int guideId = await GetCurrentGuideId();

            // Get the client
            var client = await _context.Client.FindAsync(id);
            if (client == null)
            {
                return NotFound();
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

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            var viewModel = new ConversationDetailViewModel
            {
                ClientId = id,
                ClientName = client.Username,
                Messages = messages,
                GuideId = guideId
            };

            return View(viewModel);
        }

        // POST: GuideDashboard/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int recipientId, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest("Message cannot be empty");
            }

            int guideId = await GetCurrentGuideId();

            // Create new message
            var newMessage = new Message
            {
                SenderId = guideId,
                SenderType = "Guide",
                RecipientId = recipientId,
                RecipientType = "Client",
                Content = message,
                SentAt = DateTime.UtcNow,
                IsRead = false
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

            var bookings = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Site)
                .Where(b => b.GuideId == guideId)
                .OrderByDescending(b => b.TourDateTime)
                .ToListAsync();

            return View(bookings);
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
            var booking = await _context.Bookings
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
            // Get user email from claims
            var emailClaim = User.FindFirst(ClaimTypes.Email);
            if (emailClaim == null)
            {
                throw new ApplicationException("Email claim not found");
            }

            string email = emailClaim.Value;

            // Get guide by email
            var guide = await _context.Guide
                .FirstOrDefaultAsync(g => g.Email == email);

            if (guide == null)
            {
                throw new ApplicationException("Guide not found for email: " + email);
            }

            return guide.Id;
        }
    }
}