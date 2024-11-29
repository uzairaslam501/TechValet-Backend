using ITValet.HelpingClasses;
using ITValet.Models;

namespace ITValet.Utils.Helpers
{
    public static class StripeHelper
    {
        public static Order InitializeOrder(CheckOutDTO checkOutData)
        {
            return new Order
            {
                OrderTitle = checkOutData.PaymentTitle,
                OrderDescription = checkOutData.PaymentDescription,
                StartDateTime = DateTime.Parse(checkOutData.FromDateTime),
                EndDateTime = DateTime.Parse(checkOutData.ToDateTime),
                ValetId = int.Parse(checkOutData.ValetId),
                CustomerId = int.Parse(checkOutData.customerId),
                OfferId = checkOutData.OfferId,
                PackageId = checkOutData.PackageId,
                IsActive = 0,
                OrderStatus = 0,
                IsDelivered = 0,
                OrderPrice = 0,
                TotalAmountIncludedFee = 0,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
        }


        public static UserPackage InitializePackage(PackageCOutRequest checkOut, out string packagePrice)
        {
            packagePrice = checkOut.SelectedPackage switch
            {
                "IYear" => "100",
                "2Year" => "200",
                _ => throw new ArgumentException("Invalid package selection")
            };

            var startDate = DateTime.Now;
            var endDate = startDate.AddYears(checkOut.SelectedPackage == "IYear" ? 1 : 2);

            return new UserPackage
            {
                StartDateTime = startDate,
                EndDateTime = endDate,
                PackageType = checkOut.SelectedPackage == "IYear" ? 1 : 2,
                PackageName = checkOut.SelectedPackage,
                TotalSessions = checkOut.SelectedPackage == "IYear" ? 6 : 12,
                RemainingSessions = checkOut.SelectedPackage == "IYear" ? 6 : 12,
                CustomerId = checkOut.ClientId
            };
        }
    }

    public class CreateCheckoutSessionResponse
    {
        public string? SessionId { get; set; }
        public string? CheckOutURL { get; set; }
        public string? PublicKey { get; set; }
        public string? PaymentTimeTicks { get; set; }
    }
}
