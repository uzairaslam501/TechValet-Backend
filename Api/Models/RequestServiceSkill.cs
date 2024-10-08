using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class RequestServiceSkill : BaseModel
    {
        [Column(TypeName = "nvarchar(500)")]
        public string? RequestServiceName { get; set; }
        public int? RequestServiceId { get; set; }
        public RequestService? RequestService { get; set; }
    }
}
