using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ITValet.Models
{
    public class PayPalOrderCheckOut : BaseModel
    {
        public int ClientId { get; set; }
        public int ValetId { get; set; }
        public string? CaptureId { get; set; }
        public string? AuthorizationId { get; set; }
        public int OrderId { get; set; }
        public string? PayableAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Currency { get; set; }
        public string? PaymentId { get; set; }
        public DateTime? PaymentTransmitDateTime { get; set; }
        public string? PayPalTransactionFee { get; set; }
        public bool? IsPaymentSentToValet { get; set; }
        public decimal? OrderPrice { get; set; }
        public string? PayPalAccount { get; set; }
        public bool IsRefund { get; set; }
        public bool? PaidByPackage { get; set; }
    }
}
