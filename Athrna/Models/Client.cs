using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class Client
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        public string EncryptedPassword { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        // Navigation properties
        public virtual ICollection<Rating> Ratings { get; set; }
        public virtual ICollection<Bookmark> Bookmarks { get; set; }
        public virtual Administrator Administrator { get; set; }
    }
}
