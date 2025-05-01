namespace Athrna.Models
{
    public class GuideDashboardViewModel
    {
        public Guide Guide { get; set; }
        public List<Booking> RecentBookings { get; set; }
        public int UnreadMessages { get; set; }
        public int PendingBookings { get; set; }
        public int CompletedTours { get; set; }
        public double AverageRating { get; set; }
    }
}
