namespace Athrna.Models
{
    public class GuideAvailability
    {
        public int Id { get; set; }

        public int GuideId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }

        // Navigation property
        public virtual Guide Guide { get; set; }
    }
}
