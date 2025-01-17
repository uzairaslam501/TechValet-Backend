using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class Blog : BaseModel
    {
        public string? Title { get; set; }

        public string? Description { get; set; }
        public string? Slug { get; set; }
        public string? Content { get; set; }
        public string? Image { get; set; } // FeaturedImageUrl
        public string? Tags { get; set; }
        public DateTime? PublishedDate { get; set; }
        public int? CreatedBy { get; set; }
        public string? Skill { get; set; }
    }
}
