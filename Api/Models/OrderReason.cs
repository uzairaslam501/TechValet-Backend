using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class OrderReason : BaseModel
    {
        [Column(TypeName = "nvarchar(2000)")]
        public string? ReasonExplanation { get; set; }
        public int? ReasonType { get; set; } //1 For Extended Reason, 2 For Revision Reason, 3 For Cancel Reason
        public int? OrderId { get; set;}
        public Order? Order { get; set;}
    }
}
