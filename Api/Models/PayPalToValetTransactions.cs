using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ITValet.Models
{
    public class PayPalToValetTransactions : BaseModel
    {
        public int OrderId { get; set; }
        public string? PaymentId { get; set; }
        public decimal? OrderPrice { get; set; }
        public decimal PlatformFee { get; set; }
        public string? RecipientEmail { get; set; }
        public decimal SentPayment { get; set; }
        public int ValetId { get; set; }
        public int CustomerId { get; set; }
        public string? BatchId { get; set; }
        public string? PayOutItemId { get; set; }
        public string? TransactionStatus { get; set; }
        public int OrderCheckOutId { get; set; }
        public bool CancelByAdmin { get; set; }
        public string? CancelationReason { get; set; }
        public string? CancelationStatus { get; set; }
        public string? ReturnedAmount { get; set; }
    }
}
