using ITValet.HelpingClasses;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<User> User { get; set; }
        public DbSet<UserExperience> UserExperience { get; set; }
        public DbSet<UserSocialProfile> UserSocialProfile { get; set; }
        public DbSet<UserTag> UserTag { get; set; }
        public DbSet<UserSkill> UserSkill { get; set; }
        public DbSet<UserAvailableSlot> UserAvailableSlot { get; set; }
        public DbSet<UserReferal> UserReferal { get; set; }
        public DbSet<UserRating> UserRating { get; set; }
        public DbSet<UserEducation> UserEducation { get; set; }
        public DbSet<RequestService> RequestService { get; set; }
        public DbSet<RequestServiceSkill> RequestServiceSkill { get; set; }
        public DbSet<Order> Order { get; set; }
        public DbSet<OrderReason> OrderReason { get; set; }
        public DbSet<DeliveredOrder> DeliveredOrder { get; set; }
        public DbSet<Message> Message { get; set; }
        public DbSet<OfferDetail> OfferDetail { get; set; }
        public DbSet<SearchLog> SearchLog { get; set; }
        public DbSet<Notification> Notification { get; set; }
        public DbSet<UserPackage> UserPackage { get; set; }
        public DbSet<Contact> Contact { get; set; }
        public DbSet<PayPalAccountInformation> PayPalAccount { get; set; }
        public DbSet<PayPalOrderCheckOut> PayPalOrderCheckOut { get; set; }
        public DbSet<PayPalPackagesCheckOut> PayPalPackagesCheckOut { get; set; }
        public DbSet<PayPalToValetTransactions> PayPalToValetTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FirstName = "Usman",
                    LastName = "Ali",
                    UserName = "Admin",
                    Contact = "00000000000",
                    Email = "usman@gmail.com",
                    Password = StringCipher.Encrypt("123"),
                    Gender = "Male",
                    Country = "Canada",
                    State = "Alberta",
                    City = "Alberta",
                    BirthDate = DateTime.Now.AddYears(-17),
                    Timezone = "Canada/Mountain",
                    Availability = 1,
                    Status = 1,
                    PricePerHour = Convert.ToDecimal(24.99),
                    Role = (int)EnumRoles.Admin,
                    IsActive = (int)EnumActiveStatus.Active,
                    CreatedAt = GeneralPurpose.DateTimeNow()
                },
                new User
                {
                    Id = 2,
                    FirstName = "Michael",
                    LastName = "Michael",
                    UserName = "Customer",
                    Contact = "00000000000",
                    Email = "customer@gmail.com",
                    Password = StringCipher.Encrypt("123"),
                    Gender = "Male",
                    Country = "Canada",
                    State = "Torronto",
                    City = "Torronto",
                    BirthDate = DateTime.Now.AddYears(-26),
                    Timezone = "Canada/Mountain",
                    Availability = 1,
                    Status = 1,
                    PricePerHour = Convert.ToDecimal(24.99),
                    Role = (int)EnumRoles.Customer,
                    IsActive = (int)EnumActiveStatus.Active,
                    CreatedAt = GeneralPurpose.DateTimeNow()
                },
                new User
                {
                    Id = 3,
                    FirstName = "Ian",
                    LastName = "Ian",
                    UserName = "Valet",
                    Contact = "00000000000",
                    Email = "ian@gmail.com",
                    Password = StringCipher.Encrypt("123"),
                    Gender = "Male",
                    Country = "Canada",
                    State = "Torronto",
                    City = "Torronto",
                    BirthDate = DateTime.Now.AddYears(-20),
                    Timezone = "Canada/Mountain",
                    Availability = 1,
                    Status = 1,
                    PricePerHour = Convert.ToDecimal(24.99),
                    Role = (int)EnumRoles.Valet,
                    IsActive = (int)EnumActiveStatus.Active,
                    CreatedAt = GeneralPurpose.DateTimeNow()
                }
            );
        }
    }
}
