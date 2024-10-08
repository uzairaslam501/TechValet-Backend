namespace ITValet.Models
{
    public class Order : BaseModel
    {
        public string? OrderTitle { get; set; }
        public string? OrderDescription { get; set; }
        public decimal? OrderPrice { get; set; }
        public decimal? TotalAmountIncludedFee { get; set; }
        public int? OrderStatus { get; set; } // 0 In-Progress, 1 Accept, 2 Completed, 3 PendingPayment, 4 Cancelled
        public string? StripeChargeId { get; set; }
        public string? PayPalPaymentId { get; set; }
        public string? CapturedId { get; set; }  
        public int? StripeStatus { get; set; } // 1 Refunded, 2 PaymentReceived, 3 SentToValet, 4 PaymentFailedToSend
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public int? IsDelivered { get; set; }
        public int? RequestId { get; set; }
        public int? OfferId { get; set; }
        public int? CustomerId { get; set; }
        public int? ValetId { get; set; }
        public int? PackageId { get; set; }
        public string? PackageBuyFrom { get; set; }
        public List<OrderReason>? OrderReason { get; set; }
    }
}
