using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class RequestService : BaseModel
    {
        [Column(TypeName ="nvarchar(255)")]
        public string? ServiceTitle { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? PrefferedServiceTime { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? CategoriesOfProblems { get; set; }
        [Column(TypeName = "nvarchar(2000)")]
        public string? ServiceDescription { get; set; }
        public DateTime? FromDateTime { get; set; }
        public DateTime? ToDateTime { get; set; }
        public string? RequestServiceSkills { get; set; }
        public string? ServiceLanguage { get; set; } // It can be multiple by , seperated
        public int? RequestedServiceUserId { get; set; }
        public int? RequestServiceType { get; set; } // 1 Live Now 2 Schedule Later
        public List<RequestServiceSkill>? RequestServiceSkill { get; set; }
    }
}
