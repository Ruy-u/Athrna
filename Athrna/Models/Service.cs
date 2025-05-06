using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Site")]
        public int SiteId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [StringLength(50)]
        public string IconName { get; set; } // Bootstrap icon name

        // Navigation property
        public virtual Site Site { get; set; }
    }
}
