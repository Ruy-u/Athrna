using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
        public async Task<IActionResult> SendToGuide(int guideId, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return BadRequest("Message cannot be empty.");
            }

            try
            {
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
        public async Task<IActionResult> Conversation(int guideId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get the guide
            var guide = await _context.Guide
                .Include(g => g.City)
                .FirstOrDefaultAsync(g => g.Id == guideId);

            if (guide == null)
            {
                return NotFound();
            }

            // Get all messages between user and guide
            var messages = await _context.Messages
                .Where(m => (m.SenderId == userId && m.SenderType == "Client" &&
                           m.RecipientId == guideId && m.RecipientType == "Guide") ||
                           (m.SenderId == guideId && m.SenderType == "Guide" &&
                           m.RecipientId == userId && m.RecipientType == "Client"))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // Mark unread messages as read
            var unreadMessages = messages.Where(m => m.RecipientId == userId &&
                                             m.RecipientType == "Client" &&
                                             !m.IsRead).ToList();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            var viewModel = new ConversationDetailViewModel
            {
                ClientId = userId,
                ClientName = User.Identity.Name,
                Messages = messages,
                GuideId = guideId,
                GuideName = guide.FullName
            };

            ViewBag.Guide = guide;

            return View(viewModel);
        }
    }
}