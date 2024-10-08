using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ITValet.Models
{
    public class Notification : BaseModel
    {
        [Column(TypeName = "nvarchar(255)")]
        public string? Title { get; set; }

        [Column(TypeName = "nvarchar(MAX)")]
        public string? Description { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string? Url { get; set; }
        public int? IsRead { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User? User { get; set; }
        public int? NotificationType { get; set; }
    }
}
