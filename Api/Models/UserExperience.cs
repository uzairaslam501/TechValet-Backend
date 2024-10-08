using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class UserExperience : BaseModel
    {
        [Column(TypeName ="nvarchar(255)")]
        public string? Title { get; set; }
        [Column(TypeName = "nvarchar(4000)")]
        public string? Description { get; set; }
        public DateTime? ExperienceFrom { get; set; }
        public DateTime? ExperienceTo { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? Organization { get; set; }
        [Column(TypeName = "nvarchar(1000)")]
        public string? Website { get; set; }
        
        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
