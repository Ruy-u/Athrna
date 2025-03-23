using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class Administrator
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Client")]
        public int ClientId { get; set; }

        // Navigation properties
        public virtual Client Client { get; set; }
    }
}
