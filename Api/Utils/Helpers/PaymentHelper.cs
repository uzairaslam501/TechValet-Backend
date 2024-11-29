using ITValet.HelpingClasses;
using PayPal.Api;

namespace ITValet.Utils.Helpers
{
    public static class PayPalPaymentHelper
    {
        public static PayPalPaymentResponse CreatePaymentRequest(
            PayPalOrderCheckOutViewModel orderDto,
            string reactUrl,
            string clientId,
            string clientSecret)
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
                        return_url = $"{reactUrl}PaymentSuccess",
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

}
