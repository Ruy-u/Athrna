using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class City
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; }

        // Navigation properties
        public virtual ICollection<Site> Sites { get; set; }
        public virtual ICollection<Guide> Guides { get; set; } // Added this navigation property
    }
}
