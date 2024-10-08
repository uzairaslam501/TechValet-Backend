using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class DeliveredOrder : BaseModel
    {
        [Column(TypeName = "nvarchar(2000)")]
        public string? OrderDescription { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string? FilePath { get; set; }
        public int? OrderId { get; set; }
        public int? CustomerId { get; set; }
        public int? ValetId { get; set; }

    }
}
