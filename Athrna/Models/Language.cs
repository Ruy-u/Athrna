using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class Language
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; }

        // Navigation properties
        public virtual ICollection<SiteTranslation> SiteTranslations { get; set; }
        public virtual ICollection<CulturalInfoTranslation> CulturalInfoTranslations { get; set; }
    }
}
