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

        // Add role field (1-5 where 1 is highest permission level)
        [Required]
        [Range(1, 5)]
        public int RoleLevel { get; set; } = 5; // Default to lowest permission level

        // Navigation properties
        public virtual Client Client { get; set; }
    }

    // Enum for admin role levels to make code more readable
    public enum AdminRoleLevel
    {
        SuperAdmin = 1,   // Full access
        SeniorAdmin = 2,  // All except admin management
        ContentManager = 3, // Site and content management
        UserManager = 4,  // User management
        Viewer = 5        // Read-only access
    }
}