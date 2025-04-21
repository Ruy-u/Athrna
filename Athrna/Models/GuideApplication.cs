using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Athrna.Models
{
    public class GuideApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        [Required]
        [ForeignKey("City")]
        public int CityId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Required]
        public string NationalId { get; set; } = "";

        [Required]
        public string LicenseNumber { get; set; } = "";

        [Required]
        public ApplicationStatus Status { get; set; }

        public string? RejectionReason { get; set; }

        public DateTime SubmissionDate { get; set; }

        public DateTime? ReviewDate { get; set; }

        [NotMapped] // This tells EF not to map this to the database
        public string SafeRejectionReason => RejectionReason ?? "";

        // Navigation property - only relates to City for the selected city
        public virtual City City { get; set; }

        // Constructor to initialize properties
        public GuideApplication()
        {
            // Set default values
            Status = ApplicationStatus.Pending;
            SubmissionDate = DateTime.UtcNow;
            RejectionReason = "";
            Username = "";
            Email = "";
            Password = "";
            FullName = "";
            NationalId = "";
            LicenseNumber = "";
        }
    }

    public enum ApplicationStatus
    {
        [Display(Name = "Pending Review")]
        Pending,

        [Display(Name = "Approved")]
        Approved,

        [Display(Name = "Rejected")]
        Rejected
    }
}