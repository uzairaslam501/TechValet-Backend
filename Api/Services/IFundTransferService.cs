using ITValet.HelpingClasses;
using ITValet.Models;
using PayoutsSdk.Core;
using PayoutsSdk.Payouts;
using PayPal.Api;
using Stripe;
using System.Net;

namespace ITValet.Services
{
    public interface IFundTransferService
    {
        Task<bool> TransferFundToValetAccount(ValetFundRecord valetObj);
        Task<bool> RefundPayment(string captureId, int orderId);
    }

    public class FundTransferService : IFundTransferService
    {
        private readonly IConfiguration _configuration;
        private readonly IPayPalGateWayService _payPalGateWayService;
        private readonly IOrderRepo _orderService;

        public FundTransferService(IConfiguration configuration, IPayPalGateWayService payPalGateWayService, IOrderRepo orderService)
        {       
            _configuration = configuration;
            _payPalGateWayService = payPalGateWayService;
            _orderService = orderService;
        }
        public async Task<bool> TransferFundToValetAccount(ValetFundRecord valetObj)
        {
            try
            {
                //Condition Just For Testing Purpose
                if(string.IsNullOrEmpty(valetObj.PayPalAccEmail))
                {
                    valetObj.PayPalAccEmail = "sb-kyag027007018@personal.example.com";
                }
                PayPalFundToValetViewModel fundObj = new PayPalFundToValetViewModel();
                var clientId = _configuration["PayPal:ClientId"];
                var clientSecret = _configuration["PayPal:ClientSecret"];
                var environment = new SandboxEnvironment(clientId, clientSecret);
                var client = new PayPalHttpClient(environment);

                //Calculate HST Fee
                decimal orderPrice = valetObj.OrderPrice ?? 0m;
                fundObj.PlatformFee = GeneralPurpose.CalculateHSTFee(orderPrice);

                decimal sentPayment = orderPrice - fundObj.PlatformFee;
                fundObj.SentPayment = sentPayment;
                fundObj.OrderPrice = valetObj.OrderPrice;

                var payoutItem = new PayoutsSdk.Payouts.PayoutItem
                {
                    RecipientType = "EMAIL",
                    Receiver = valetObj.PayPalAccEmail,
                    Amount = new PayoutsSdk.Payouts.Currency
                    {
                        Value = sentPayment.ToString("0.00"),
                        CurrencyCode = "USD"
                    }
                };

                var senderBatchHeader = new SenderBatchHeader()
                {
                    EmailSubject = "You have a payout against your ITValetOrder!",
                    SenderBatchId = "batch_" + Guid.NewGuid()
                };

                // Create a PayoutsPostRequest and set the request body
                var payoutRequest = new PayoutsPostRequest()
                    .RequestBody(new CreatePayoutRequest()
                    {
                        SenderBatchHeader = senderBatchHeader,
                        Items = new List<PayoutsSdk.Payouts.PayoutItem> { payoutItem }
                    });

                //Map values With ViewModel 
                fundObj.ValetId = valetObj.ValetId;
                fundObj.OrderId = valetObj.OrderId;
                //fundObj.PayPalAccEmail = valetObj.PayPalAccEmail;
                fundObj.PayPalAccEmail = valetObj.PayPalAccEmail;
                fundObj.ClientId = valetObj.ClientId;
                fundObj.OrderCheckOutId = valetObj.OrderCheckOutId;
                fundObj.PaymentId = valetObj.PaymentId;

                // Execute the Payouts API request
                var response = await client.Execute(payoutRequest);
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    // Retrieve the payout batch status
                    var batchId = response.Result<CreatePayoutResponse>().BatchHeader.PayoutBatchId;
                    fundObj.BatchId = batchId;
                    // Retrieve the transaction status for the individual item
                    var statusResponse = await CheckPaymentStatus(client, batchId);
                    fundObj.TransactionStatus = statusResponse.TransactionStatus;
                    fundObj.PayOutItemId = statusResponse.PayoutItemId;
                    // Check if the batch status is "UNCLAIMED"
                    if (statusResponse.TransactionStatus == "UNCLAIMED")
                    {
                        fundObj.TransactionStatus = statusResponse.TransactionStatus;
                    }
                    bool updateFundRecord = await _payPalGateWayService.AddPayPalTransactionAdminToValet(fundObj);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }

        private async Task<(PaymentStatusCheckResult Result, string TransactionStatus, string PayoutItemId)> CheckPaymentStatus(PayPalHttpClient client, string batchId)
        {
            try
            {
                var request = new PayoutsGetRequest(batchId);
                var response = await client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var payoutBatch = response.Result<PayoutsSdk.Payouts.PayoutBatch>();

                    // Find the item by its ID in the Items list
                    var item = payoutBatch.Items.FirstOrDefault(i => i.PayoutBatchId == batchId);
                    var payoutItemId = item?.PayoutItemId;
                    if (item != null)
                    {
                        var transactionStatus = item.TransactionStatus;
                        return (PaymentStatusCheckResult.Success, transactionStatus, payoutItemId);
                    }
                    else
                    {
                        return (PaymentStatusCheckResult.ItemNotFound, null, null);
                    }
                }
                else
                {
                    return (PaymentStatusCheckResult.UnexpectedStatusCode, null, null);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                return (PaymentStatusCheckResult.Exception, null, null);
            }
        }

        public async Task<bool> RefundPayment(string captureId, int orderId)
        {
            try
            {
                var config = new Dictionary<string, string> { { "mode", "sandbox" } };
                var accessToken = new OAuthTokenCredential(_configuration["PayPal:ClientId"], _configuration["PayPal:ClientSecret"], config).GetAccessToken();

                var apiContext = new APIContext(accessToken);

                var capturedInfo = await _payPalGateWayService.CapturedAmount(captureId);
                if (capturedInfo != null)
                {
                    var refund = new PayPal.Api.Refund
                    {
                        amount = new Amount
                        {
                            currency = capturedInfo.Currency,
                            total = capturedInfo.PayableAmount
                        }
                    };

                    var refundedCapture = Capture.Refund(apiContext, captureId, refund);
                    if (refundedCapture.state == "completed")
                    {
                        bool refundStatus = await _payPalGateWayService.PaymentRefunding(captureId);
                        bool updateOrderStatusForCancel = await _orderService.UpdateOrderStatusForCancel(orderId);
                        return refundStatus;
                    }
                }

                return false;
            }
            catch (PayPal.HttpException ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }
    }

}


