using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class Translation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string SourceLanguage { get; set; } = "en";

        [Required]
        [StringLength(10)]
        public string TargetLanguage { get; set; }

        [Required]
        public string OriginalText { get; set; }

        [Required]
        public string TranslatedText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Hash of the OriginalText to enable indexing for faster lookups
        [StringLength(100)]
        public string TextHash { get; set; }
    }
}
