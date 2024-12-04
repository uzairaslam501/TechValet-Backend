using ITValet.HelpingClasses;
using ITValet.Models;
using PayPal.Api;

namespace ITValet.Utils.Helpers
{
    public static class PayPalPaymentHelper
    {
        public static PayPalPaymentResponse CreatePaymentRequest(
            PayPalOrderCheckOutViewModel orderDto,
            string reactUrl,
            string clientId,
            string clientSecret,
            string type)
        {
            try
            {
                var config = new Dictionary<string, string> { { "mode", "sandbox" } };
                var accessToken = new OAuthTokenCredential(clientId, clientSecret, config).GetAccessToken();
                var apiContext = new APIContext(accessToken);

                var payment = new Payment
                {
                    intent = "authorize",
                    payer = new Payer { payment_method = "paypal" },
                    redirect_urls = new RedirectUrls
                    {
                        return_url = $"{reactUrl}PaymentSuccess?type={type}",
                        cancel_url = $"{reactUrl}CancelOrder"
                    },
                    transactions = new List<Transaction>
                {
                    new Transaction
                    {
                        item_list = new ItemList
                        {
                            items = new List<Item>
                            {
                                new Item
                                {
                                    name = orderDto.OrderTitle,
                                    sku = "001",
                                    price = orderDto.TotalPrice.ToString("0.00"),
                                    currency = "CAD",
                                    quantity = "1"
                                }
                            }
                        },
                        amount = new Amount
                        {
                            currency = "CAD",
                            total = orderDto.TotalPrice.ToString("0.00")
                        },
                        description = orderDto.OrderDescription
                    }
                }
                };

                var createdPayment = payment.Create(apiContext);
                var approvalUrl = createdPayment.links
                    .FirstOrDefault(link => link.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase))?.href;

                return new PayPalPaymentResponse
                {
                    PaymentId = createdPayment.id,
                    ApprovalUrl = approvalUrl
                };
            }
            catch (Exception ex)
            {
                return new PayPalPaymentResponse { PaymentId = null, ApprovalUrl = null };
            }
        }

        public static Payment ExecutePayment(string paymentId, string payerID, IConfiguration configuration)
        {
            var config = new Dictionary<string, string> { { "mode", "sandbox" } };
            var accessToken = new OAuthTokenCredential(configuration["PayPal:ClientId"], configuration["PayPal:ClientSecret"], config).GetAccessToken();
            var apiContext = new APIContext(accessToken);

            var paymentExecution = new PaymentExecution { payer_id = payerID };
            return Payment.Execute(apiContext, paymentId, paymentExecution);
        }

        public static CaptureResponse CapturePayment(Payment executedPayment, IConfiguration configuration)
        {
            var authorizationId = executedPayment.transactions
                .FirstOrDefault()?.related_resources
                .FirstOrDefault()?.authorization?.id;

            if (string.IsNullOrEmpty(authorizationId)) return null;

            var config = new Dictionary<string, string> { { "mode", "sandbox" } };
            var accessToken = new OAuthTokenCredential(configuration["PayPal:ClientId"], configuration["PayPal:ClientSecret"], config).GetAccessToken();
            var apiContext = new APIContext(accessToken);

            var capture = new Capture
            {
                is_final_capture = true,
                amount = new Amount
                {
                    currency = "CAD",
                    total = executedPayment.transactions.FirstOrDefault()?.amount.total
                }
            };

            var capturedPayment = PayPal.Api.Authorization.Get(apiContext, authorizationId).Capture(apiContext, capture);

            return new CaptureResponse
            {
                State = capturedPayment.state,
                CaptureId = capturedPayment.id,
                AuthorizationId = authorizationId,
                TransactionFee = capturedPayment.transaction_fee?.value
            };
        }

        public static decimal CalculateOrderPrice(int sessions, decimal pricePerHour)
        {
            return sessions * pricePerHour;
        }

        public static int CalculateSessions(DateTime startDate, DateTime endDate, int sessionDurationInMinutes = 60)
        {
            TimeSpan duration = endDate - startDate;
            int totalMinutes = (int)duration.TotalMinutes;
            return (int)Math.Ceiling(totalMinutes / (double)sessionDurationInMinutes);
        }

        public static ResponseDto CreateErrorResponse(string message, string statusCode = "400")
        {
            return new ResponseDto { Status = false, StatusCode = statusCode, Message = message };
        }
    }

    public static class PackageDetailsExtensions
    {
        public static UserPackage ToUserPackage(this PackageDetails packageDetails)
        {
            return new UserPackage
            {
                PackageName = packageDetails.PackageName,
                StartDateTime = packageDetails.StartDate,
                EndDateTime = packageDetails.EndDate,
                TotalSessions = packageDetails.TotalSessions,
                RemainingSessions = packageDetails.RemainingSessions,
                PackageType = packageDetails.PackageType,
                PaidBy = "PAYPAL"
            };
        }

        public static PackageCheckOutViewModel ToPackageCheckOutViewModel(this PackageDetails packageDetails, int userPackageId)
        {
            return new PackageCheckOutViewModel
            {
                UserPackageId = userPackageId,
                PaymentId = packageDetails.PaymentId,
                PackagePrice = packageDetails.Price,
                StartDate = packageDetails.StartDate,
                EndDate = packageDetails.EndDate,
                PackageType = packageDetails.PackageName,
                ClientId = Convert.ToInt32(packageDetails.ClientId)
            };
        }
    }

    public class PayPalPaymentResponse
    {
        public string? PaymentId { get; set; }
        public string? ApprovalUrl { get; set; }
    }

    public class CaptureResponse
    {
        public string State { get; set; }
        public string CaptureId { get; set; }
        public string AuthorizationId { get; set; }
        public string TransactionFee { get; set; }
    }

    public class PackageDetails
    {
        public int Id { get; set; } // Unique identifier for the package
        public string PackageName { get; set; } // Name of the package (e.g., "1 Year", "2 Years")
        public string Description { get; set; } // Description of the package
        public decimal Price { get; set; } // Price of the package
        public DateTime StartDate { get; set; } // Start date of the package
        public DateTime EndDate { get; set; } // End date of the package
        public int TotalSessions { get; set; } // Total sessions included in the package
        public int RemainingSessions { get; set; } // Sessions remaining
        public string Currency { get; set; } // Currency (e.g., "CAD", "USD")
        public int PackageType { get; set; } // Type of package (e.g., 1 for 1 Year, 2 for 2 Years)
        public bool IsActive { get; set; } // Status of the package
        public string PaymentId { get; set; } // Associated payment ID
        public string ClientId { get; set; } // ID of the client purchasing the package
        public string PaymentStatus { get; set; } // Status of the payment (e.g., "completed", "pending")
    }


}
