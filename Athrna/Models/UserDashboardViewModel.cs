using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class UserDashboardViewModel
    {
        public Client User { get; set; }
        public IEnumerable<Bookmark> Bookmarks { get; set; }
        public IEnumerable<Rating> Ratings { get; set; }
        public IEnumerable<Booking> RecentBookings { get; set; }
        public int UnreadMessages { get; set; } = 0;
        public int PendingConversations { get; set; } = 0;
    }
}