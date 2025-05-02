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

        // GET: Booking/Create/5 (5 is the guide ID)
        public async Task<IActionResult> Create(int id, int? siteId)
        {
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

                _context.Bookings.Add(booking);
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

            var booking = await _context.Bookings
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

            var bookings = await _context.Bookings
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

            var booking = await _context.Bookings
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

        // GET: Booking/Request/5?siteId=10
        public async Task<IActionResult> Request(int id, int? siteId)
        {
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
        public async Task<IActionResult> BookTour(int? cityId)
        {
            // Get all cities for filtering
            var cities = await _context.City.OrderBy(c => c.Name).ToListAsync();

            // Get guides, filtered by city if specified
            var guidesQuery = _context.Guide
                .Include(g => g.City)
                .AsQueryable();

            if (cityId.HasValue)
            {
                guidesQuery = guidesQuery.Where(g => g.CityId == cityId.Value);
            }

            var guides = await guidesQuery.ToListAsync();

            // Create view model
            var viewModel = new BookTourViewModel
            {
                Cities = cities,
                Guides = guides,
                SelectedCityId = cityId
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

        // POST: Booking/Request
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

            // Log model state validity
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogWarning("Model validation failed for booking: {Errors}", errors);

                // Reload guide and site details on validation failure
                var guide = await _context.Guide
                    .Include(g => g.City)
                    .FirstOrDefaultAsync(g => g.Id == model.GuideId);

                if (guide == null)
                {
                    _logger.LogWarning("Guide not found for ID {GuideId}", model.GuideId);
                    return NotFound();
                }

                // Get the site if specified
                Site site = null;
                if (model.SiteId.HasValue)
                {
                    site = await _context.Site
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
                return View(model);
            }

            try
            {
                // Log the selected time slot for debugging
                _logger.LogInformation("Selected time slot: {TimeSlot}", model.SelectedTimeSlot);

                // Parse selected time slot to get date and time
                var selectedSlotParts = model.SelectedTimeSlot.Split('|');
                _logger.LogInformation("Split time slot into {PartCount} parts: {Parts}",
                    selectedSlotParts.Length, string.Join(", ", selectedSlotParts));

                if (selectedSlotParts.Length != 2)
                {
                    _logger.LogWarning("Invalid time slot format: {TimeSlot}", model.SelectedTimeSlot);
                    ModelState.AddModelError("SelectedTimeSlot", "Invalid time slot selected");
                    return View(model);
                }

                DateTime tourDateTime;
                if (!DateTime.TryParse(selectedSlotParts[0], out tourDateTime))
                {
                    _logger.LogWarning("Failed to parse date/time: {DateTimeString}", selectedSlotParts[0]);
                    ModelState.AddModelError("SelectedTimeSlot", "Invalid date/time format");
                    return View(model);
                }

                _logger.LogInformation("Parsed tour date/time: {TourDateTime}", tourDateTime);

                // Create booking - log before saving
                var booking = new Booking
                {
                    ClientId = userId,
                    GuideId = model.GuideId,
                    SiteId = model.SiteId,
                    TourDateTime = tourDateTime,
                    BookingDate = DateTime.UtcNow,
                    GroupSize = model.GroupSize,
                    Status = "Pending",
                    Notes = model.Notes ?? ""
                };

                _logger.LogInformation("Created booking object: ClientId={ClientId}, GuideId={GuideId}, SiteId={SiteId}, TourDateTime={TourDateTime}",
                    booking.ClientId, booking.GuideId, booking.SiteId, booking.TourDateTime);

                // Log DbContext state for debugging
                _logger.LogInformation("Adding booking to context");
                _context.Bookings.Add(booking);

                _logger.LogInformation("Saving booking to database");
                var saveResult = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChangesAsync result: {SaveResult} records affected", saveResult);

                // Create initial message to guide
                var message = new Message
                {
                    SenderId = userId,
                    SenderType = "Client",
                    RecipientId = model.GuideId,
                    RecipientType = "Guide",
                    Content = $"Hello, I've requested a tour with you for {tourDateTime.ToString("MMMM dd, yyyy")} at {tourDateTime.ToString("h:mm tt")}.\n\nGroup size: {model.GroupSize}\n\nAdditional notes: {model.Notes}",
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _logger.LogInformation("Adding message to context");
                _context.Messages.Add(message);

                _logger.LogInformation("Saving message to database");
                var messageSaveResult = await _context.SaveChangesAsync();
                _logger.LogInformation("Message SaveChangesAsync result: {SaveResult} records affected", messageSaveResult);

                _logger.LogInformation("User {UserId} created booking {BookingId} with guide {GuideId}", userId, booking.Id, model.GuideId);

                TempData["SuccessMessage"] = "Your booking request has been submitted successfully. The guide will contact you to confirm the details.";
                _logger.LogInformation("Redirecting to Confirmation page for booking {BookingId}", booking.Id);
                return RedirectToAction("Confirmation", new { id = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking for user {UserId} with guide {GuideId}", userId, model.GuideId);
                ModelState.AddModelError("", "An error occurred while creating your booking. Please try again later.");

                // Reload necessary data for the view
                var guide = await _context.Guide
                    .Include(g => g.City)
                    .FirstOrDefaultAsync(g => g.Id == model.GuideId);

                if (guide == null)
                {
                    return NotFound();
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
        }
    }
}