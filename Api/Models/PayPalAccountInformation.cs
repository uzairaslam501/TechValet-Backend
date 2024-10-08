using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ITValet.Models
{
    public class PayPalAccountInformation : BaseModel
    {
        [ForeignKey("User")]
        public int? ValetId { get; set; }
        public User? User { get; set; }
        public string? PayPalEmail { get; set; }
        public bool IsPayPalAuthorized { get; set; }
      
    }
}
