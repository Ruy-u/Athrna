namespace Athrna.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalSites { get; set; }
        public int TotalUsers { get; set; }
        public int TotalBookmarks { get; set; }
        public int TotalRatings { get; set; }
        public IEnumerable<Client> RecentUsers { get; set; }
        public IEnumerable<Rating> RecentRatings { get; set; }
    }
}
