using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace ITValet.Models
{
    public class Message : BaseModel
    {
        [Column(TypeName = "nvarchar(MAX)")]
        public string? MessageDescription { get; set; }
        public int? IsRead { get; set; }
        public string? FilePath { get; set; }
        
        [ForeignKey("User")]
        public int? SenderId { get; set; }
        public User? Sender { get; set; }

        [ForeignKey("User")]
        public int? ReceiverId { get; set; }
        public int? IsZoomMessage { get; set; }
        public User? Receiver { get; set; }
        public int? OrderId { get; set; }
        public int? OrderReasonId { get; set; }
        public OfferDetail? OfferDetails { get; set; }

    }
}
