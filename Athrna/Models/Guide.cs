using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class Guide
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("City")]
        public int CityId { get; set; } 

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

        // Navigation property updated
        public virtual City City { get; set; }  
    }
}
