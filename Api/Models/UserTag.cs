using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ITValet.Models
{
    public class UserTag : BaseModel
    {
        [Column(TypeName = "nvarchar(255)")]
        public string? TagName { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
