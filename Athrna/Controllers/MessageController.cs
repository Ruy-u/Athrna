using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Athrna.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MessageController> _logger;

        public MessageController(
            ApplicationDbContext context,
            ILogger<MessageController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Make sure this attribute is present
        public async Task<IActionResult> SendToGuide(int guideId, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return BadRequest("Message cannot be empty.");
            }

            try
            {
                // Check if the user is a guide
                bool isGuide = User.IsInRole("Guide");

                if (isGuide)
                {
                    string userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                    // Get the guide being messaged
                    var guide = await _context.Guide.FirstOrDefaultAsync(g => g.Id == guideId);

                    if (guide == null)
                    {
                        return Json(new { success = false, message = "Guide not found." });
                    }

                    // Check if the guide is trying to message themselves
                    if (guide.Email == userEmail)
                    {
                        return Json(new { success = false, message = "You cannot send messages to yourself." });
                    }
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var message = new Message
                {
                    SenderId = userId,
                    SenderType = "Client",
                    RecipientId = guideId,
                    RecipientType = "Guide",
                    Content = content,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to guide: {GuideId}", guideId);
                return Json(new { success = false, message = "An error occurred while sending your message." });
            }
        }

        [HttpGet]
        [Authorize] // Ensure only authenticated users access this
        public async Task<IActionResult> Conversation(int guideId)
        {
            // Get user ID
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            string userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            // Check if the user is a guide
            bool isGuide = User.IsInRole("Guide");

            // Get the guide being messaged
            var guide = await _context.Guide
                .Include(g => g.City)
                .FirstOrDefaultAsync(g => g.Id == guideId);

            if (guide == null)
            {
                return NotFound();
            }

            if (isGuide && guide.Email == userEmail)
            {
                TempData["ErrorMessage"] = "You cannot send messages to yourself.";
                return RedirectToAction("Index", "Home");
            }

            // Get all messages between user and guide
            var messages = await _context.Messages
                .Where(m =>
                    (m.SenderId == userId && m.SenderType == "Client" &&
                     m.RecipientId == guideId && m.RecipientType == "Guide") ||
                    (m.SenderId == guideId && m.SenderType == "Guide" &&
                     m.RecipientId == userId && m.RecipientType == "Client"))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // Mark unread messages as read
            var unreadMessages = messages
                .Where(m => m.RecipientId == userId &&
                            m.RecipientType == "Client" &&
                            !m.IsRead)
                .ToList();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            // Create view model
            var viewModel = new ConversationDetailViewModel
            {
                ClientId = userId,
                ClientName = User.Identity.Name,
                Messages = messages,
                GuideId = guideId,
                GuideName = guide.FullName
            };

            ViewBag.Guide = guide;

            return View("Conversation", viewModel);
        }


        // Display a list of guides that the user can message
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var guides = await _context.Guide
                .Include(g => g.City)
                .OrderBy(g => g.FullName)
                .ToListAsync();

            return View(guides);
        }

        // API endpoint to get new messages
        [HttpGet]
        public async Task<IActionResult> GetNewMessages(int guideId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get all unread messages from guide to user
            var messages = await _context.Messages
                .Where(m => m.SenderId == guideId &&
                            m.SenderType == "Guide" &&
                            m.RecipientId == userId &&
                            m.RecipientType == "Client" &&
                            !m.IsRead)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // Mark messages as read
            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            // Format messages for JSON response
            var formattedMessages = messages.Select(m => new
            {
                id = m.Id,
                content = m.Content,
                sentAt = m.SentAt.ToString("yyyy-MM-dd HH:mm:ss"),
                formattedTime = m.SentAt.ToString("MMM dd, h:mm tt")
            });

            return Json(formattedMessages);
        }
    }
}