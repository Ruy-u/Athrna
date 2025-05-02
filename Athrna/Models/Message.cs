namespace Athrna.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int SenderId { get; set; }
        public string SenderType { get; set; } // "Guide" or "Client"

        public int RecipientId { get; set; }
        public string RecipientType { get; set; } // "Guide" or "Client"

        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }

        // Navigation properties
        public virtual Client Client { get; set; }
        public virtual Guide Guide { get; set; }
    }
}
