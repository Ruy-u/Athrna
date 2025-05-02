using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Athrna.Controllers
{
    [Authorize]
    public class UserDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: UserDashboard
        public async Task<IActionResult> Index()
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            // Get current user
            var user = await _context.Client
                .FirstOrDefaultAsync(c => c.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            // Get user's bookmarks with site information
            var bookmarks = await _context.Bookmark
                .Include(b => b.Site)
                    .ThenInclude(s => s.City)
                .Where(b => b.ClientId == userId)
                .ToListAsync();

            // Get user's ratings with site information
            var ratings = await _context.Rating
                .Include(r => r.Site)
                    .ThenInclude(s => s.City)
                .Where(r => r.ClientId == userId)
                .ToListAsync();

            // Get recent bookings
            var recentBookings = await _context.Bookings
                .Include(b => b.Guide)
                    .ThenInclude(g => g.City)
                .Include(b => b.Site)
                .Where(b => b.ClientId == userId)
                .OrderByDescending(b => b.BookingDate)
                .Take(5)
                .ToListAsync();

            // Get unread messages count
            int unreadMessages = await _context.Messages
                .CountAsync(m => m.RecipientId == userId
                         && m.RecipientType == "Client"
                         && !m.IsRead);

            // Get count of conversations with unread messages
            var pendingConversations = await _context.Messages
                .Where(m => m.RecipientId == userId && m.RecipientType == "Client" && !m.IsRead)
                .Select(m => m.SenderId)
                .Distinct()
                .CountAsync();

            // Create view model with all information
            var viewModel = new UserDashboardViewModel
            {
                User = user,
                Bookmarks = bookmarks,
                Ratings = ratings,
                RecentBookings = recentBookings,
                UnreadMessages = unreadMessages,
                PendingConversations = pendingConversations
            };

            return View(viewModel);
        }

        // GET: UserDashboard/Messages
        public async Task<IActionResult> Messages()
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            // Get all message threads for the user
            var messages = await _context.Messages
                .Include(m => m.Guide)
                .Where(m => (m.SenderId == userId && m.SenderType == "Client") ||
                           (m.RecipientId == userId && m.RecipientType == "Client"))
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            // Group by conversation with guides
            var conversations = messages
                .GroupBy(m => m.SenderType == "Client" ? m.RecipientId : m.SenderId)
                .Select(g => new
                {
                    GuideId = g.Key,
                    GuideName = g.FirstOrDefault(m => m.Guide != null)?.Guide?.FullName ?? "Guide",
                    LastMessage = g.OrderByDescending(m => m.SentAt).First(),
                    UnreadCount = g.Count(m => m.RecipientId == userId &&
                                         m.RecipientType == "Client" &&
                                         !m.IsRead)
                })
                .ToList();

            // Convert to view models
            var viewModels = conversations.Select(c => new ConversationViewModel
            {
                GuideId = c.GuideId,
                GuideName = c.GuideName,
                LastMessage = c.LastMessage,
                UnreadCount = c.UnreadCount
            }).ToList();

            return View(viewModels);
        }

        // GET: UserDashboard/Bookmarks
        public async Task<IActionResult> Bookmarks()
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            // Get user's bookmarks with site information
            var bookmarks = await _context.Bookmark
                .Include(b => b.Site)
                    .ThenInclude(s => s.City)
                .Where(b => b.ClientId == userId)
                .ToListAsync();

            return View(bookmarks);
        }

        // GET: UserDashboard/Ratings
        public async Task<IActionResult> Ratings()
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            // Get user's ratings with site information
            var ratings = await _context.Rating
                .Include(r => r.Site)
                    .ThenInclude(s => s.City)
                .Where(r => r.ClientId == userId)
                .ToListAsync();

            return View(ratings);
        }

        // POST: UserDashboard/RemoveBookmark/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBookmark(int id)
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            var bookmark = await _context.Bookmark
                .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == userId);

            if (bookmark != null)
            {
                _context.Bookmark.Remove(bookmark);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Bookmark removed successfully!";
            }

            return RedirectToAction(nameof(Bookmarks));
        }

        // POST: UserDashboard/RemoveRating/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRating(int id)
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            var rating = await _context.Rating
                .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == userId);

            if (rating != null)
            {
                _context.Rating.Remove(rating);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Rating removed successfully!";
            }

            return RedirectToAction(nameof(Ratings));
        }

        // GET: UserDashboard/Profile
        public async Task<IActionResult> Profile()
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            var user = await _context.Client
                .FirstOrDefaultAsync(c => c.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var profileViewModel = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email
            };

            return View(profileViewModel);
        }

        // POST: UserDashboard/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            var user = await _context.Client
                .FirstOrDefaultAsync(c => c.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            // Check if new email is already taken by another user
            if (model.Email != user.Email)
            {
                if (await _context.Client.AnyAsync(c => c.Email == model.Email && c.Id != userId))
                {
                    ModelState.AddModelError("Email", "Email is already registered to another account");
                    return View("Profile", model);
                }
                user.Email = model.Email;
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (model.CurrentPassword != user.EncryptedPassword)
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                    return View("Profile", model);
                }

                user.EncryptedPassword = model.NewPassword;
            }

            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Profile));
        }
    }
}