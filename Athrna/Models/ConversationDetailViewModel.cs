namespace Athrna.Models
{
    public class ConversationDetailViewModel
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public List<Message> Messages { get; set; }
        public int GuideId { get; set; }
    }
}
