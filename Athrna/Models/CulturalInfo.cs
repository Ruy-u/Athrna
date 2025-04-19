using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Athrna.Models
{
    public class CulturalInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Site")]
        public int SiteId { get; set; }

        public string Summary { get; set; }

        [DataType(DataType.Date)]
        public int EstablishedDate { get; set; }

        // Navigation properties
        public virtual Site Site { get; set; }
        public virtual ICollection<CulturalInfoTranslation> CulturalInfoTranslations { get; set; }
    }
}
