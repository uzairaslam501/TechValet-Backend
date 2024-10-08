using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class UserAvailableSlot : BaseModel
    {
        public DateTime? DateTimeOfDay { get; set; }
        public int? Slot1 { get; set; }
        public int? Slot2 { get; set; }
        public int? Slot3 { get; set; }
        public int? Slot4 { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
