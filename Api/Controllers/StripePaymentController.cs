using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using System.Globalization;
using System.Runtime.CompilerServices;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ITValet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripePaymentController : ControllerBase
    {
        private readonly IOrderRepo orderRepo;
        private readonly IUserRepo userRepo;
        private readonly IOrderReasonRepo orderReasonRepo;
        private readonly IOfferDetailsRepo offerDetailService;
        private readonly IPayPalGateWayService payPalGateWayService;
        private readonly IJwtUtils jwtUtils;
        private readonly INotificationService userPackageService;

        public StripePaymentController(IUserRepo _userRepo, IJwtUtils _jwtUtils,
             IOrderRepo _orderRepo, IOrderReasonRepo _orderReasonRepo, IPayPalGateWayService _payPalGateWayService, INotificationService _userPackageService, IOfferDetailsRepo _offerDetailService)
        {
            userRepo = _userRepo;
            jwtUtils = _jwtUtils;
            orderRepo = _orderRepo;
            orderReasonRepo = _orderReasonRepo;
            userPackageService = _userPackageService;
            offerDetailService = _offerDetailService;
            payPalGateWayService = _payPalGateWayService;
        }

        [HttpPost("CreateStripeCharge")]
        public async Task<IActionResult> CreateStripeCharge(CheckOutDTO checkOutData)
        {

            var order = new Order
            {
                OrderTitle = checkOutData.PaymentTitle,
                OrderDescription = checkOutData.PaymentDescription,
                StartDateTime = Convert.ToDateTime(checkOutData.FromDateTime),
                EndDateTime = Convert.ToDateTime(checkOutData.ToDateTime),
                ValetId = Convert.ToInt32(checkOutData.ValetId),
                CustomerId = Convert.ToInt32(checkOutData.customerId),
                OfferId = checkOutData.OfferId,
                PackageId = checkOutData.PackageId,
                IsActive = 0,
                OrderStatus = 0,
                IsDelivered = 0,
                OrderPrice = 0,
                TotalAmountIncludedFee = 0,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };

            var OrderId = await orderRepo.GetOrderId(order);

            if (OrderId == -1)
            {
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "404",
                    Message = GlobalMessages.SystemFailureMessage
                });
            }

            var chargePayment = "";
            var updateOrder = false;
            var updateUserPackage = false;
            if (checkOutData.PackageId == null)
            {
                chargePayment = await ChargeAsync(checkOutData.stripeEmail, checkOutData.stripeToken, checkOutData.PaymentTitle, checkOutData.TotalWorkCharges, OrderId, false);
                updateOrder = await postUpdateOrder(checkOutData.TotalWorkCharges, checkOutData.ActualOrderPrice, chargePayment, OrderId);
            }
            else
            {
                updateOrder = await postUpdateOrder(checkOutData.TotalWorkCharges, checkOutData.ActualOrderPrice, "", OrderId);
                updateUserPackage = await postUpdatePackage(checkOutData.PackageId, checkOutData.WorkingHours);

            }
            if (!updateOrder)
            {
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "404",
                    Message = GlobalMessages.SystemFailureMessage
                });
            }

            // for changing the offer status in messages
            if (checkOutData.OfferId != null)
            {
                bool isOfferUpdate = await offerDetailService.UpdateOfferStatus(OrderId, checkOutData.OfferId);

            }


            return Ok(new ResponseDto
            {
                Id = StringCipher.EncryptId(OrderId),
                Status = true,
                StatusCode = "200",
                Message = GlobalMessages.SuccessMessage
            });
        }

        [HttpPost("StripeCheckOutForPackages")]
        public async Task<IActionResult> StripeCheckOutForPackages(PackageCOutRequest checkOut)
        {
            try
            {
                UserPackage package = new UserPackage();

                string PackagePrice;

                if (checkOut.SelectedPackage == "IYear")
                {
                    //Calculate start and end dates for a 1-year package
                    DateTime startDate = DateTime.Now;
                    DateTime endDate = startDate.AddYears(1);
                    PackagePrice = "100";
                    package.StartDateTime = startDate;
                    package.EndDateTime = endDate;
                    package.PackageType = 1;
                    package.PackageName = checkOut.SelectedPackage;
                    package.TotalSessions = 6;
                    package.RemainingSessions = 6;
                    package.CustomerId = checkOut.ClientId;

                }
                else if (checkOut.SelectedPackage == "2Year")
                {
                    // Calculate start and end dates for a 2-year package
                    DateTime startDate = DateTime.Now;
                    DateTime endDate = startDate.AddYears(2);
                    PackagePrice = "200";
                    package.StartDateTime = startDate;
                    package.EndDateTime = endDate;
                    package.PackageType = 2;
                    package.PackageName = checkOut.SelectedPackage;
                    package.TotalSessions = 12;
                    package.RemainingSessions = 12;
                    package.CustomerId = checkOut.ClientId;

                }
                else
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "InvalidSelection" });
                }

                int userPackageId = -1;
                try
                {
                    userPackageId = await userPackageService.AddUserPackageAndGetId(package);
                }
                catch (Exception ex)
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "An error occurred during package processing" });
                }

                if (userPackageId != -1)
                {
                    var ChargePackagePayment = await ChargeAsync(checkOut.stripeEmail,
                                                          checkOut.stripeToken,
                                                          checkOut.SelectedPackage,
                                                          PackagePrice = "100", 0, true);


                    if (!string.IsNullOrEmpty(ChargePackagePayment))
                    {
                        var updateUserPackage = await userPackageService.UpdateUserPackage(userPackageId, "STRIPE");
                        if (updateUserPackage == true)
                        {
                            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Your Package Buy Successfully" });
                        }
                    }

                }

                return Ok(new ResponseDto() { Status = false, StatusCode = "500", Message = "Something went wrong" });
            }
            catch (Exception ex)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "500", Message = "An unexpected error occurred" });
            }
        }

        

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpPost("StripeRefund")]
        public async Task<IActionResult> StripeRefund(string chargeId, int OrderId)
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
                    bool updateStripeStatus = await orderRepo.ChangeStripePaymentStatus(OrderId, StripePaymentStatus.Refunded);
                    bool updateOrderStatusForCancel = await orderRepo.UpdateOrderStatusForCancel(OrderId);

                    return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Refunded" });
                }
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "FailedToRefund" });
            }
            catch (Exception ex)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = "Something's went wrong" });
            }

        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpPost("CancelOrderAndRevertSession")]
        public async Task<IActionResult> CancelOrderAndRevertSession(string OrderId)
        {
            int id = Convert.ToInt32(OrderId);
            var isSessionRevert = await payPalGateWayService.CancelOrderAndRevertSessionAsync(id);
            if (isSessionRevert)
            {
                return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Order Cancelled" });
            }

            return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Session Not Updated" });
        }
        /*
                [HttpPost("StripeWithdrawAsync")]
                public async Task<bool> StripeWithdrawAsync(string amount, string userId)
                {
                    try
                    {
                        int Id = Convert.ToInt32(userId);
                        var user = await userRepo.GetUserById(Id);
                        if (user.IsBankAccountAdded == 1)
                        {
                            var payout_to_bank = new PayoutCreateOptions
                            {
                                Amount = Convert.ToInt32(amount) * 100,
                                Currency = "CAD",
                            };
                            var requestOptions = new RequestOptions();
                            requestOptions.StripeAccount = user.StripeId;

                            var payoutService = new PayoutService();
                            var payoutCreate = await payoutService.CreateAsync(payout_to_bank, requestOptions);

                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }
                }*/

        [HttpPost("StripeWithdrawAsync")]
        public async Task<bool> StripeWithdrawAsync(string amount, string userId)
        {
            try
            {
                int Id = Convert.ToInt32(userId);
                var user = await userRepo.GetUserById(Id);

                if (user.IsBankAccountAdded == 1)
                {
                    // Parse the amount as decimal and convert to cents (integer)
                    decimal decimalAmount = decimal.Parse(amount, CultureInfo.InvariantCulture);
                    int amountInCents = (int)(decimalAmount * 100);

                    var payout_to_bank = new PayoutCreateOptions
                    {
                        Amount = amountInCents,
                        Currency = "USD",
                    };

                    var requestOptions = new RequestOptions();
                    requestOptions.StripeAccount = user.StripeId;

                    var payoutService = new PayoutService();
                    var payoutCreate = await payoutService.CreateAsync(payout_to_bank, requestOptions);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Handle the exception (log, etc.) if needed
                return false;
            }
        }

        [HttpDelete("DeleteStripeAccount")]
        public bool DeleteAccount(string? account ="")
        {
            try
            {
                List<string> myList = new List<string>();
                if (!string.IsNullOrEmpty(account))
                {
                    myList = new List<string>
                    {
                        account
                    };
                }
                else
                {
                    myList = new List<string>
                    {
                        "acct_1OhAuYR487Ktnl9T",
                        "acct_1OgosWQwqtJ8Iz7f",
                        "acct_1OgjoiR0xeeCubnH",
                        "acct_1Ogjk6Qt3XT3e9bp",
                        "acct_1OfIwaR02CdScAON",
                        "acct_1OfIwXQrF1gbh7zb",
                        "acct_1OfIIDR9opcODduy",
                        "acct_1OHNq2R31Tbxj1Ly",
                        "acct_1OHNq0QwzUvS6ZwH",
                        "acct_1OHNpzR4ShgxRsNB",
                        "acct_1OH1jYQuzgxyAIg1",
                        "acct_1OFx3ZQqwLvxkEeV",
                        "acct_1OFwtxR7wIx3OFuN",
                        "acct_1O7es6QqK4Yw5cAG",
                        "acct_1O4fqAR9HJbWgxOL"
                    };
                }
                foreach (var item in myList)
                {
                    var service = new AccountService();
                    service.Delete(item);
                }
                return true;
            }
            catch (Exception ex)
            {
                var x = ex.Message;
                return false;
            }
        }

        private async Task<bool> postUpdateOrder(string orderCharges, string actualOrderAmount,
            string paymentCharged, int orderId)
        {
            var OrderCharges = Convert.ToDecimal(orderCharges);
            var actualAmount = Convert.ToDecimal(actualOrderAmount);
            var getOrder = await orderRepo.GetOrderById(orderId);
            getOrder.IsActive = 1;
            getOrder.TotalAmountIncludedFee = OrderCharges;
            getOrder.OrderPrice = actualAmount;
            if (getOrder.PackageId != null)
            {
                getOrder.PackageBuyFrom = "STRIPE";
                getOrder.StripeStatus = (int)StripePaymentStatus.SessionUsed;
            }

            if (!string.IsNullOrEmpty(paymentCharged))
            {
                getOrder.StripeChargeId = paymentCharged;
            }
            if (await orderRepo.UpdateOrder(getOrder))
            {
                return true;
            }
            return false;
        }

        private async Task<string> ChargeAsync(string stripeEmail, string stripeToken, string stripeDescription = "", string OrderAmount = "", int OrderId = 0, bool IsPaymentForPackage = false)
        {
            try
            {
                var Amount = Convert.ToDouble(OrderAmount);
                long OrderPrice = (long)Amount;
                var customerService = new CustomerService();
                var chargeService = new ChargeService();
                var serviceToken = new TokenService();

                var customer = await customerService.CreateAsync(new CustomerCreateOptions
                {
                    Email = stripeEmail,
                    Source = stripeToken,
                });

                var options = new ChargeCreateOptions
                {
                    Amount = OrderPrice * 100,
                    Currency = "USD",
                    Description = stripeDescription,
                    Customer = customer.Id,
                };

                var charge = await chargeService.CreateAsync(options);

                if (charge.Paid)
                {
                    if (OrderId != 0)
                    {
                        bool updateStripePaymentStatus = await orderRepo.ChangeStripePaymentStatus(OrderId, StripePaymentStatus.PaymentReceived);
                        if (updateStripePaymentStatus)
                        {
                            return charge.Id;
                        }
                    }
                    else
                    {
                        return charge.Id;
                    }
                }
                else
                {
                    if (OrderId != 0)
                    {
                        bool updateStripePaymentStatus = await orderRepo.ChangeStripePaymentStatus(OrderId, StripePaymentStatus.PaymentNotReceived);
                    }
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        private async Task<bool> postUpdatePackage(int? packageId, string workingHours = "")
        {
            var getUserPackage = await userPackageService.GetUserPackageById(packageId.Value);
            var sss = Math.Round(Convert.ToDecimal(workingHours));
            var userConsumingSession = Convert.ToInt32(sss);
            var remainingSessions = getUserPackage.RemainingSessions - userConsumingSession;
            getUserPackage.RemainingSessions = remainingSessions;
            if (await userPackageService.UpdateUserPackageSession(getUserPackage))
            {
                return true;
            }
            return false;
        }
    }
}
