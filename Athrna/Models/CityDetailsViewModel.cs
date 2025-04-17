namespace Athrna.Models
{
    public class CityDetailsViewModel
    {
        public City City { get; set; }
        public IEnumerable<Site> Sites { get; set; }
        public IEnumerable<Guide> Guides { get; set; }
    }
}
