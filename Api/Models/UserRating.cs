using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class UserRating : BaseModel
    {
        [Column (TypeName = " nvarchar(1000)")]
        public string? Reviews { get; set; }
        public int? Stars { get; set; }
        public int? CustomerId { get; set; }
        public int? ValetId { get; set; }
        public int? OrderId { get; set; }
    }
}
