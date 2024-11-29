using ITValet.HelpingClasses;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.Services;
using ITValet.Utils.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Globalization;

namespace ITValet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripePaymentController : ControllerBase
    {
        private readonly IUserRepo _userRepo;
        private readonly IOrderRepo _orderRepo;
        private readonly IConfiguration _configuration;
        private readonly ProjectVariables _projectVariables;
        private readonly IOfferDetailsRepo _offerDetailService;
        private readonly INotificationService _userPackageService;
        private readonly IPayPalGateWayService _paypalGatewayService;

        public StripePaymentController(IUserRepo userRepo, IOptions<ProjectVariables> options, IConfiguration configuration,
            IOrderRepo orderRepo, IPayPalGateWayService paypalGateWayService, INotificationService userPackageService, 
            IOfferDetailsRepo offerDetailService)
        {
            _userRepo = userRepo;
            _orderRepo = orderRepo;
            _configuration = configuration;
            _userPackageService = userPackageService;
            _offerDetailService = offerDetailService;
            _paypalGatewayService = paypalGateWayService;
            _projectVariables = options.Value;
        }

        [HttpPost("create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession(string userId, CheckOutDTO checkoutDTO)
        {
            try
            {
                var getUser = await _userRepo.GetUserById(Convert.ToInt32(StringCipher.DecryptId(userId)));
                if (getUser != null && getUser.StripeId == null)
                {
                    var customerOptions = new CustomerCreateOptions
                    {
                        Email = getUser.Email,
                        Metadata = new Dictionary<string, string>
                        {
                            { "userId", getUser.Id.ToString() },
                        }
                    };
                    var customerService = new CustomerService();
                    Customer customer = await customerService.CreateAsync(customerOptions);
                    getUser.StripeId = customer.Id;
                    await _userRepo.UpdateUser(getUser);
                }

                // Checkout session for one-time payment
                var options = new SessionCreateOptions
                {
                    SuccessUrl = $"{_projectVariables.FrontEnd}PaymentSuccess",
                    CancelUrl = $"{_projectVariables.FrontEnd}PaymentCancelled",
                    PaymentMethodTypes = new List<string> { "card" },
                    Mode = "payment",
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = checkoutDTO.ActualOrderPrice,
                        },
                    },
                    Customer = getUser?.Id.ToString(),
                };

                var service = new SessionService();
                try
                {
                    var session = await service.CreateAsync(options);
                    CreateCheckoutSessionResponse data = new CreateCheckoutSessionResponse
                    {
                        SessionId = session.Id,
                        CheckOutURL = session.Url,
                        PublicKey = _configuration["Stripe:StripeClientId"],
                        PaymentTimeTicks = Convert.ToDateTime(GeneralPurpose.DateTimeNow()).Ticks.ToString()
                    };

                    return Ok(new { data = data, Status = true, StatusCode = "200", Message = "You Are Ready To Proceed, Hit Confirm CheckOut Button." });

                }
                catch (StripeException e)
                {
                    Console.WriteLine(e.StripeError.Message);

                    return Ok(new { Status = false, StatusCode = "500", Message = e.StripeError.Message });
                }
            }
            catch (StripeException e)
            {
                return Ok(new { Status = false, StatusCode = "500", Message = e.StripeError.Message });
            }
            catch (Exception ex)
            {
                return Ok(new { Status = false, StatusCode = "500", Message = ex.Message });
            }
        }

        [HttpPost("CreateStripeCharge")]
        public async Task<IActionResult> CreateStripeCharge(CheckOutDTO checkOutData)
        {
            var order = StripeHelper.InitializeOrder(checkOutData);
            var orderId = await _orderRepo.GetOrderId(order);

            if (orderId == -1)
                return BadRequest(new ResponseDto() { StatusCode = "404", Message = GlobalMessages.SystemFailureMessage, Data = null, Status = false });

            bool isOrderUpdated, isPackageUpdated = false;

            if (checkOutData.PackageId == null)
            {
                var chargePayment = await ProcessCharge(checkOutData, orderId, false);
                isOrderUpdated = await UpdateOrder(checkOutData?.TotalWorkCharges!, checkOutData?.ActualOrderPrice!,
                    chargePayment, orderId);
            }
            else
            {
                isOrderUpdated = await UpdateOrder(checkOutData?.TotalWorkCharges!, checkOutData?.ActualOrderPrice!,
                    "", orderId);
                isPackageUpdated = await UpdatePackage(checkOutData.PackageId, checkOutData?.WorkingHours!);
            }

            if (!isOrderUpdated)
                return BadRequest(new ResponseDto()
                {
                    Status = false,
                    StatusCode = "404",
                    Message = GlobalMessages.SystemFailureMessage,
                    Data = null,
                });

            if (checkOutData?.OfferId! != null)
                await _offerDetailService.UpdateOfferStatus(orderId, checkOutData.OfferId);

            return Ok(new ResponseDto()
            {
                Status = true,
                StatusCode = "200",
                Message = GlobalMessages.SuccessMessage,
                Data = StringCipher.EncryptId(orderId),
            });
        }

        [HttpPost("StripeCheckOutForPackages")]
        public async Task<IActionResult> StripeCheckOutForPackages(PackageCOutRequest checkOut)
        {
            try
            {
                var package = StripeHelper.InitializePackage(checkOut, out var packagePrice);
                var userPackageId = await _userPackageService.AddUserPackageAndGetId(package);

                if (userPackageId == -1)
                    throw new Exception("Package processing failed");

                var chargeResult = await ProcessChargeForPackage(checkOut, packagePrice);

                if (!string.IsNullOrEmpty(chargeResult) && await _userPackageService.UpdateUserPackage(userPackageId, "STRIPE"))
                    return Ok(new ResponseDto()
                    {
                        Status = true,
                        StatusCode = "200",
                        Message = "Your Package Buy Successfully",
                        Data = null,
                    });

                return BadRequest(new ResponseDto()
                {
                    Status = false,
                    StatusCode = "500",
                    Message = GlobalMessages.SystemFailureMessage,
                    Data = null,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto()
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Error: " + ex.Message,
                    Data = null
                });
            }
        }

        [CustomAuthorize(EnumRoles.Admin)]
        [HttpPost("StripeRefund")]
        public async Task<IActionResult> StripeRefund(string chargeId, int orderId)
        {
            try
            {
                var refundResult = await ProcessRefund(chargeId, orderId);

                if (refundResult)
                    return Ok(new ResponseDto()
                    {
                        Status = true,
                        StatusCode = "200",
                        Message = "Refunded",
                        Data = null
                    });

                return BadRequest(new ResponseDto()
                {
                    Status = false,
                    StatusCode = "400",
                    Message = "Failed To Refund",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(404, new ResponseDto()
                {
                    Status = false,
                    StatusCode = "404",
                    Message = "Error: " + ex.Message,
                    Data = null
                });
            }
        }

        [CustomAuthorize(EnumRoles.Admin)]
        [HttpPost("CancelOrderAndRevertSession")]
        public async Task<IActionResult> CancelOrderAndRevertSession(int orderId)
        {
            var result = await _paypalGatewayService.CancelOrderAndRevertSessionAsync(orderId);

            return result
                ? Ok(new ResponseDto()
                {
                    Status = true,
                    StatusCode = "200",
                    Message = "Order Cancelled",
                    Data = null
                })
                : BadRequest(new ResponseDto()
                {
                    Status = false,
                    StatusCode = "400",
                    Message = "Session Not Updated",
                    Data = null
                });
        }

        [HttpPost("StripeWithdrawAsync")]
        public async Task<IActionResult> StripeWithdrawAsync(string amount, int userId)
        {
            try
            {
                var user = await _userRepo.GetUserById(userId);

                if (user?.IsBankAccountAdded != 1)
                    return BadRequest(new ResponseDto() { 
                        Status = false, StatusCode = "400", Message = "Bank account not linked", Data = null
                    });

                var result = await ProcessWithdrawal(amount, user.StripeId);

                if (result)
                    return Ok(new ResponseDto() { 
                        Status = true, StatusCode = "200", Message = "Withdrawal Successful", Data = null 
                    });

                return BadRequest(new ResponseDto() {
                    Status = false, StatusCode = "400", Message = "Withdrawal Failed", Data = null 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto()
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Error: " + ex.Message,
                    Data = null
                });
            }
        }

        [HttpDelete("DeleteStripeAccount")]
        public IActionResult DeleteStripeAccount(string? account = null)
        {
            try
            {
                var service = new AccountService();
                service.Delete(account);

                return Ok(new ResponseDto()
                {
                    Status=true,
                    StatusCode="200",
                    Message="Accounts Deleted",
                    Data= null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto()
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Error: " + ex.Message,
                    Data = null
                });
            }
        }

        private async Task<string> ProcessCharge(CheckOutDTO checkOutData, int orderId, bool isPaymentForPackage)
        {
            try
            {
                string chargePayment = string.Empty;
                double amount = Convert.ToDouble(checkOutData.TotalWorkCharges); // Assuming TotalWorkCharges is the payment amount
                long amountInCents = (long)(amount * 100); // Stripe requires amount in cents

                var customerService = new CustomerService();
                var chargeService = new ChargeService();

                var customer = await customerService.CreateAsync(new CustomerCreateOptions
                {
                    Email = checkOutData.stripeEmail,
                    Source = checkOutData.stripeToken,
                });

                var chargeOptions = new ChargeCreateOptions
                {
                    Amount = amountInCents,
                    Currency = "CAD",
                    Description = checkOutData.PaymentTitle ?? "Payment for Services",
                    Customer = customer.Id,
                };

                var charge = await chargeService.CreateAsync(chargeOptions);

                if (charge.Paid)
                {
                    chargePayment = charge.Id;
                }

                if (chargePayment != string.Empty)
                {
                    bool updateOrderStatus = await UpdateOrder(checkOutData?.TotalWorkCharges!,
                        checkOutData?.ActualOrderPrice!, chargePayment, orderId);
                    if (!updateOrderStatus)
                    {
                        return string.Empty;
                    }
                }

                return chargePayment;
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage($"Error occurred during payment processing: {ex.Message} - {ex.StackTrace}");
                return string.Empty;
            }
        }

        private async Task<bool> ProcessWithdrawal(string amount, string stripeId)
        {
            try
            {
                decimal decimalAmount = decimal.Parse(amount, CultureInfo.InvariantCulture);
                int amountInCents = (int)(decimalAmount * 100);

                var payout_to_bank = new PayoutCreateOptions
                {
                    Amount = amountInCents,
                    Currency = "CAD",
                };

                var requestOptions = new RequestOptions();
                requestOptions.StripeAccount = stripeId;

                var payoutService = new PayoutService();
                var payoutCreate = await payoutService.CreateAsync(payout_to_bank, requestOptions);
                return true;
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(_projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return false;
            }
        }

        private async Task<bool> ProcessRefund(string chargeId, int OrderId)
        {
            try
            {
                var refundService = new RefundService();
                var refundOptions = new RefundCreateOptions
                {
                    Charge = chargeId,
                };

                var refund = refundService.Create(refundOptions);
                if (refund.Status == "succeeded")
                {
                    bool updateStripeStatus = await _orderRepo.ChangeStripePaymentStatus(OrderId, StripePaymentStatus.Refunded);
                    bool updateOrderStatusForCancel = await _orderRepo.UpdateOrderStatusForCancel(OrderId);

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(_projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return false;
            }
        }

        private async Task<bool> UpdateOrder(string orderCharges, string actualOrderAmount,
            string paymentCharged, int orderId)
        {
            // Parse input strings to decimals with error handling
            if (!decimal.TryParse(orderCharges, out var orderChargesValue) ||
                !decimal.TryParse(actualOrderAmount, out var actualOrderAmountValue))
            {
                throw new ArgumentException("Invalid orderCharges or actualOrderAmount values provided.");
            }

            // Retrieve the order by ID
            var order = await _orderRepo.GetOrderById(orderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order with ID {orderId} not found.");
            }

            // Update order fields
            order.IsActive = 1;
            order.TotalAmountIncludedFee = orderChargesValue;
            order.OrderPrice = actualOrderAmountValue;

            // Update package-related fields if applicable
            if (order.PackageId.HasValue)
            {
                order.PackageBuyFrom = "STRIPE";
                order.StripeStatus = (int)StripePaymentStatus.SessionUsed;
            }

            // Update payment-related fields if provided
            if (!string.IsNullOrEmpty(paymentCharged))
            {
                order.StripeChargeId = paymentCharged;
            }

            // Save changes to the database
            var updateResult = await _orderRepo.UpdateOrder(order);
            return updateResult;
        }

        private async Task<string> CreateStripeChargeAsync(string email, string token, string description, string amount, string currency = "CAD")
        {
            try
            {
                var amountInCents = (long)(Convert.ToDouble(amount) * 100);

                var customerService = new CustomerService();
                var chargeService = new ChargeService();

                var customer = await customerService.CreateAsync(new CustomerCreateOptions
                {
                    Email = email,
                    Source = token,
                });

                var chargeOptions = new ChargeCreateOptions
                {
                    Amount = amountInCents,
                    Currency = currency,
                    Description = description,
                    Customer = customer.Id,
                };

                var charge = await chargeService.CreateAsync(chargeOptions);

                return charge.Paid ? charge.Id : string.Empty;
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(_projectVariables.BaseUrl + " ----------<br>" + ex.Message + "---------------" + ex.StackTrace);
                return string.Empty;
            }
        }

        private async Task<string> ProcessChargeForPackage(PackageCOutRequest package, string packagePrice)
        {
            return await CreateStripeChargeAsync(package.StripeEmail, package.StripeToken, package.Description ?? "Package Purchase", packagePrice);
        }

        private async Task<bool> UpdatePackage(int? packageId, string workingHours = "")
        {
            var getUserPackage = await _userPackageService.GetUserPackageById(packageId.Value);
            var sss = Math.Round(Convert.ToDecimal(workingHours));
            var userConsumingSession = Convert.ToInt32(sss);
            var remainingSessions = getUserPackage.RemainingSessions - userConsumingSession;
            getUserPackage.RemainingSessions = remainingSessions;
            if (await _userPackageService.UpdateUserPackageSession(getUserPackage))
            {
                return true;
            }
            return false;
        }
    }
}
