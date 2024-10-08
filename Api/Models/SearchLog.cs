using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class SearchLog : BaseModel
    {
        [Column(TypeName = "nvarchar(255)")]
        public string? SearchKeyword { get; set; }
        public int? ValetProfileId { get; set; } //Incase if user searched the valet he can go to directly to that valet by just clicking the name
        public int? SearchKeywordCount { get; set; }
    }
}
