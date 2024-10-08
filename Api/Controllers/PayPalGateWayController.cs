using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.NotificationHub;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using PayoutsSdk.Payouts;
using PayPal.Api;
using PayPalCheckoutSdk.Core;
using PayPalHttp;
using System.Net;

namespace ITValet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayPalGateWayController : ControllerBase
    {
        private readonly IPayPalGateWayService _payPalGateWayService;
        private readonly IConfiguration _configuration;
        private readonly PayPalHttpClient _paypalClient;
        private readonly IFundTransferService _fundTransferService;
        private readonly IOrderRepo _orderService;
        private readonly IUserRepo _userService;
        private readonly INotificationService _userPackageService;
        private readonly IUserRatingRepo _ratingService;
        private readonly IOfferDetailsRepo _offerService;
        private readonly IOrderReasonRepo _orderReasonService;
        private readonly ProjectVariables projectVariables;
        private readonly IJwtUtils jwtUtils;
        private readonly IHubContext<NotificationHubSocket> _notificationHubSocket;
        private readonly INotificationRepo _notificationService;

        public PayPalGateWayController(IPayPalGateWayService payPalGateWayService, IConfiguration configuration,
            PayPalHttpClient payPalHttpClient, IOrderRepo orderService, IUserRepo userService,
            IOrderReasonRepo orderReasonService,
            INotificationService userPackageService, IFundTransferService fundTransferService,
            IOfferDetailsRepo offerService, IOptions<ProjectVariables> options, IUserRatingRepo ratingService, IJwtUtils _jwtUtils, IHubContext<NotificationHubSocket> notificationHubSocket, INotificationRepo notificationService)
        {
            _payPalGateWayService = payPalGateWayService;
            _configuration = configuration;
            _paypalClient = payPalHttpClient;
            _orderService = orderService;
            _orderReasonService = orderReasonService;
            _userPackageService = userPackageService;
            _fundTransferService = fundTransferService;
            _offerService = offerService;
            projectVariables = options.Value;
            _ratingService = ratingService;
            _notificationHubSocket = notificationHubSocket;
            jwtUtils = _jwtUtils;
            _notificationService = notificationService;
            _userService = userService;
        }

        [HttpPost("payPalCheckoutForPackage")]
        public async Task<IActionResult> PayPalCheckOutForPackages(PackageCOutRequest checkOut)
        {
            try
            {
                PackageCheckOutViewModel packageobj = new PackageCheckOutViewModel();
                UserPackage package = new UserPackage();
                var config = new Dictionary<string, string> { { "mode", "sandbox" } };
                var accessToken = new OAuthTokenCredential(_configuration["PayPal:ClientId"], _configuration["PayPal:ClientSecret"], config).GetAccessToken();
                var apiContext = new APIContext(accessToken);

                decimal price;
                string description;

                if (checkOut.SelectedPackage == "IYear")
                {
                    price = 100.00m;
                    description = "1 Year (6 Sessions) Package";
                    //Calculate start and end dates for a 1-year package
                    DateTime startDate = DateTime.Now;
                    DateTime endDate = startDate.AddYears(1);
                    packageobj.StartDate = startDate;
                    packageobj.EndDate = endDate;
                    packageobj.PackageType = "OneYear";
                    package.StartDateTime = startDate;
                    package.EndDateTime = endDate;
                    package.PackageType = 1;
                    package.PackageName = checkOut.SelectedPackage;
                    package.TotalSessions = 6;
                    package.RemainingSessions = 6;
                }
                else if (checkOut.SelectedPackage == "2Year")
                {

                    price = 200.00m;
                    description = "2 Years (12 Sessions) Package";

                    // Calculate start and end dates for a 2-year package
                    DateTime startDate = DateTime.Now;
                    DateTime endDate = startDate.AddYears(2);
                    packageobj.StartDate = startDate;
                    packageobj.EndDate = endDate;
                    packageobj.PackageType = "TwoYear";
                    package.StartDateTime = startDate;
                    package.EndDateTime = endDate;
                    package.PackageType = 2;
                    package.PackageName = checkOut.SelectedPackage;
                    package.TotalSessions = 12;
                    package.RemainingSessions = 12;
                }
                else
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "InvalidSelection" });
                }

                var payment = new Payment
                {
                    intent = "sale",
                    payer = new Payer { payment_method = "paypal" },
                    redirect_urls = new RedirectUrls
                    {
                        return_url = projectVariables.BaseUrl+"PayPalClientGateway/PaymentStatusForPackage",
                        cancel_url = projectVariables.BaseUrl+"PayPalClientGateway/CancelPackage"
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
                                        name = description,
                                        sku = "001",
                                        price = price.ToString("0.00"),
                                        currency = "CAD",
                                        quantity = "1"
                                    }
                                }
                            },
                            amount = new Amount
                            {
                                currency = "CAD",
                                total = price.ToString("0.00") // Convert the price to a formatted string
                            },
                            description = description // Use the description of the selected package
                        }
                    }
                };

                var createdPayment = payment.Create(apiContext);
                var paymentId = createdPayment.id;
                packageobj.PaymentId = paymentId;
                packageobj.PackagePrice = price;
                packageobj.ClientId = checkOut.ClientId.Value;
                package.CustomerId = checkOut.ClientId;

                int userPackageId = -1;
                bool isPackageInsertion = false;
                try
                {
                    userPackageId = await _userPackageService.AddUserPackageAndGetId(package);
                    if (userPackageId != -1)
                    {
                        packageobj.UserPackageId = userPackageId;
                        isPackageInsertion = await _payPalGateWayService.AddPayPalPackage(packageobj);
                    }
                }
                catch (Exception ex)
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "An error occurred during package processing" });
                }

                if (isPackageInsertion)
                {
                    var approvalUrl = createdPayment.links.FirstOrDefault(link => link.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase))?.href;
                    if (approvalUrl != null)
                    {
                        PayPalCheckOutURL urlObj = new PayPalCheckOutURL();
                        var redirectUrl = approvalUrl + "&paymentId=" + paymentId;
                        urlObj.Url = redirectUrl;
                        return Ok(new ResponseDto() { Data = urlObj, Status = true, StatusCode = "200" });
                    }
                }

                return Ok(new ResponseDto() { Status = false, StatusCode = "500", Message = "Something went wrong" });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpGet("packagestatus")]
        public async Task<IActionResult> CheckPaymentStatusForPackages(string paymentId, string token, string PayerID)
        {
            try
            {
                // Retrieve the data from the database with the help of paymentId
                var packageobj = await _payPalGateWayService.GetPackageByPaymentId(paymentId);
                if (packageobj == null)
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = "NotFound" });
                }

                var config = new Dictionary<string, string> { { "mode", "sandbox" } };
                var accessToken = new OAuthTokenCredential(_configuration["PayPal:ClientId"], _configuration["PayPal:ClientSecret"], config).GetAccessToken();
                var apiContext = new APIContext(accessToken);

                // Execute the payment using the payer ID
                var paymentExecution = new PaymentExecution { payer_id = PayerID };
                var executedPayment = Payment.Execute(apiContext, paymentId, paymentExecution);

                // Check if the payment is approved
                if (executedPayment.state.ToLower() == "approved" && executedPayment.intent.ToLower() == "sale")
                {
                    var paymentStatus = executedPayment.transactions.FirstOrDefault()?.related_resources.FirstOrDefault()?.sale.state;
                    if (paymentStatus == "completed")
                    {
                        packageobj.PaymentStatus = paymentStatus;
                        packageobj.IsActive = 1;
                        packageobj.PayableAmount = executedPayment.transactions.FirstOrDefault()?.amount.total;
                        packageobj.Currency = executedPayment.transactions.FirstOrDefault()?.amount.currency;

                        // Update package record
                        bool isUpdatePackageRecord = await _payPalGateWayService.UpdatePackageRecord(packageobj);
                        bool IsUserPackageUpdated = await _userPackageService.UpdateUserPackage(packageobj.UserPackageId, "PAYPAL");

                        if (isUpdatePackageRecord && IsUserPackageUpdated)
                        {
                            CheckoutPaymentStatusPackage status = new CheckoutPaymentStatusPackage
                            {
                                PaymentStatus = "success",
                                PaymentId = paymentId,
                            };
                            return Ok(new ResponseDto() { Data = status, Status = true, StatusCode = "200" });
                        }
                    }
                    else
                    {
                        CheckoutPaymentStatusPackage status = new CheckoutPaymentStatusPackage
                        {
                            PaymentStatus = "notCompleted",
                            PaymentId = paymentId,
                        };
                        return Ok(new ResponseDto() { Data = status, Status = false, StatusCode = "400", Message = "NotFound" });
                    }
                }

                return Ok(new ResponseDto() { Status = false, StatusCode = "500", Message = "Something went wrong" });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpPost("paypalCheckoutForOrder")]
        public async Task<IActionResult> PayPalCheckoutForOrders(PayPalOrderCheckOutViewModel orderDto)
        {
            try
            {
                if (orderDto.OrderPrice <= 0m)
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = "Price can't be negative" });
                }

                OrderCheckOutViewModel orderObj = new OrderCheckOutViewModel();
                var config = new Dictionary<string, string> { { "mode", "sandbox" } };
                var accessToken = new OAuthTokenCredential(_configuration["PayPal:ClientId"], _configuration["PayPal:ClientSecret"], config).GetAccessToken();

                var apiContext = new APIContext(accessToken);

                var payment = new Payment
                {
                    intent = "authorize",
                    payer = new Payer { payment_method = "paypal" },
                    redirect_urls = new RedirectUrls
                    {
                        return_url = projectVariables.BaseUrl + "PayPalClientGateway/PaymentStatusForOrder",
                        cancel_url = projectVariables.BaseUrl + "PayPalClientGateway/CancelOrder"
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
                                        price = orderDto.TotalPrice.ToString("0.00"), // Convert the price to a formatted string
                                        currency = "CAD",
                                        quantity = "1"
                                    }
                                }
                            },
                            amount = new Amount
                            {
                                currency = "CAD",
                                total = orderDto.TotalPrice.ToString("0.00") // Convert the price to a formatted string
                            },
                            description = orderDto.OrderDescription,
                        }
                    }
                };

                var createdPayment = payment.Create(apiContext);
                var paymentId = createdPayment.id;
                orderObj.PaymentId = paymentId;
                orderObj.OrderId = orderDto.OrderId;
                orderObj.ClientId = orderDto.ClientId;
                orderObj.ValetId = orderDto.ValetId;
                orderObj.OrderPrice = orderDto.OrderPrice;

                // Insert record in orderTable Against PayPalPaymentId
                orderDto.OrderStatus = 0;
                orderDto.PaymentId = paymentId;

                var orderId = await _orderService.AddOrderByPayPal(orderDto);
                if (orderId != -1)
                {
                    orderObj.OrderId = orderId;
                }
                bool isOrderInsertedInPayPalRec = await _payPalGateWayService.AddPayPalOrder(orderObj);
                if (!isOrderInsertedInPayPalRec && orderId != -1)
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "InsertionFailed" });
                }
                var approvalUrl = createdPayment.links.FirstOrDefault(link => link.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase))?.href;
                if (approvalUrl != null)
                {
                    PayPalCheckOutURL urlObj = new PayPalCheckOutURL();
                    var redirectUrl = approvalUrl + "&paymentId=" + paymentId;
                    urlObj.Url = redirectUrl;
                    return Ok(new ResponseDto() { Data = urlObj, Status = true, StatusCode = "200" });
                }
                return Ok(new ResponseDto() { Status = false, StatusCode = "500", Message = "Something went wrong" });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

       
        [HttpGet("orderstatus")]
        public async Task<IActionResult> CheckPaymentStatusForOrder(string paymentId, string token, string PayerID)
        {
            try
            {
                CheckPaymentStatusForOrder orderstatus = new CheckPaymentStatusForOrder();
                var orderObj = await _payPalGateWayService.GetOrderByPaymentId(paymentId);
                if (orderObj == null)
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = "OrderRecordNotFound" });
                }
                var config = new Dictionary<string, string> { { "mode", "sandbox" } };
                var accessToken = new OAuthTokenCredential(_configuration["PayPal:ClientId"], _configuration["PayPal:ClientSecret"], config).GetAccessToken();
                var apiContext = new APIContext(accessToken);

                var paymentExecution = new PaymentExecution { payer_id = PayerID };
                var executedPayment = Payment.Execute(apiContext, paymentId, paymentExecution);

                if (executedPayment.state.ToLower() == "approved" && executedPayment.intent.ToLower() == "authorize")
                {
                    orderObj.PayableAmount = executedPayment.transactions.FirstOrDefault()?.amount.total;
                    orderObj.Currency = executedPayment.transactions.FirstOrDefault()?.amount.currency;
                    var authorizationId = executedPayment.transactions.FirstOrDefault()?.related_resources.FirstOrDefault()?.authorization.id;

                    var capture = new Capture
                    {
                        is_final_capture = true,
                        amount = new Amount
                        {
                            currency = "CAD",
                            total = orderObj.PayableAmount
                        }
                    };

                    var capturedPayment = PayPal.Api.Authorization.Get(apiContext, authorizationId).Capture(apiContext, capture);
                    orderObj.PayPalTransactionFee = capturedPayment.transaction_fee.value;
                    if (capturedPayment.state == "completed")
                    {
                        orderObj.PaymentStatus = capturedPayment.state;
                        orderObj.CaptureId = capturedPayment.id;
                        orderObj.AuthorizationId = authorizationId;
                        orderObj.IsRefund = false;

                        bool isPayPalOrderRecUpdate = await _payPalGateWayService.UpdateOrderRecord(orderObj);
                        var orderUpdate = await _orderService.UpdateOrderByPayPal(paymentId, capturedPayment.id);
                        var encOrderId = StringCipher.EncryptId(orderUpdate.Id);
                        if (orderUpdate != null)
                        {
                            if (orderUpdate.OfferId != 0)
                            {
                              bool isOfferUpdate = await _offerService.UpdateOfferStatus(orderUpdate.Id, orderUpdate.OfferId);
                            }
                        }

                        if (isPayPalOrderRecUpdate && orderUpdate != null)
                        {
                            orderstatus.AuthorizationId = authorizationId;
                            orderstatus.CaptureId = capturedPayment.id;
                            orderstatus.PaymentId = paymentId;
                            orderstatus.EncOrderId = encOrderId;
                            orderstatus.PaymentStatus = "success";
                            return Ok(new ResponseDto() { Data = orderstatus, Status = true, StatusCode = "200" });
                        }
                        return Ok(new ResponseDto() { Status = false, StatusCode = "402", Message = "IssueAriseDuringPayment" });
                    }
                    else
                    {
                        return Ok(new ResponseDto() { Status = false, StatusCode = "204", Message = "Payment Executed But captured Failed" });
                    }
                }

                return Ok(new ResponseDto() { Status = false, StatusCode = "500", Message = "Something went wrong" });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpPost("paypalRefund")]
        public async Task<IActionResult> Refund(string captureId, int orderId)
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
                        bool orderStatusCanceled = await _orderService.UpdateOrderStatusForCancel(orderId);
                        if (refundStatus == true)
                        {
                            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Refunded" });
                        }

                        return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "RefundedButInsertionFailed" });
                    }

                }
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = "objectNotFound" });
            }
            catch (PayPal.HttpException ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpPost("paypalTransactionDetail")]
        public async Task<IActionResult> GetTransactionDetail(string transactionId)
        {
            try
            {
                var config = new Dictionary<string, string> { { "mode", "sandbox" } };
                var accessToken = new OAuthTokenCredential(_configuration["PayPal:ClientId"], _configuration["PayPal:ClientSecret"], config).GetAccessToken();

                var apiContext = new APIContext(accessToken);

                // Use the PayPal API to get the payment details
                var payment = Payment.Get(apiContext, transactionId);

                // Extract the relevant information from the payment object
                var paymentDetails = new TransactionDetailViewModel
                {
                    TransactionId = payment.id,
                    State = payment.state,
                    Amount = payment.transactions.FirstOrDefault()?.amount.total,
                    Currency = payment.transactions.FirstOrDefault()?.amount.currency,
                };

                return BadRequest();
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpPost("canceledUnclaimedpayment")]
        public async Task<IActionResult> CancelUnclaimedPayment(string payOutItemId)
        {
            try
            {
                PaymentCancelViewModel cancelObj = new PaymentCancelViewModel();
                // Set up your PayPal credentials and API context
                var clientId = _configuration["PayPal:ClientId"];
                var clientSecret = _configuration["PayPal:ClientSecret"];
                var environment = new SandboxEnvironment(clientId, clientSecret);
                var client = new PayPalHttpClient(environment);

                // Create a PayoutsItemCancelRequest to cancel the unclaimed payout
                var cancelRequest = new PayoutsItemCancelRequest(payOutItemId);
                var cancelResponse = await client.Execute(cancelRequest);

                if (cancelResponse.StatusCode == HttpStatusCode.OK)
                {
                    if (cancelResponse.Result<PayoutItemResponse>()?.TransactionStatus == "RETURNED")
                    {
                        // Payment has been canceled and refunded, handle accordingly
                        cancelObj.CancelationStatus = "RECOVER";
                        cancelObj.CancelationReason = "We had to cancel the payment due to an inaccurate PayPal account";
                        cancelObj.CancelByAdmin = true;
                        cancelObj.ReturnedAmount = cancelResponse.Result<PayoutItemResponse>()?.PayoutItem?.Amount.Value;
                        bool cancelPayment = await _payPalGateWayService.CancelUnclaimedPayment(payOutItemId, cancelObj);
                        if (cancelPayment)
                        {
                            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "ClaimCleared" });
                        }
                    }
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "ClaimNotCleared" });
                }
                else
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
                }
            }
            catch (HttpException ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpPost("addPayPalAccount")]
        public async Task<IActionResult> AddPayPalAccount(PayPalAccountViewModel paypalAcc)
        {
            try
            {
                var response = await _payPalGateWayService.AddPayPalAccountInformation(paypalAcc);
                if (response.Success==true)
                {
                    return Ok(new ResponseDto() { Data = response, Status = true, StatusCode = "200" });
                }
                else if(response.Message == "Email already exists.")
                {
                    return Ok(new ResponseDto() { Message = response.Message, Status = false, StatusCode = "204" });
                }
                return Ok(new ResponseDto() { Data = response, Status = false, StatusCode = "400" });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpGet("GetPayPalAccount")]
        public async Task<IActionResult> GetPayPalAccount(string id)
        {
            try
            {
                var account = await _payPalGateWayService.GetPayPalAccount(Convert.ToInt32(id));
                if(account == null)
                {
                    return Ok(new ResponseDto() {Message = "Account not found", Status = false, StatusCode = "404" });
                }
                return Ok(new ResponseDto() { Data = account, Status = true, StatusCode = "200" });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpDelete("DeletePayPalAccount")]
        public async Task<IActionResult> DeletePayPalAccount(int id)
        {
            try
            {
                bool isAccountDeleted = await _payPalGateWayService.DeletePayPalAccount(id);
                if (isAccountDeleted)
                {
                    return Ok(new ResponseDto() { Status = true, StatusCode = "200" });
                }
                return Ok(new ResponseDto() { Message= "Not Deleted", Status = false, StatusCode = "400" });
            } 
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpPost("OrderAccepted")]
        public async Task<IActionResult> OrderAccepted(AcceptOrder orderDetail)
        {
            try
            {
                UserClaims? getUserFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
                var orderObj = await _orderService.UpdateOrderStatusForPayPal(orderDetail);
                var isUserRatingInserted = await AddUserRatingAgainstOrder(orderDetail);
                var valetPayPalEmail = await _payPalGateWayService.GetPayPalAccount(Convert.ToInt32(orderDetail.ValetId));

                if (orderObj == null)
                {
                    return Ok(new ResponseDto { Message = "Database Update Failed", Status = false, StatusCode = "404" });
                }

                if (orderObj.PackageId != null && orderObj.PackageBuyFrom == "PAYPAL")
                {
                    // Order from a package
                    OrderAcceptedOfPackage orderObject = new OrderAcceptedOfPackage();
                    orderObject.PaidByPackage = true;
                    orderObject.ValetId = Convert.ToInt32(orderDetail.ValetId);
                    orderObject.CustomerId = Convert.ToInt32(orderDetail.CustomerId);
                    orderObject.OrderId = orderObj.Id;
                    orderObject.OrderPrice = orderObj.OrderPrice;
                    orderObject.PayPalAccount = valetPayPalEmail;

                    bool isOrderAccepted = await _payPalGateWayService.OrderCreatedByPayPalPackage(orderObject);

                    if (isOrderAccepted)
                    {
                        return Ok(new ResponseDto { Message = "Order Completed Successfully", Status = true, StatusCode = "200" });
                    }
                    else
                    {
                        return Ok(new ResponseDto { Message = "Failed to update Object", Status = false, StatusCode = "404" });
                    }
                }
                else
                {
                    // Order from checkout
                    OrderCheckOutAccepted checkoutObj = new OrderCheckOutAccepted
                    {
                        OrderId = orderObj.Id,
                        PaymentId = orderObj.PayPalPaymentId,
                        PayPalAccount = valetPayPalEmail, // May be null, which is okay
                    };

                    // Calculate HST fee and deduct it from the Order Price 
                    decimal orderPrice = orderObj.OrderPrice ?? 0m;
                    checkoutObj.OrderPrice = orderPrice;

                    bool isCheckoutObjUpdated = await _payPalGateWayService.PayPalOrderCheckoutAccepted(checkoutObj);

                    if (isCheckoutObjUpdated)
                    {
                        Models.Notification notificationObj = new Models.Notification
                        {
                            UserId = Convert.ToInt32(orderDetail.ValetId),
                            Title = "Order Accepted",
                            IsRead = 0,
                            IsActive = (int)EnumActiveStatus.Active,
                            Url = projectVariables.BaseUrl + "User/OrderDetail?orderId=" + StringCipher.EncryptId(checkoutObj.OrderId),
                            CreatedAt = GeneralPurpose.DateTimeNow(),
                            Description = "Customer Accepted Your Delivery",
                            NotificationType = (int)NotificationType.OrderCancellationRequested
                        };
                        bool isNotification = await _notificationService.AddNotification(notificationObj);


                        await _notificationHubSocket.Clients.All.SendAsync("ReloadNotifications",orderDetail.ValetId);

                        await _notificationHubSocket.Clients.All.SendAsync("UpdateOrderStatus", getUserFromToken.Id.ToString(), "", "", "", "2", orderDetail.ValetId.ToString());
                        return Ok(new ResponseDto { Message = "Order Completed Successfully", Status = true, StatusCode = "200" });
                    }
                    else
                    {
                        return Ok(new ResponseDto { Message = "Failed to update Object", Status = false, StatusCode = "404" });
                    }
                }
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpPost("CreateOrderBySession")]
        public async Task<IActionResult> CreateOrderBySession(CheckOutDTO orderObj)
        {
            try
            {
                var order = new Models.Order
                {
                    OrderTitle = orderObj.PaymentTitle,
                    OrderDescription = orderObj.PaymentDescription,
                    StartDateTime = Convert.ToDateTime(orderObj.FromDateTime),
                    EndDateTime = Convert.ToDateTime(orderObj.ToDateTime),
                    ValetId = Convert.ToInt32(orderObj.ValetId),
                    CustomerId = Convert.ToInt32(orderObj.customerId),
                    OfferId = orderObj.OfferId,
                    PackageId = orderObj.PackageId,
                    PackageBuyFrom = "PAYPAL",
                    StripeStatus = (int)StripePaymentStatus.SessionUsed,
                    IsActive = 0,
                    OrderStatus = 0,
                    IsDelivered = 0,
                    OrderPrice = 0,
                    TotalAmountIncludedFee = 0,
                    CreatedAt = GeneralPurpose.DateTimeNow()
                };

                // Get the order ID
                var orderId = await _orderService.GetOrderId(order);
                //Update OrderRecord in PayPalGateWayService
                PayPalOrderCheckOutViewModel orderCK = new PayPalOrderCheckOutViewModel();
                orderCK.ValetId = Convert.ToInt32(orderObj.ValetId);
                orderCK.ClientId = Convert.ToInt32(orderObj.customerId);
                orderCK.OrderId = orderId;
                orderCK.PayByPackage = true;

                // Calculate session duration in minutes, rounding up to the nearest hour
                DateTime endDate = Convert.ToDateTime(orderObj.ToDateTime);
                DateTime startDate = Convert.ToDateTime(orderObj.FromDateTime);
                TimeSpan duration = endDate - startDate;

                // Calculate sessions based on working hours (e.g., 1 session = 60 minutes)
                int totalMinutes = (int)duration.TotalMinutes;
                int sessions = (int)Math.Ceiling(totalMinutes / 60.0); // Round up to the nearest hour

                // Calculate the order price based on sessions (1 session = 1 hour)
                var getValet = await _userService.GetUserById(Convert.ToInt32(orderObj.ValetId));
                decimal orderPrice = sessions * (decimal)getValet.PricePerHour;
                orderCK.OrderPrice = orderPrice;

                var createOrderInOrderCheckOutForPackage = await _payPalGateWayService.AddPayPalOrderForPackage(orderCK);

                // Update remaining sessions in the user package if there are enough sessions available
                var packageObj = await _userPackageService.GetUserPackageById(orderObj.PackageId.Value);

                if (packageObj.RemainingSessions >= sessions)
                {
                    packageObj.RemainingSessions -= sessions;
                    bool updateSessionRecord = await _userPackageService.UpdateUserPackageSession(packageObj);

                    if (updateSessionRecord)
                    {
                        // Update the order
                        bool updateOrder = await UpdateOrder(orderPrice, orderId);
                        var orderOBJ = await _orderService.GetOrderById(orderId);
                        if (orderOBJ.OfferId != null)
                        {
                            bool isOfferUpdate = await _offerService.UpdateOfferStatus(orderOBJ.Id, orderOBJ.OfferId);
                        }
                        if (updateOrder)
                        {
                            return Ok(new ResponseDto
                            {
                                Id = StringCipher.EncryptId(orderId),
                                Status = true,
                                StatusCode = "200",
                                Message = GlobalMessages.SuccessMessage
                            });
                        }
                    }
                }

                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Session Not Updated" });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        private async Task<bool> UpdateOrder(decimal price, int orderId)
        {
            var orderObj = await _orderService.GetOrderById(orderId);
            orderObj.IsActive = 1;
            orderObj.OrderPrice = price;
            return await _orderService.UpdateOrder(orderObj);
        }

        private async Task<bool> AddUserRatingAgainstOrder(AcceptOrder order)
        {
            try
            {
                UserRating rating = new UserRating();
                rating.Stars = Convert.ToInt32(order.Stars);
                rating.Reviews = order.Reviews;
                rating.CustomerId = Convert.ToInt32(order.CustomerId);
                rating.OrderId = StringCipher.DecryptId(order.OrderId);
                rating.ValetId = Convert.ToInt32(order.ValetId);
                rating.IsActive = (int)EnumActiveStatus.Active;
                rating.CreatedAt = GeneralPurpose.DateTimeNow();
                await _ratingService.AddUserRating(rating);
                return true;           
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpPost("CancelOrderAndRevertSession")]
        public async Task<IActionResult> CancelOrderAndRevertSession(string OrderId)
        {
            int id = Convert.ToInt32(OrderId);
            var isSessionRevert = await _payPalGateWayService.CancelOrderAndRevertSessionAsync(id);
            var cancelOrderInCheckOutDb = await _payPalGateWayService.DeleteCheckOutOrderOfPackages(id);
            if (isSessionRevert && cancelOrderInCheckOutDb)
            {
                return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Order Cancelled" });
            }

            return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Session Not Updated" });
        }

        /*   [HttpGet("CalculatePayPalEarning")]
           public async Task<IActionResult> CalculatePayPalEarning (int valetId)
           {
               var totalPayPalEarning = await _payPalGateWayService.CalculateTotalPayPalEarning(valetId);
               if(totalPayPalEarning.HasValue)
               {
                   var totalEarnedAmount = totalPayPalEarning.ToString();
                   return Ok(new ResponseDto()
                   {
                       Status = true,
                       Data = totalEarnedAmount,
                       StatusCode = "200"
                   });
               }
               else
               {
                   return Ok(new ResponseDto()
                   {
                       Status = false,
                       Message = "Amount Not Found",
                       StatusCode = "400"
                   });
               }

           }*/

    }
}
