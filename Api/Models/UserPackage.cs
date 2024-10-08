using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class UserPackage : BaseModel
    {
        [Column(TypeName = "nvarchar(255)")]
        public string? PackageName { get; set; }
        public int? PackageType { get; set; } // 1 For Yearly 2 For 2 Years
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public int? TotalSessions { get; set; }

        public string? PaidBy { get; set; } // STRIPE  , PAYPAL
        public int? RemainingSessions { get; set; }

        [ForeignKey("CustomerId")]
        public int? CustomerId { get; set; }
        public User? User { get; set; }
    }
}
