using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class Rating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Client")]
        public int ClientId { get; set; }

        [Required]
        [ForeignKey("Site")]
        public int SiteId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Value { get; set; }

        public string Review { get; set; }

        // Navigation properties
        public virtual Client Client { get; set; }
        public virtual Site Site { get; set; }
    }
}
