using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class OfferDetail : BaseModel
    {
        public string? OfferTitle { get; set; }
        public string? OfferDescription { get; set; }
        public double? OfferPrice { get; set; }
        public string? TransactionFee { get; set; }
        public DateTime? StartedDateTime { get; set; }
        public DateTime? EndedDateTime { get; set; }
        public int? OfferStatus { get; set; }
        public int? CustomerId { get; set; }
        public int? ValetId { get; set; }
        public int? OrderId { get; set; }

        [ForeignKey("Message")]
        public int? MessageId { get; set; }
        public Message? Message { get; set; }
    }
}
