using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class UserEducation : BaseModel
    {
        [Column(TypeName = "nvarchar(255)")]
        public string? DegreeName { get; set; }
        [Column(TypeName = "nvarchar(1000)")]
        public string? InstituteName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate{ get; set; }
        
        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
