using System.ComponentModel.DataAnnotations;

namespace ITValet.Models
{
    public class FrequentlyAskedQuestion : BaseModel
    {
        [Required]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Anwer { get; set; } = string.Empty;

        public int? BlogId { get; set; }
        public int? CreatedBy { get; set; }
    }
}
