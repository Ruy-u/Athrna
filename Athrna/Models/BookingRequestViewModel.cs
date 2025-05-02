using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class BookingRequestViewModel
    {
        // Guide information
        public int GuideId { get; set; }
        public string GuideName { get; set; }
        public string GuideCity { get; set; }

        // Site information (optional)
        public int? SiteId { get; set; }
        public string SiteName { get; set; }

        // Booking details
        [Required(ErrorMessage = "Please select a time slot")]
        [Display(Name = "Select Date & Time")]
        public string SelectedTimeSlot { get; set; }

        [Required(ErrorMessage = "Group size is required")]
        [Range(1, 20, ErrorMessage = "Group size must be between 1 and 20 people")]
        [Display(Name = "Group Size")]
        public int GroupSize { get; set; } = 1;

        [Display(Name = "Additional Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }

        // Available time slots
        public List<TimeSlot> AvailableTimeSlots { get; set; }

        // Available dates (for date selection)
        public List<DateTime> AvailableDates { get; set; }
    }
}
