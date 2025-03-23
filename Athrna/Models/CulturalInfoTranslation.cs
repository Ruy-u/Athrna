using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Athrna.Models
{
    public class CulturalInfoTranslation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("CulturalInfo")]
        public int CulturalInfoId { get; set; }

        [Required]
        [ForeignKey("Language")]
        public int LanguageId { get; set; }

        public string TranslatedSummary { get; set; }

        // Navigation properties
        public virtual CulturalInfo CulturalInfo { get; set; }
        public virtual Language Language { get; set; }
    }
}
