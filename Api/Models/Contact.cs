using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class Contact : BaseModel
    {
        [Column(TypeName = "nvarchar(255)")]
        public string? Name { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? Email { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? Subject { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string? Message { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string? FilePath { get; set; }
    }
}
