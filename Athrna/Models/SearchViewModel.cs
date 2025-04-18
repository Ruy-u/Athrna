namespace Athrna.Models
{
    public class SearchViewModel
    {
        public string Query { get; set; }
        public IEnumerable<Site> Results { get; set; }
    }
}