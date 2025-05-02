namespace Athrna.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public int GuideId { get; set; }
        public int? SiteId { get; set; } // Optional - can be for a specific site or general

        public DateTime BookingDate { get; set; }
        public DateTime TourDateTime { get; set; }
        public int GroupSize { get; set; }
        public string Status { get; set; } // "Pending", "Confirmed", "Completed", "Cancelled"

        public string Notes { get; set; }

        // Navigation properties
        public virtual Client Client { get; set; }
        public virtual Guide Guide { get; set; }
        public virtual Site Site { get; set; }
    }
}
