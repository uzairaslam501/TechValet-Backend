using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITValet.Models
{
    public class User: BaseModel
    {
        [Column(TypeName = "nvarchar(255)")]
        public string? FirstName { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? LastName { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? UserName { get; set; }
        [Column(TypeName = "nvarchar(4000)")]
        public string? Description { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? Email { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? Contact { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string? Password { get; set; }
        public int? Role { get; set; }
        public DateTime? BirthDate { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string? ProfilePicture { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? Country { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? State { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? ZipCode { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? City { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? Gender { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? Timezone { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? Language { get; set; }
        public int? Availability { get; set; }
        public int? IsVerify_StripeAccount { get; set; }
        public int? IsBankAccountAdded { get; set; }
        public int? IsPayPalAccount { get; set; }
        public int? Status { get; set; }
        public int? StarsCount { get; set; }
        public decimal? AverageRating { get; set; }
        public int? HST { get; set; }
        [Column(TypeName = "nvarchar(255)")]
        public string? StripeId { get; set; }
        public decimal? PricePerHour { get; set; }
        public List<UserExperience>? Experience { get; set; }
        public List<UserSocialProfile>? SocialProfile { get; set; }
        public List<UserTag>? UserTag { get; set; }
        public List<UserSkill>? UserSkill { get; set; }
        public List<UserAvailableSlot>? UsersAvailableSlot { get; set; }
        public List<UserPackage>? UserPackage { get; set; }
        public PayPalAccountInformation? PayPalAccountInformation { get; set; }
    }
}
