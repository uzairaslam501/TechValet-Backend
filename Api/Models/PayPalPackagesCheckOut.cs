using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ITValet.Models
{
    public class PayPalPackagesCheckOut : BaseModel
    {
        public int ClientId { get; set; }
        public int UserPackageId { get; set; }
        public string? PackageType { get; set; }
        public string? PayableAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal PackagePrice { get; set; }
        public string? Currency { get; set; }
        public string? PaymentId { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? StartDate { get; set; }
    }
}
