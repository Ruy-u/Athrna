using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class Guide
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Site")]
        public int SiteId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        public string NationalId { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        // Navigation properties
        public virtual Site Site { get; set; }
    }
}
