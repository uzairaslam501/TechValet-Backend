using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITValet.Migrations
{
    public partial class _25092024 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contact",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveredOrder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderDescription = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ValetId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveredOrder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalAmountIncludedFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OrderStatus = table.Column<int>(type: "int", nullable: true),
                    StripeChargeId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayPalPaymentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CapturedId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StripeStatus = table.Column<int>(type: "int", nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDelivered = table.Column<int>(type: "int", nullable: true),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    OfferId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ValetId = table.Column<int>(type: "int", nullable: true),
                    PackageId = table.Column<int>(type: "int", nullable: true),
                    PackageBuyFrom = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayPalOrderCheckOut",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    ValetId = table.Column<int>(type: "int", nullable: false),
                    CaptureId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorizationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PayableAmount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentTransmitDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PayPalTransactionFee = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPaymentSentToValet = table.Column<bool>(type: "bit", nullable: true),
                    OrderPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PayPalAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRefund = table.Column<bool>(type: "bit", nullable: false),
                    PaidByPackage = table.Column<bool>(type: "bit", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPalOrderCheckOut", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayPalPackagesCheckOut",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    UserPackageId = table.Column<int>(type: "int", nullable: false),
                    PackageType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayableAmount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PackagePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPalPackagesCheckOut", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayPalToValetTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PaymentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PlatformFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentPayment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValetId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    BatchId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayOutItemId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderCheckOutId = table.Column<int>(type: "int", nullable: false),
                    CancelByAdmin = table.Column<bool>(type: "bit", nullable: false),
                    CancelationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CancelationStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnedAmount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPalToValetTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestService",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceTitle = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    PrefferedServiceTime = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    CategoriesOfProblems = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    ServiceDescription = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    FromDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestServiceSkills = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceLanguage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedServiceUserId = table.Column<int>(type: "int", nullable: true),
                    RequestServiceType = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestService", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchKeyword = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    ValetProfileId = table.Column<int>(type: "int", nullable: true),
                    SearchKeywordCount = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Contact = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<int>(type: "int", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProfilePicture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Timezone = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Availability = table.Column<int>(type: "int", nullable: true),
                    IsVerify_StripeAccount = table.Column<int>(type: "int", nullable: true),
                    IsBankAccountAdded = table.Column<int>(type: "int", nullable: true),
                    IsPayPalAccount = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true),
                    StarsCount = table.Column<int>(type: "int", nullable: true),
                    AverageRating = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HST = table.Column<int>(type: "int", nullable: true),
                    StripeId = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    PricePerHour = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRating",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reviews = table.Column<string>(type: " nvarchar(1000)", nullable: true),
                    Stars = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ValetId = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRating", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserReferal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserReferalCode = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    UserReferalType = table.Column<int>(type: "int", nullable: true),
                    ReferedByUser = table.Column<int>(type: "int", nullable: true),
                    RefferedId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReferal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderReason",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReasonExplanation = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    ReasonType = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderReason", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderReason_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RequestServiceSkill",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestServiceName = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    RequestServiceId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestServiceSkill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestServiceSkill_RequestService_RequestServiceId",
                        column: x => x.RequestServiceId,
                        principalTable: "RequestService",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Message",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageDescription = table.Column<string>(type: "nvarchar(MAX)", nullable: true),
                    IsRead = table.Column<int>(type: "int", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenderId = table.Column<int>(type: "int", nullable: true),
                    ReceiverId = table.Column<int>(type: "int", nullable: true),
                    IsZoomMessage = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    OrderReasonId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Message", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Message_User_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Message_User_SenderId",
                        column: x => x.SenderId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(MAX)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    IsRead = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    NotificationType = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notification_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PayPalAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ValetId = table.Column<int>(type: "int", nullable: true),
                    PayPalEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPayPalAuthorized = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPalAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayPalAccount_User_ValetId",
                        column: x => x.ValetId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserAvailableSlot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateTimeOfDay = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Slot1 = table.Column<int>(type: "int", nullable: true),
                    Slot2 = table.Column<int>(type: "int", nullable: true),
                    Slot3 = table.Column<int>(type: "int", nullable: true),
                    Slot4 = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAvailableSlot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAvailableSlot_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserEducation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DegreeName = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    InstituteName = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEducation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEducation_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserExperience",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", nullable: true),
                    ExperienceFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExperienceTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Organization = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExperience", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserExperience_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserPackage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageName = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    PackageType = table.Column<int>(type: "int", nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalSessions = table.Column<int>(type: "int", nullable: true),
                    PaidBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemainingSessions = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPackage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPackage_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserSkill",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkillName = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSkill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSkill_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserSocialProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Link = table.Column<string>(type: "nvarchar(4000)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSocialProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSocialProfile_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserTag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagName = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTag_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OfferDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfferTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OfferDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OfferPrice = table.Column<double>(type: "float", nullable: true),
                    TransactionFee = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OfferStatus = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ValetId = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    MessageId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferDetail_Message_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Message",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "Availability", "AverageRating", "BirthDate", "City", "Contact", "Country", "CreatedAt", "DeletedAt", "Description", "Email", "FirstName", "Gender", "HST", "IsActive", "IsBankAccountAdded", "IsPayPalAccount", "IsVerify_StripeAccount", "Language", "LastName", "Password", "PricePerHour", "ProfilePicture", "Role", "StarsCount", "State", "Status", "StripeId", "Timezone", "UpdatedAt", "UserName", "ZipCode" },
                values: new object[] { 1, 1, null, new DateTime(2007, 9, 25, 8, 45, 43, 870, DateTimeKind.Local).AddTicks(5929), "Alberta", "00000000000", "Canada", new DateTime(2024, 9, 25, 7, 45, 43, 870, DateTimeKind.Utc).AddTicks(5971), null, null, "usman@gmail.com", "Usman", "Male", null, 1, null, null, null, null, "Ali", "2wbYxvfTLTLxw8w3VJHu3+XuhrfrJgyWJuAkx/TmeAY=", 24.99m, null, 1, null, "Alberta", 1, null, "Canada/Mountain", null, "Admin", null });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "Availability", "AverageRating", "BirthDate", "City", "Contact", "Country", "CreatedAt", "DeletedAt", "Description", "Email", "FirstName", "Gender", "HST", "IsActive", "IsBankAccountAdded", "IsPayPalAccount", "IsVerify_StripeAccount", "Language", "LastName", "Password", "PricePerHour", "ProfilePicture", "Role", "StarsCount", "State", "Status", "StripeId", "Timezone", "UpdatedAt", "UserName", "ZipCode" },
                values: new object[] { 2, 1, null, new DateTime(1998, 9, 25, 8, 45, 43, 870, DateTimeKind.Local).AddTicks(6016), "Torronto", "00000000000", "Canada", new DateTime(2024, 9, 25, 7, 45, 43, 870, DateTimeKind.Utc).AddTicks(6021), null, null, "customer@gmail.com", "Michael", "Male", null, 1, null, null, null, null, "Michael", "1FVLWiQ0cZwpEJcDAxDmSn5Ry67+cVBFUP7iMgSQ7Aw=", 24.99m, null, 3, null, "Torronto", 1, null, "Canada/Mountain", null, "Customer", null });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "Availability", "AverageRating", "BirthDate", "City", "Contact", "Country", "CreatedAt", "DeletedAt", "Description", "Email", "FirstName", "Gender", "HST", "IsActive", "IsBankAccountAdded", "IsPayPalAccount", "IsVerify_StripeAccount", "Language", "LastName", "Password", "PricePerHour", "ProfilePicture", "Role", "StarsCount", "State", "Status", "StripeId", "Timezone", "UpdatedAt", "UserName", "ZipCode" },
                values: new object[] { 3, 1, null, new DateTime(2004, 9, 25, 8, 45, 43, 870, DateTimeKind.Local).AddTicks(6074), "Torronto", "00000000000", "Canada", new DateTime(2024, 9, 25, 7, 45, 43, 870, DateTimeKind.Utc).AddTicks(6078), null, null, "ian@gmail.com", "Ian", "Male", null, 1, null, null, null, null, "Ian", "JM8Zsm123c943H92zbGS7bPYiniHEhslFM0PuAtEHhg=", 24.99m, null, 4, null, "Torronto", 1, null, "Canada/Mountain", null, "Valet", null });

            migrationBuilder.CreateIndex(
                name: "IX_Message_ReceiverId",
                table: "Message",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_SenderId",
                table: "Message",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserId",
                table: "Notification",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferDetail_MessageId",
                table: "OfferDetail",
                column: "MessageId",
                unique: true,
                filter: "[MessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReason_OrderId",
                table: "OrderReason",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalAccount_ValetId",
                table: "PayPalAccount",
                column: "ValetId",
                unique: true,
                filter: "[ValetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RequestServiceSkill_RequestServiceId",
                table: "RequestServiceSkill",
                column: "RequestServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvailableSlot_UserId",
                table: "UserAvailableSlot",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEducation_UserId",
                table: "UserEducation",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserExperience_UserId",
                table: "UserExperience",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPackage_UserId",
                table: "UserPackage",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSkill_UserId",
                table: "UserSkill",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSocialProfile_UserId",
                table: "UserSocialProfile",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTag_UserId",
                table: "UserTag",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contact");

            migrationBuilder.DropTable(
                name: "DeliveredOrder");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "OfferDetail");

            migrationBuilder.DropTable(
                name: "OrderReason");

            migrationBuilder.DropTable(
                name: "PayPalAccount");

            migrationBuilder.DropTable(
                name: "PayPalOrderCheckOut");

            migrationBuilder.DropTable(
                name: "PayPalPackagesCheckOut");

            migrationBuilder.DropTable(
                name: "PayPalToValetTransactions");

            migrationBuilder.DropTable(
                name: "RequestServiceSkill");

            migrationBuilder.DropTable(
                name: "SearchLog");

            migrationBuilder.DropTable(
                name: "UserAvailableSlot");

            migrationBuilder.DropTable(
                name: "UserEducation");

            migrationBuilder.DropTable(
                name: "UserExperience");

            migrationBuilder.DropTable(
                name: "UserPackage");

            migrationBuilder.DropTable(
                name: "UserRating");

            migrationBuilder.DropTable(
                name: "UserReferal");

            migrationBuilder.DropTable(
                name: "UserSkill");

            migrationBuilder.DropTable(
                name: "UserSocialProfile");

            migrationBuilder.DropTable(
                name: "UserTag");

            migrationBuilder.DropTable(
                name: "Message");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "RequestService");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
