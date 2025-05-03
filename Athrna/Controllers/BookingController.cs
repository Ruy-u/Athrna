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
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingController> _logger;

        public BookingController(
            ApplicationDbContext context,
            ILogger<BookingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize]
        // GET: Booking/Create/5 (5 is the guide ID)
        public async Task<IActionResult> Create(int id, int? siteId)
        {
            // Check if the user is a guide
            bool isGuide = User.IsInRole("Guide");

            if (isGuide)
            {
                string userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                // Get the guide being booked
                var guideCheck = await _context.Guide
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (guideCheck == null)
                {
                    return NotFound();
                }

                // Check if the guide is trying to book themselves
                if (guideCheck.Email == userEmail)
                {
                    TempData["ErrorMessage"] = "You cannot book yourself as a guide.";
                    return RedirectToAction("Index", "Home");
                }

                // Guides can't book other guides
                TempData["ErrorMessage"] = "Guides cannot book other guides.";
                return RedirectToAction("Index", "Home");
            }

            // Get the guide
            var guide = await _context.Guide
                .Include(g => g.City)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (guide == null)
            {
                return NotFound();
            }

            // Get the site if specified
            Site site = null;
            if (siteId.HasValue)
            {
                site = await _context.Site
                    .Include(s => s.City)
                    .FirstOrDefaultAsync(s => s.Id == siteId.Value);
            }

            // Create booking view model
            var viewModel = new CreateBookingViewModel
            {
                GuideId = guide.Id,
                GuideName = guide.FullName,
                GuideCity = guide.City.Name,
                SiteId = siteId,
                SiteName = site?.Name,
                MinDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                TourDateTime = DateTime.Now.AddDays(1)
            };

            // Get guide availability
            var availability = await _context.GuideAvailabilities
                .Where(a => a.GuideId == guide.Id && a.IsAvailable)
                .OrderBy(a => a.DayOfWeek)
                .ToListAsync();

            viewModel.GuideAvailability = availability;

            return View(viewModel);
        }


        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload guide and site details on validation failure
                var guide = await _context.Guide
                    .Include(g => g.City)
                    .FirstOrDefaultAsync(g => g.Id == model.GuideId);

                if (guide != null)
                {
                    model.GuideName = guide.FullName;
                    model.GuideCity = guide.City.Name;
                }

                if (model.SiteId.HasValue)
                {
                    var site = await _context.Site.FindAsync(model.SiteId.Value);
                    if (site != null)
                    {
                        model.SiteName = site.Name;
                    }
                }

                // Reload guide availability
                var availability = await _context.GuideAvailabilities
                    .Where(a => a.GuideId == model.GuideId && a.IsAvailable)
                    .OrderBy(a => a.DayOfWeek)
                    .ToListAsync();

                model.GuideAvailability = availability;

                return View(model);
            }

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            try
            {
                // Create booking
                var booking = new Booking
                {
                    ClientId = userId,
                    GuideId = model.GuideId,
                    SiteId = model.SiteId,
                    TourDateTime = model.TourDateTime,
                    BookingDate = DateTime.UtcNow,
                    GroupSize = model.GroupSize,
                    Status = "Pending",
                    Notes = model.Notes
                };

                _context.Booking.Add(booking);
                await _context.SaveChangesAsync();

                // Create initial message to guide
                var message = new Message
                {
                    SenderId = userId,
                    SenderType = "Client",
                    RecipientId = model.GuideId,
                    RecipientType = "Guide",
                    Content = $"Hello, I've booked a tour with you for {model.TourDateTime.ToString("MMMM dd, yyyy")} at {model.TourDateTime.ToString("h:mm tt")}.\n\nGroup size: {model.GroupSize}\n\nAdditional notes: {model.Notes}",
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} created booking {BookingId} with guide {GuideId}", userId, booking.Id, model.GuideId);

                TempData["SuccessMessage"] = "Your booking has been submitted successfully. The guide will contact you shortly to confirm the details.";
                return RedirectToAction("Confirmation", new { id = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking for user {UserId} with guide {GuideId}", userId, model.GuideId);
                ModelState.AddModelError("", "An error occurred while creating your booking. Please try again later.");

                return View(model);
            }
        }

        // GET: Booking/Confirmation/5
        public async Task<IActionResult> Confirmation(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var booking = await _context.Booking
                .Include(b => b.Guide)
                .Include(b => b.Site)
                .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Booking/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var bookings = await _context.Booking
                .Include(b => b.Guide)
                .Include(b => b.Site)
                .Where(b => b.ClientId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        // POST: Booking/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var booking = await _context.Booking
                .Include(b => b.Guide)
                .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            // Only allow cancellation if status is Pending or Confirmed
            if (booking.Status == "Pending" || booking.Status == "Confirmed")
            {
                booking.Status = "Cancelled";
                await _context.SaveChangesAsync();

                // Notify guide of cancellation
                var message = new Message
                {
                    SenderId = userId,
                    SenderType = "Client",
                    RecipientId = booking.GuideId,
                    RecipientType = "Guide",
                    Content = $"I've cancelled my booking for {booking.TourDateTime.ToString("MMMM dd, yyyy")} at {booking.TourDateTime.ToString("h:mm tt")}.",
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your booking has been cancelled.";
            }
            else
            {
                TempData["ErrorMessage"] = "This booking cannot be cancelled.";
            }

            return RedirectToAction("MyBookings");
        }
        // GET: Booking/GetGuideAvailability?guideId=5
        [HttpGet]
        public async Task<IActionResult> GetGuideAvailability(int guideId)
        {
            try
            {
                var availability = await _context.GuideAvailabilities
                    .Where(a => a.GuideId == guideId)
                    .OrderBy(a => a.DayOfWeek)
                    .ToListAsync();

                // Return the availability as JSON
                return Json(availability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guide availability for guide ID {GuideId}", guideId);
                return StatusCode(500, new { error = "Failed to load guide availability" });
            }
        }

        [Authorize]
        // GET: Booking/Request/5?siteId=10
        public async Task<IActionResult> Request(int id, int? siteId)
        {
            // Check if the user is a guide
            bool isGuide = User.IsInRole("Guide");

            if (isGuide)
            {
                string userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                // Get the guide being booked
                var guideCheck = await _context.Guide
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (guideCheck == null)
                {
                    return NotFound();
                }

                // Check if the guide is trying to book themselves
                if (guideCheck.Email == userEmail)
                {
                    TempData["ErrorMessage"] = "You cannot book yourself as a guide.";
                    return RedirectToAction("Index", "Home");
                }

                // Guides can't book other guides
                TempData["ErrorMessage"] = "Guides cannot book other guides.";
                return RedirectToAction("Index", "Home");
            }

            // Get the guide
            var guide = await _context.Guide
                .Include(g => g.City)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (guide == null)
            {
                return NotFound();
            }

            // Get the site if specified
            Site site = null;
            if (siteId.HasValue)
            {
                site = await _context.Site
                    .Include(s => s.City)
                    .FirstOrDefaultAsync(s => s.Id == siteId.Value);
            }

            // Get guide availability
            var availability = await _context.GuideAvailabilities
                .Where(a => a.GuideId == id && a.IsAvailable)
                .OrderBy(a => a.DayOfWeek)
                .ToListAsync();

            // Create available time slots for the next 14 days
            var availableSlots = GenerateAvailableTimeSlots(availability, 14);

            // Create booking request view model
            var viewModel = new BookingRequestViewModel
            {
                GuideId = guide.Id,
                GuideName = guide.FullName,
                GuideCity = guide.City.Name,
                SiteId = siteId,
                SiteName = site?.Name,
                AvailableTimeSlots = availableSlots,
                AvailableDates = availableSlots.Select(s => s.StartTime.Date).Distinct().OrderBy(d => d).ToList()
            };

            return View(viewModel);
        }


        // Helper method to generate available time slots based on guide availability
        private List<TimeSlot> GenerateAvailableTimeSlots(List<GuideAvailability> availability, int daysToShow)
        {
            var slots = new List<TimeSlot>();
            var today = DateTime.Today;

            // Generate slots for the next X days
            for (int day = 1; day <= daysToShow; day++)
            {
                var date = today.AddDays(day);
                var dayOfWeek = date.DayOfWeek;

                // Find availability for this day of week
                var dayAvailability = availability.FirstOrDefault(a => a.DayOfWeek == dayOfWeek && a.IsAvailable);

                if (dayAvailability != null)
                {
                    // Create time slots at hourly intervals
                    var startTime = new DateTime(date.Year, date.Month, date.Day,
                        dayAvailability.StartTime.Hours, dayAvailability.StartTime.Minutes, 0);
                    var endTime = new DateTime(date.Year, date.Month, date.Day,
                        dayAvailability.EndTime.Hours, dayAvailability.EndTime.Minutes, 0);

                    for (var time = startTime; time < endTime; time = time.AddHours(2))
                    {
                        slots.Add(new TimeSlot
                        {
                            SlotId = Guid.NewGuid().ToString(),
                            StartTime = time,
                            EndTime = time.AddHours(2),
                            IsAvailable = true
                        });
                    }
                }
            }

            return slots;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Request(BookingRequestViewModel model)
        {

            // Add detailed logging at the start
            _logger.LogInformation("Booking request received. GuideId: {GuideId}, SiteId: {SiteId}, SelectedTimeSlot: {TimeSlot}, GroupSize: {GroupSize}",
                model.GuideId, model.SiteId, model.SelectedTimeSlot, model.GroupSize);

            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("User not authenticated while attempting to make booking");
                return RedirectToAction("Login", "Account");
            }

            // Get user ID early
            int userId;
            try
            {
                userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("Retrieved userId {UserId} for booking", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user ID for booking");
                ModelState.AddModelError("", "User authentication error. Please try logging in again.");

                // Redirect to login instead of trying to reload the view
                TempData["ErrorMessage"] = "Authentication error. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // Check basic model validity
            if (string.IsNullOrEmpty(model.SelectedTimeSlot))
            {
                _logger.LogWarning("No time slot selected");
                ModelState.AddModelError("SelectedTimeSlot", "Please select a date and time for your booking");
            }

            if (model.GroupSize <= 0 || model.GroupSize > 20)
            {
                _logger.LogWarning("Invalid group size: {GroupSize}", model.GroupSize);
                ModelState.AddModelError("GroupSize", "Group size must be between 1 and 20");
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogWarning("Model validation failed for booking: {Errors}", errors);

                // Regenerate the view with all needed data
                return await RegenerateRequestView(model);
            }

            try
            {
                // Simple datetime parsing - try direct parse first
                DateTime tourDateTime;
                string timeSlotValue = model.SelectedTimeSlot;

                _logger.LogInformation("Attempting to parse time slot value: '{TimeSlot}'", timeSlotValue);

                // First approach: Split by the pipe character if it exists
                if (timeSlotValue.Contains("|"))
                {
                    string[] parts = timeSlotValue.Split('|');
                    timeSlotValue = parts[0].Trim();
                    _logger.LogInformation("Extracted date/time part: '{TimeSlotPart}'", timeSlotValue);
                }

                // Try parsing with various formats
                string[] formats = {
    "yyyy-MM-dd HH:mm:ss", // 2025-05-03 11:00:00
    "yyyy-MM-dd H:mm:ss",  // 2025-05-03 1:00:00
    "yyyy-MM-dd HH:mm",    // 2025-05-03 11:00
    "yyyy-MM-dd H:mm",     // 2025-05-03 1:00
    "M/d/yyyy HH:mm:ss",   // 5/3/2025 11:00:00
    "M/d/yyyy H:mm:ss",    // 5/3/2025 1:00:00
    "M/d/yyyy HH:mm",      // 5/3/2025 11:00
    "M/d/yyyy H:mm",       // 5/3/2025 1:00
    "yyyy-MM-ddTHH:mm:ss", // 2025-05-03T11:00:00
    "yyyy-MM-dd"           // 2025-05-03 (default to midnight)
};

                // Try standard DateTime.Parse first
                if (DateTime.TryParse(timeSlotValue, out tourDateTime))
                {
                    _logger.LogInformation("Successfully parsed tour date/time using standard parsing: {TourDateTime}", tourDateTime);
                }
                // Try each custom format
                else if (DateTime.TryParseExact(timeSlotValue, formats,
                         System.Globalization.CultureInfo.InvariantCulture,
                         System.Globalization.DateTimeStyles.None, out tourDateTime))
                {
                    _logger.LogInformation("Successfully parsed tour date/time using format parsing: {TourDateTime}", tourDateTime);
                }
                // If all else fails
                else
                {
                    _logger.LogError("Failed to parse date/time from '{TimeSlot}' after trying all formats", timeSlotValue);

                    // Log some additional information for debugging
                    _logger.LogInformation("Time slot raw value: '{RawValue}'", model.SelectedTimeSlot);
                    _logger.LogInformation("Time slot type: {Type}", model.SelectedTimeSlot?.GetType().FullName ?? "null");
                    _logger.LogInformation("Time slot length: {Length}", model.SelectedTimeSlot?.Length.ToString() ?? "n/a");

                    ModelState.AddModelError("SelectedTimeSlot", "Invalid date/time format. Please select a valid time slot.");
                    return await RegenerateRequestView(model);
                }

                // Check if the date is in the past
                if (tourDateTime < DateTime.Now)
                {
                    _logger.LogWarning("Attempted to book a tour in the past: {TourDateTime}", tourDateTime);
                    ModelState.AddModelError("SelectedTimeSlot", "You cannot book a tour in the past");
                    return await RegenerateRequestView(model);
                }

                _logger.LogInformation("Final parsed tour date/time: {TourDateTime}", tourDateTime);
                // Create a new booking with explicit field assignments
                var booking = new Booking();
                booking.ClientId = userId;
                booking.GuideId = model.GuideId;
                booking.SiteId = model.SiteId;
                booking.TourDateTime = tourDateTime;
                booking.BookingDate = DateTime.UtcNow;
                booking.GroupSize = model.GroupSize;
                booking.Status = "Pending";
                booking.Notes = model.Notes ?? string.Empty;

                _logger.LogInformation("Created booking object: ClientId={ClientId}, GuideId={GuideId}, SiteId={SiteId}, TourDateTime={TourDateTime}",
                    booking.ClientId, booking.GuideId, booking.SiteId, booking.TourDateTime);

                // CRITICAL FIX: Ensure we're explicitly adding to the DbSet
                _context.Booking.Add(booking);

                // Save the booking to the database immediately
                int bookingSaveCount = await _context.SaveChangesAsync();
                _logger.LogInformation("Booking saved to database. Affected records: {Count}", bookingSaveCount);

                if (bookingSaveCount <= 0)
                {
                    _logger.LogError("Database reported no records affected when saving booking");
                    ModelState.AddModelError("", "Failed to save booking to database");
                    return await RegenerateRequestView(model);
                }

                // Now that we have a booking ID, create the message
                var message = new Message();
                message.SenderId = userId;
                message.SenderType = "Client";
                message.RecipientId = model.GuideId;
                message.RecipientType = "Guide";
                message.Content = $"Hello, I've requested a tour with you for {tourDateTime.ToString("MMMM dd, yyyy")} at {tourDateTime.ToString("h:mm tt")}.\n\nGroup size: {model.GroupSize}\n\nAdditional notes: {model.Notes}";
                message.SentAt = DateTime.UtcNow;
                message.IsRead = false;

                _context.Messages.Add(message);
                int messageSaveCount = await _context.SaveChangesAsync();
                _logger.LogInformation("Message saved to database. Affected records: {Count}", messageSaveCount);

                _logger.LogInformation("User {UserId} created booking {BookingId} with guide {GuideId}", userId, booking.Id, model.GuideId);

                TempData["SuccessMessage"] = "Your booking request has been submitted successfully. The guide will contact you to confirm the details.";
                return RedirectToAction("Confirmation", new { id = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking for user {UserId} with guide {GuideId}: {ErrorMessage}",
                    userId, model.GuideId, ex.Message);

                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionMessage}", ex.InnerException.Message);
                }

                ModelState.AddModelError("", "An error occurred while creating your booking. Please try again later.");
                return await RegenerateRequestView(model);
            }
        }


        // Helper method to regenerate the view with all necessary data
        private async Task<IActionResult> RegenerateRequestView(BookingRequestViewModel model)
        {
            try
            {
                // Reload guide
                var guide = await _context.Guide
                    .Include(g => g.City)
                    .FirstOrDefaultAsync(g => g.Id == model.GuideId);

                if (guide == null)
                {
                    _logger.LogWarning("Guide not found when regenerating view for ID {GuideId}", model.GuideId);
                    return NotFound("Guide not found");
                }

                // Get the site if specified
                if (model.SiteId.HasValue)
                {
                    var site = await _context.Site
                        .FirstOrDefaultAsync(s => s.Id == model.SiteId.Value);
                    model.SiteName = site?.Name;
                }

                model.GuideName = guide.FullName;
                model.GuideCity = guide.City.Name;

                // Regenerate available time slots
                var availability = await _context.GuideAvailabilities
                    .Where(a => a.GuideId == model.GuideId && a.IsAvailable)
                    .OrderBy(a => a.DayOfWeek)
                    .ToListAsync();

                var availableSlots = GenerateAvailableTimeSlots(availability, 14);
                model.AvailableTimeSlots = availableSlots;
                model.AvailableDates = availableSlots.Select(s => s.StartTime.Date).Distinct().OrderBy(d => d).ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating request view");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}