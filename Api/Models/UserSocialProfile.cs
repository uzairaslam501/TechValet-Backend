using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ITValet.Models
{
    public class UserSocialProfile : BaseModel
    {
        [Column(TypeName = "nvarchar(255)")]
        public string? Title { get; set; }
        [Column(TypeName = "nvarchar(4000)")]
        public string? Link { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
