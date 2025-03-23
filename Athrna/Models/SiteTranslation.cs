using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class SiteTranslation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Site")]
        public int SiteId { get; set; }

        [Required]
        [ForeignKey("Language")]
        public int LanguageId { get; set; }

        [Required]
        [StringLength(100)]
        public string TranslatedName { get; set; }

        public string TranslatedDescription { get; set; }

        // Navigation properties
        public virtual Site Site { get; set; }
        public virtual Language Language { get; set; }
    }
}
