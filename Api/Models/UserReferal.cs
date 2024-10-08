using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class UserReferal : BaseModel
    {
        [Column(TypeName = "nvarchar(255)")]
        public string? UserReferalCode { get; set; }
        public int? UserReferalType { get; set; }
        public int? ReferedByUser { get; set; }
        public int? RefferedId { get; set; }
    }
}
