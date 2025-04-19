using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class FeedbackViewModel
    {
        [Required(ErrorMessage = "Please select a feedback type")]
        [Display(Name = "Feedback Type")]
        public FeedbackType Type { get; set; }

        [Required(ErrorMessage = "Please provide feedback details")]
        [Display(Name = "Details")]
        [MinLength(10, ErrorMessage = "Please provide more details (minimum 10 characters)")]
        [MaxLength(2000, ErrorMessage = "Feedback is too long (maximum 2000 characters)")]
        public string Details { get; set; }

        [Display(Name = "Page URL")]
        public string PageUrl { get; set; }

        [Display(Name = "Browser Information")]
        public string BrowserInfo { get; set; }

        [Display(Name = "Device Information")]
        public string DeviceInfo { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Contact Email (optional)")]
        public string Email { get; set; }

        [Display(Name = "Your Name (optional)")]
        [MaxLength(100, ErrorMessage = "Name is too long (maximum 100 characters)")]
        public string Name { get; set; }

        [Display(Name = "Allow Contact")]
        public bool AllowContact { get; set; }
    }

    public enum FeedbackType
    {
        [Display(Name = "General Feedback")]
        General,

        [Display(Name = "Bug Report")]
        Bug,

        [Display(Name = "Feature Request")]
        FeatureRequest,

        [Display(Name = "Content Issue")]
        ContentIssue,

        [Display(Name = "Accessibility Issue")]
        AccessibilityIssue,

        [Display(Name = "Performance Issue")]
        PerformanceIssue
    }
}
