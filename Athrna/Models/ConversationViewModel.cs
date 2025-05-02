namespace Athrna.Models
{
    public class ConversationViewModel
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public int GuideId { get; set; }
        public string GuideName { get; set; }
        public Message LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
}
