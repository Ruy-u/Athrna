using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class CreateBookingViewModel
    {
        // Guide information
        public int GuideId { get; set; }
        public string GuideName { get; set; }
        public string GuideCity { get; set; }

        // Site information (optional)
        public int? SiteId { get; set; }
        public string SiteName { get; set; }

        // Booking details
        [Required]
        [Display(Name = "Tour Date & Time")]
        [DataType(DataType.DateTime)]
        public DateTime TourDateTime { get; set; }

        [Required]
        [Display(Name = "Group Size")]
        [Range(1, 20, ErrorMessage = "Group size must be between 1 and 20 people")]
        public int GroupSize { get; set; } = 1;

        [Display(Name = "Additional Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }

        // Guide availability
        public List<GuideAvailability> GuideAvailability { get; set; }

        // Validation helper
        public string MinDate { get; set; }
    }
}