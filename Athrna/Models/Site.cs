using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System;

namespace Athrna.Models
{
    public class Site
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("City")]
        public int CityId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Location { get; set; }

        public string Description { get; set; }

        public string SiteType { get; set; }

        // Navigation properties
        public virtual City City { get; set; }
        public virtual ICollection<Rating> Ratings { get; set; }
        public virtual ICollection<Bookmark> Bookmarks { get; set; }
        public virtual ICollection<Service> Services { get; set; }
        public virtual CulturalInfo CulturalInfo { get; set; }
        [Display(Name = "Image")]
        public string ImagePath { get; set; }
    }
}
