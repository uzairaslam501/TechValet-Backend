using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class UserSkill : BaseModel
    {
        [Column(TypeName = "nvarchar(255)")]
        public string? SkillName { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
