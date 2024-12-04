using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.NotificationHub;
using ITValet.Services;
using ITValet.Utils.Helpers;
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


        #region Code Refactor

        [HttpPost("PayPalCheckoutForOrder")]
        public async Task<IActionResult> PayPalCheckoutForOrders(PayPalOrderCheckOutViewModel orderDto)
        {
            try
            {
                // Validate order price
                if (orderDto.OrderPrice <= 0m)
                    return BadRequest(new ResponseDto { Status = false, StatusCode = "400", Message = "Price can't be negative" });

                // Create the payment request
                var paymentRequest = PayPalPaymentHelper.CreatePaymentRequest(
                    orderDto,
                    projectVariables.ReactUrl,
                    _configuration["PayPal:ClientId"],
                    _configuration["PayPal:ClientSecret"],
                    "Order"
                );

                if (paymentRequest.PaymentId == null)
                    return StatusCode(500, new ResponseDto { Status = false, StatusCode = "500", Message = "Payment creation failed" });

                // Save the order in the database
                orderDto.PaymentId = paymentRequest.PaymentId;
                var orderId = await _orderService.AddOrderByPayPal(orderDto);
                if (orderId == -1)
                    return StatusCode(500, new ResponseDto { Status = false, StatusCode = "500", Message = "Order insertion failed" });

                // Save PayPal order details
                var orderSaved = await _payPalGateWayService.AddPayPalOrder(new OrderCheckOutViewModel
                {
                    PaymentId = paymentRequest.PaymentId,
                    OrderId = orderId,
                    ClientId = orderDto.ClientId,
                    ValetId = orderDto.ValetId,
                    OrderPrice = orderDto.OrderPrice
                });

                if (!orderSaved)
                    return StatusCode(500, new ResponseDto { Status = false, StatusCode = "500", Message = "PayPal record insertion failed" });

                if (!string.IsNullOrEmpty(paymentRequest.ApprovalUrl))
                {
                    return Ok(new ResponseDto
                    {
                        Status = true,
                        StatusCode = "200",
                        Data = new PayPalCheckOutURL { Url = paymentRequest.ApprovalUrl },
                    });
                }

                return StatusCode(500, new ResponseDto { Status = false, StatusCode = "500", Message = "Approval URL not found" });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage($"{projectVariables.BaseUrl} ----------<br>{ex.Message}<br>{ex.StackTrace}");
                return StatusCode(500, new ResponseDto { Status = false, StatusCode = "500", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpGet("CheckPaymentStatusForOrder")]
        public async Task<IActionResult> CheckPaymentStatusForOrder(string paymentId, string token, string payerID)
        {
            try
            {
                // Retrieve order by payment ID
                var orderObj = await _payPalGateWayService.GetOrderByPaymentId(paymentId);
                if (orderObj == null)
                {
                    return NotFound(new ResponseDto { Status = false, StatusCode = "404", Message = "OrderRecordNotFound" });
                }

                //// Execute payment
                var executedPayment = PayPalPaymentHelper.ExecutePayment(paymentId, payerID, _configuration);
                if (executedPayment.state.ToLower() != "approved" || executedPayment.intent.ToLower() != "authorize")
                {
                    return StatusCode(500, new ResponseDto { Status = false, StatusCode = "500", Message = "Something went wrong" });
                }

                // Capture payment
                var captureResponse = PayPalPaymentHelper.CapturePayment(executedPayment, _configuration);
                if (captureResponse == null || captureResponse.State != "completed")
                {
                    return StatusCode(204, new ResponseDto { Status = false, StatusCode = "204", Message = "Payment executed but capture failed" });
                }

                // Update order details
                bool isUpdated = await UpdateOrderDetails(orderObj, captureResponse, paymentId);
                if (!isUpdated)
                {
                    return StatusCode(402, new ResponseDto { Status = false, StatusCode = "402", Message = "Issue arose during payment update" });
                }

                // Create response
                var orderStatus = new CheckPaymentStatusForOrder
                {
                    AuthorizationId = captureResponse?.AuthorizationId,
                    CaptureId = captureResponse?.CaptureId,
                    PaymentId = orderObj?.PaymentId,
                    EncOrderId = StringCipher.EncryptId(orderObj.OrderId),
                    TotalPayment = orderObj.OrderPrice.ToString(),
                    TransactionFee = orderObj.PayPalTransactionFee,
                    PaymentStatus = "success",
                    PaymentMethod = "PayPal"
                };

                var getOrder = await _orderService.GetOrderById(orderObj.OrderId);
                return Ok(new ResponseDto { Data = getOrder, Status = true, StatusCode = "200" });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage($"{projectVariables.BaseUrl}<br>{ex.Message}<br>{ex.StackTrace}");
                return StatusCode(400, new ResponseDto { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpPost("PayPalCheckoutForPackage")]
        public async Task<IActionResult> PayPalCheckOutForPackages(PackageCOutRequest checkOut)
        {
            try
            {
                // Prepare package data
                var packageDetails = GetPackageDetails(checkOut.SelectedPackage);
                if (packageDetails == null)
                {
                    return BadRequest(new ResponseDto { Status = false, StatusCode = "400", Message = "Invalid package selection" });
                }

                // Create payment request using PayPalPaymentHelper
                var paymentRequest = PayPalPaymentHelper.CreatePaymentRequest(
                    new PayPalOrderCheckOutViewModel
                    {
                        OrderTitle = packageDetails.Description,
                        TotalPrice = packageDetails.Price,
                        OrderDescription = packageDetails.Description
                    },
                    $"{projectVariables.ReactUrl}",
                    _configuration["PayPal:ClientId"],
                    _configuration["PayPal:ClientSecret"],
                    "Package"
                );

                if (string.IsNullOrEmpty(paymentRequest.PaymentId))
                {
                    return StatusCode(500, new ResponseDto { Status = false, StatusCode = "500", Message = "Payment creation failed" });
                }

                // Save package and PayPal details
                packageDetails.PaymentId = paymentRequest.PaymentId;
                packageDetails.ClientId = checkOut?.ClientId?.ToString();
                var userPackageId = await _userPackageService.AddUserPackageAndGetId(packageDetails.ToUserPackage());
                if (userPackageId == -1 || !await _payPalGateWayService.AddPayPalPackage(packageDetails.ToPackageCheckOutViewModel(userPackageId)))
                {
                    return StatusCode(500, new ResponseDto { Status = false, StatusCode = "500", Message = "Failed to save package details" });
                }

                return Ok(new ResponseDto
                {
                    Status = true,
                    StatusCode = "200",
                    Data = new PayPalCheckOutURL { Url = paymentRequest.ApprovalUrl }
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage($"{projectVariables.BaseUrl}<br>{ex.Message}<br>{ex.StackTrace}");
                return StatusCode(500, new ResponseDto { Status = false, StatusCode = "500", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpGet("CheckPaymentStatusForPackage")]
        public async Task<IActionResult> CheckPaymentStatusForPackages(string paymentId, string token, string payerID)
        {
            try
            {
                // Retrieve package by payment ID
                var packageObj = await _payPalGateWayService.GetPackageByPaymentId(paymentId);
                if (packageObj == null)
                {
                    return NotFound(new ResponseDto { Status = false, StatusCode = "404", Message = "Package not found" });
                }

                // Execute payment using PayPalPaymentHelper
                var executedPayment = PayPalPaymentHelper.ExecutePayment(paymentId, payerID, _configuration);
                if (executedPayment.state.ToLower() != "approved")
                {
                    return StatusCode(400, new ResponseDto { Status = false, StatusCode = "400", Message = "Payment not approved" });
                }

                // Update package details
                packageObj.PaymentStatus = "completed";
                packageObj.PayableAmount = executedPayment.transactions.FirstOrDefault()?.amount.total ;
                packageObj.Currency = executedPayment.transactions.FirstOrDefault()?.amount.currency;

                if (!await _payPalGateWayService.UpdatePackageRecord(packageObj) ||
                    !await _userPackageService.UpdateUserPackage(packageObj.UserPackageId, "PAYPAL"))
                {
                    return StatusCode(500, new ResponseDto { Status = false, StatusCode = "500", Message = "Failed to update package record" });
                }
                var getPackage = await _userPackageService.GetUserPackageById(packageObj.UserPackageId);
                return Ok(new ResponseDto()
                {
                    Status = true,
                    StatusCode = "200",
                    Message = "Payment Completed",
                    Data = getPackage
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage($"{projectVariables.BaseUrl}<br>{ex.Message}<br>{ex.StackTrace}");
                return StatusCode(500, new ResponseDto { Status = false, StatusCode = "500", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpPost("CreateOrderBySession")]
        public async Task<IActionResult> CreateOrderBySession(CheckOutDTO orderObj)
        {
            try
            {
                var order = ModelBinding(orderObj);

                int orderId = await _orderService.GetOrderId(order);

                var orderCK = new PayPalOrderCheckOutViewModel
                {
                    ValetId = Convert.ToInt32(orderObj.ValetId),
                    ClientId = Convert.ToInt32(orderObj.customerId),
                    OrderId = orderId,
                    PayByPackage = true
                };

                DateTime startDate = Convert.ToDateTime(orderObj.FromDateTime);
                DateTime endDate = Convert.ToDateTime(orderObj.ToDateTime);
                int sessions = PayPalPaymentHelper.CalculateSessions(startDate, endDate);

                var valet = await _userService.GetUserById(Convert.ToInt32(orderObj.ValetId));
                orderCK.OrderPrice = PayPalPaymentHelper.CalculateOrderPrice(sessions, (decimal)valet.PricePerHour);

                bool orderCreated = await _payPalGateWayService.AddPayPalOrderForPackage(orderCK);

                if (orderCreated)
                {
                    bool packageUpdated = await UpdatePackageSessions(orderObj.PackageId.Value, sessions);

                    if (packageUpdated)
                    {
                        bool orderUpdated = await UpdateOrder(orderCK.OrderPrice, orderId);

                        if (orderUpdated)
                        {
                            var orderOBJ = await _orderService.GetOrderById(orderId);

                            if (orderOBJ.OfferId != null)
                            {
                                await _offerService.UpdateOfferStatus(orderOBJ.Id, orderOBJ.OfferId);
                            }

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

                return Ok(PayPalPaymentHelper.CreateErrorResponse("Session Not Updated"));
            }
            catch (Exception ex)
            {
                await LogError(ex, projectVariables.BaseUrl);
                return Ok(PayPalPaymentHelper.CreateErrorResponse(GlobalMessages.SystemFailureMessage));
            }
        }

        private async Task<bool> UpdateOrder(decimal price, int orderId)
        {
            var orderObj = await _orderService.GetOrderById(orderId);
            orderObj.IsActive = 1;
            orderObj.OrderPrice = price;
            return await _orderService.UpdateOrder(orderObj);
        }

        private PackageDetails GetPackageDetails(string selectedPackage)
        {
            return selectedPackage switch
            {
                "IYear" => new PackageDetails
                {
                    Price = 100.00m,
                    Description = "1 Year (6 Sessions) Package",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddYears(1),
                    TotalSessions = 6,
                    RemainingSessions = 6,
                    PackageType = 1,
                    PackageName = "IYear"
                },
                "2Year" => new PackageDetails
                {
                    Price = 200.00m,
                    Description = "2 Years (12 Sessions) Package",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddYears(2),
                    TotalSessions = 12,
                    RemainingSessions = 12,
                    PackageType = 2,
                    PackageName = "2Year"
                },
                _ => null
            };
        }

        private async Task<bool> UpdateOrderDetails(OrderCheckOutViewModel orderObj, CaptureResponse captureResponse, string paymentId)
        {
            orderObj.PayableAmount = captureResponse.TransactionFee;
            orderObj.Currency = "CAD";
            orderObj.PayPalTransactionFee = captureResponse.TransactionFee;
            orderObj.PaymentStatus = captureResponse.State;
            orderObj.CaptureId = captureResponse.CaptureId;
            orderObj.AuthorizationId = captureResponse.AuthorizationId;
            orderObj.IsRefund = false;

            // Update PayPal order record
            bool isPayPalOrderUpdated = await _payPalGateWayService.UpdateOrderRecord(orderObj);

            // Update main order
            var orderUpdate = await _orderService.UpdateOrderByPayPal(paymentId, captureResponse.CaptureId);

            if (orderUpdate != null && orderUpdate.OfferId != 0)
            {
                await _offerService.UpdateOfferStatus(orderUpdate.Id, orderUpdate.OfferId);
            }

            return isPayPalOrderUpdated && orderUpdate != null;
        }

        private async Task<bool> UpdatePackageSessions(int packageId, int sessionsUsed)
        {
            var packageObj = await _userPackageService.GetUserPackageById(packageId);
            if (packageObj.RemainingSessions < sessionsUsed) return false;

            packageObj.RemainingSessions -= sessionsUsed;
            return await _userPackageService.UpdateUserPackageSession(packageObj);
        }

        private async Task LogError(Exception ex, string baseUrl)
        {
            string message = $"{baseUrl} ----------<br>{ex.Message}---------------{ex.StackTrace}";
            await MailSender.SendErrorMessage(message);
        }

        private Models.Order ModelBinding(CheckOutDTO order)
        {
            return new Models.Order
            {
                OrderTitle = order.PaymentTitle,
                OrderDescription = order.PaymentDescription,
                StartDateTime = Convert.ToDateTime(order.FromDateTime),
                EndDateTime = Convert.ToDateTime(order.ToDateTime),
                ValetId = Convert.ToInt32(order.ValetId),
                CustomerId = Convert.ToInt32(order.customerId),
                OfferId = order.OfferId,
                PackageId = order.PackageId,
                PackageBuyFrom = "PAYPAL",
                StripeStatus = (int)StripePaymentStatus.SessionUsed,
                IsActive = 0,
                OrderStatus = 0,
                IsDelivered = 0,
                OrderPrice = 0,
                TotalAmountIncludedFee = 0,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
        }
        #endregion

    }
}
