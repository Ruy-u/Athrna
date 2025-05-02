namespace Athrna.Models
{
    public class TimeSlot
    {
        public string SlotId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAvailable { get; set; }
    }
}
