namespace Athrna.Models
{
    public class BookTourViewModel
    {
        public IEnumerable<City> Cities { get; set; }
        public IEnumerable<Guide> Guides { get; set; }
        public int? SelectedCityId { get; set; }
    }
}
