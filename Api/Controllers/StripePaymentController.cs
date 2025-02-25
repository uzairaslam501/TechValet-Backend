using ITValet.HelpingClasses;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.NotificationHub;
using ITValet.Services;
using ITValet.Utils.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace ITValet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripePaymentController : ControllerBase
    {
        private readonly IUserRepo _userRepo;
        private readonly IOrderRepo _orderRepo;
        private readonly IConfiguration _configuration;
        private readonly IMessagesRepo _messageService;
        private readonly ProjectVariables _projectVariables;
        private readonly IOfferDetailsRepo _offerDetailService;
        private readonly INotificationService _userPackageService;
        private readonly IPayPalGateWayService _paypalGatewayService;
        private readonly IHubContext<NotificationHubSocket> _notificationHubSocket;

        public StripePaymentController(IUserRepo userRepo, IOptions<ProjectVariables> options, IConfiguration configuration,
            IOrderRepo orderRepo, IPayPalGateWayService paypalGateWayService, INotificationService userPackageService, 
            IOfferDetailsRepo offerDetailService, IHubContext<NotificationHubSocket> notificationHubSocket,
            IMessagesRepo messageService)
        {
            _userRepo = userRepo;
            _orderRepo = orderRepo;
            _configuration = configuration;
            _messageService = messageService;
            _projectVariables = options.Value;
            _userPackageService = userPackageService;
            _offerDetailService = offerDetailService;
            _paypalGatewayService = paypalGateWayService;
            _notificationHubSocket = notificationHubSocket;
        }

        [HttpPost("create-checkout-session/{userId}")]
        public async Task<IActionResult> CreateCheckoutSession(string userId, CheckOutDTO checkoutDTO)
        {
            try
            {
                var getUser = await _userRepo.GetUserById(Convert.ToInt32(userId));
                if (getUser != null)
                {
                    string duration = GeneralPurpose.CalculcateTimeDifference(checkoutDTO?.FromDateTime, checkoutDTO?.ToDateTime);
                    string startTo = checkoutDTO?.FromDateTime;
                    string endTo = checkoutDTO?.ToDateTime;
                    string customerId = checkoutDTO?.CustomerId?.ToString() ?? "";
                    string valetId = checkoutDTO?.ValetId?.ToString() ?? "";
                    string offerId = checkoutDTO?.OfferId?.ToString() ?? "";

                    
                    if (getUser.StripeId == null)
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

                    var priceOptions = new PriceCreateOptions
                    {
                        UnitAmountDecimal = decimal.Parse(checkoutDTO.ActualOrderPrice) * 100, // Stripe uses cents
                        Currency = _projectVariables.PaymentCurrency, // Adjust currency if needed
                        ProductData = new PriceProductDataOptions
                        {
                            Name = $"Title: {checkoutDTO.PaymentTitle}" ?? "Payment",
                            StatementDescriptor = checkoutDTO.PaymentDescription
                        },
                    };

                    var priceService = new PriceService();
                    Price price = await priceService.CreateAsync(priceOptions);


                    // Checkout session for one-time payment
                    var options = new SessionCreateOptions
                    {
                        SuccessUrl = $"{_projectVariables.ReactUrl}PaymentSuccessfully?session_id={{CHECKOUT_SESSION_ID}}",
                        CancelUrl = $"{_projectVariables.ReactUrl}PaymentCancelled?session_id={{CHECKOUT_SESSION_ID}}",
                        PaymentMethodTypes = new List<string> { "card" },
                        Mode = "payment",                        
                        LineItems = new List<SessionLineItemOptions>
                        {
                            new SessionLineItemOptions
                            {
                                Price = price.Id,
                                Quantity = 1,
                            },
                            
                        },
                        Customer = getUser?.StripeId,
                        Metadata = new Dictionary<string, string>
                        {
                            { "ValetId", valetId },
                            { "OfferId", offerId },
                            { "CustomerId", customerId },
                            { "ToTime", checkoutDTO.ToDateTime.ToString() },
                            { "FromTime", checkoutDTO.FromDateTime.ToString() },
                            { "PaymentDescription", checkoutDTO.PaymentDescription },
                            { "PaymentTitle", checkoutDTO.PaymentTitle ?? "Payment" },
                            { "TotalWorkCharges", checkoutDTO.TotalWorkCharges!.ToString() },
                            { "ActualOrderPrice", checkoutDTO.ActualOrderPrice.ToString() },
                            { "Duration", GeneralPurpose.CalculcateTimeDifference(checkoutDTO.FromDateTime, checkoutDTO.ToDateTime) },
                            { "StripeEmail", getUser?.Email! },
                            { "StripeId", getUser?.StripeId! },
                        }
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


                        return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "You Are Ready To Proceed, Hit Confirm CheckOut Button.", data));

                    }
                    catch (StripeException e)
                    {
                        return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", e.StripeError.Message));
                    }
                }

                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));

            }
            catch (StripeException e)
            {
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", e.StripeError.Message));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpPost("payment-intent/{userId}")]
        public async Task<IActionResult> CreatePaymentIntent(string userId, CheckOutDTO checkoutDTO)
        {
            try
            {
                var paymentIntentService = new PaymentIntentService();
                var sss = Convert.ToDouble(checkoutDTO.ActualOrderPrice) * 100;
                var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = (long)sss, // Amount in cents
                    Currency = _projectVariables.PaymentCurrency,
            });

                return Ok(new ResponseDto() { Data = paymentIntent.ClientSecret, Status = true, StatusCode = "200", Message="Cleint Secret" });
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return Ok(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpGet]
        [Route("payment-success/{session_id}")]
        public async Task<IActionResult> PaymentSuccess(string session_id)
        {
            try
            {
                if (string.IsNullOrEmpty(session_id))
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", "Session ID is required."));
                
                var sessionService = new SessionService();
                var session = await sessionService.GetAsync(session_id);

                if (session != null)
                {
                    var metadata = session.Metadata;
                    var data = new {
                        CustomerId = metadata.ContainsKey("CustomerId") ? metadata["CustomerId"] : "N/A",
                        ValetId = metadata.ContainsKey("ValetId") ? metadata["ValetId"] : "N/A",
                        OfferId = metadata.ContainsKey("OfferId") ? metadata["OfferId"] : "N/A",
                        PaymentTitle = metadata.ContainsKey("PaymentTitle") ? metadata["PaymentTitle"] : "N/A",
                        PaymentDescription = metadata.ContainsKey("PaymentDescription") ? metadata["PaymentDescription"] : "N/A",
                        TotalWorkCharges = metadata.ContainsKey("TotalWorkCharges") ? metadata["TotalWorkCharges"] : "0",
                        ActualOrderPrice = metadata.ContainsKey("ActualOrderPrice") ? metadata["ActualOrderPrice"] : "0",
                        FromDateTime = metadata.ContainsKey("FromTime") ? metadata["FromTime"] : "N/A",
                        ToDateTime = metadata.ContainsKey("ToTime") ? metadata["ToTime"] : "N/A",
                        duration = metadata.ContainsKey("Duration") ? metadata["Duration"] : "N/A",
                        StripeEmail = metadata.ContainsKey("StripeEmail") ? metadata["StripeEmail"] : "N/A",
                        StripeId = metadata.ContainsKey("StripeId") ? metadata["StripeId"] : "N/A",
                        PaymentId = session.PaymentIntentId,
                    };

                    // Return the metadata as part of the response
                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Payment successful", data));
                }

                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", "Invalid session or session not found."));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return Ok(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpPost("CreateStripeCharge")]
        public async Task<IActionResult> CreateStripePayment(DirectOrderDTO stripePayment)
        {
            try
            {

                if (string.IsNullOrEmpty(stripePayment.ValetId) || string.IsNullOrEmpty(stripePayment?.CustomerId))
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

                var createStripeDto = new CheckOutDTO();
                createStripeDto.StripeId = stripePayment?.StripeId;
                createStripeDto.StripeEmail = stripePayment?.StripeEmail;
                createStripeDto.StripeToken = stripePayment?.StripeToken;
                createStripeDto.PaymentTitle = stripePayment?.Title;
                createStripeDto.PaymentDescription = stripePayment?.Description;
                createStripeDto.ActualOrderPrice = stripePayment?.ActualOrderPrice;
                createStripeDto.TotalWorkCharges = stripePayment?.TotalWorkCharges;
                createStripeDto.FromDateTime = stripePayment?.FromDateTime;
                createStripeDto.ToDateTime = stripePayment?.ToDateTime;
                createStripeDto.WorkingHours = stripePayment?.WorkingHours;
                createStripeDto.ValetId = StringCipher.DecryptionId(stripePayment?.ValetId!).ToString();
                createStripeDto.CustomerId = StringCipher.DecryptionId(stripePayment?.CustomerId!).ToString();
                createStripeDto.OfferId = !string.IsNullOrEmpty(stripePayment?.OfferId) ? Convert.ToInt32(stripePayment?.OfferId!) : null;

                var response = await CreateStripeCharge(createStripeDto);
                if (response?.Status == true)
                    return Ok(response);

                return BadRequest(response);
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", "Payment cannot be prcocessed at the current moment please try again later"));
            }
        }

        [HttpPost("CreateStripeChargeForPackage")]
        public async Task<IActionResult> CreateStripeChargeForPackage(PackageOrderDTO stripePayment)
        {

            try
            {
                if (string.IsNullOrEmpty(stripePayment.ValetId) ||
                    string.IsNullOrEmpty(stripePayment?.CustomerId) ||
                    string.IsNullOrEmpty(stripePayment?.PackageId)
                    )
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

                var createStripeDto = new CheckOutDTO();
                createStripeDto.PaymentTitle = stripePayment?.Title;
                createStripeDto.ToDateTime = stripePayment?.ToDateTime;
                createStripeDto.FromDateTime = stripePayment?.FromDateTime;
                createStripeDto.WorkingHours = stripePayment?.WorkingHours;
                createStripeDto.PackagePaidBy = stripePayment?.PackagePaidBy;
                createStripeDto.PaymentDescription = stripePayment?.Description;
                createStripeDto.TotalWorkCharges = stripePayment?.TotalWorkCharges;
                createStripeDto.ActualOrderPrice = stripePayment?.ActualOrderPrice;
                createStripeDto.ValetId = StringCipher.DecryptionId(stripePayment?.ValetId!).ToString();
                createStripeDto.CustomerId = StringCipher.DecryptionId(stripePayment?.CustomerId!).ToString();
                createStripeDto.OfferId = !string.IsNullOrEmpty(stripePayment?.OfferId) ? Convert.ToInt32(stripePayment?.OfferId!) : null;
                createStripeDto.PackageId = !string.IsNullOrEmpty(stripePayment?.PackageId) ? Convert.ToInt32(stripePayment?.PackageId) : null;

                var response = await CreateStripeCharge(createStripeDto);
                if (response?.StatusCode == "200")
                    return Ok(response);

                return BadRequest(response);
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return Ok(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        private async Task<ResponseDto> CreateStripeCharge(CheckOutDTO checkOutData)
        {
            var order = InitializeOrder(checkOutData);
            var orderId = await _orderRepo.GetOrderId(order);
            var getLoggedInUser = await _userRepo.GetUserById((int)order.CustomerId!);
            if (orderId == -1)
                return GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage, null);

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
                isPackageUpdated = await UpdatePackage(checkOutData!.PackageId, checkOutData?.WorkingHours!);
            }

            if (!isOrderUpdated)
                return GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage, null);

            if (checkOutData?.OfferId! != null)
                await _offerDetailService.UpdateOfferStatus(orderId, checkOutData.OfferId);


            return GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.SuccessMessage, orderId);
        }

        [HttpPost("StripeCheckOutForPackages")]
        public async Task<IActionResult> StripeCheckOutForPackages(PackageCOutRequest checkOut)
        {
            try
            {
                var package = StripeHelper.InitializePackage(checkOut, out var packagePrice);
                var chargeResult = await ProcessChargeForPackage(checkOut, packagePrice);
                
                if (!string.IsNullOrEmpty(chargeResult))
                {
                    var userPackageId = await _userPackageService.AddUserPackageAndGetId(package);
                    return Ok(GeneralPurpose.GenerateResponse(true, "200", $"You have succesfully bought the {package.PackageName} Package", userPackageId));
                }

                return BadRequest(GeneralPurpose.GenerateResponse(false, "400",
                    $"The purchased of {package.PackageName} Package is not successfull. Please try again later"));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return Ok(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [CustomAuthorize(EnumRoles.Admin)]
        [HttpPost("StripeRefund/{orderId}")]
        public async Task<IActionResult> StripeRefund(string orderId, string chargeId)
        {
            try
            {
                var decryptOrderId = StringCipher.DecryptionId(orderId);
                var refundResult = await ProcessRefund(chargeId, decryptOrderId);

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
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return Ok(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [CustomAuthorize(EnumRoles.Admin)]
        [HttpPost("CancelOrderAndRevertSession/{orderId}")]
        public async Task<IActionResult> CancelOrderAndRevertSession(string orderId)
        {
            var decryptOrderId = StringCipher.DecryptionId(orderId);
            var result = await _paypalGatewayService.CancelOrderAndRevertSessionAsync(decryptOrderId, "stripe");

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

        [HttpPost("StripeWithdrawAsync/{userId}")]
        public async Task<IActionResult> StripeWithdrawAsync(string userId)
        {
            try
            {
                var decrypt = StringCipher.DecryptionId(userId);
                var user = await _userRepo.GetUserById(decrypt);
                if (user == null)
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));


                if (user?.IsBankAccountAdded != 1)
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", "Bank account not linked"));

                var response = await StripeHelper.GetStripeEarnings(user!.StripeId!, _projectVariables);
                var result = await ProcessWithdrawal(response.BalanceAvailable, user!.StripeId!);

                if (result)
                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Withdrawal Successful", user));

                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", "Withdrawal Failed"));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", "Withdrawal Failed"));
            }
        }

        [HttpDelete("DeleteStripeAccount")]
        public IActionResult DeleteStripeAccount(string? account = null)
        {
            try
            {
                var service = new AccountService();
                service.Delete(account);

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.DeletedMessage));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpPost("create-account/{userId}")]
        public async Task<IActionResult> CreateAccount(string userId, string email = "")
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", "Invalid input parameters."));
                
                var decrypt = StringCipher.DecryptionId(userId);
                var user = await _userRepo.GetUserById(decrypt);
                
                var account = await StripeHelper.CreateStripeAccount(email, _projectVariables.ReactUrl, _projectVariables.PaymentCurrency);
                var verificationResult = await StripeHelper.VerifyAccount(account.Id, _projectVariables.ReactUrl);

                user!.StripeId = account.Id;
                user.IsVerify_StripeAccount = 0;
                await _userRepo.UpdateUser(user);

                var responseList = new List<string>
                {
                    verificationResult,
                    account.Id
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Verify Your Account", responseList));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpGet("account-verified/{userId}")]
        public async Task<IActionResult> AccountVerification(string userId, string stripeId)
        {
            try
            {
                var decrypt = StringCipher.DecryptionId(userId);
                var getUser = await _userRepo.GetUserById(decrypt);

                var toaster = "";
                var verificationResult = "";
                if (getUser!.StripeId == stripeId)
                {
                    var response = await StripeHelper.StripeAccountStatus(stripeId);
                    if (response.Status == false)
                    {
                        verificationResult = await StripeHelper.VerifyAccount(stripeId, _projectVariables.ReactUrl);
                        toaster = "You have to complete your stripe account information to verify the account";
                    }
                    else
                    {
                        getUser!.IsVerify_StripeAccount = 1;
                        await _userRepo.UpdateUser(getUser);
                        toaster = "Account Verified successfully";
                    }
                    var newDto = new
                    {
                        getUser = getUser,
                        verificationResult = verificationResult
                    };
                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", toaster, newDto));
                }
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", ex.Message));
            }
        }

        [HttpPost("add-bank-account/{userId}")]
        public async Task<IActionResult> AddExternalBankAccountToStripe(string userId, StripeBankAccountDto bankDto)
        {
            var response = new ResponseDto();
            try
            {
                var decrypt = StringCipher.DecryptionId(userId);
                var getUser = await _userRepo.GetUserById(decrypt);
                if (getUser.IsBankAccountAdded != 1)
                {
                    response = await StripeHelper.StripeAccountStatus(bankDto.stripeAccountId);
                    if (response.Data != "Completed")
                    {
                        if (bankDto.bankAccountNumber.Contains("\t"))
                        {
                            string keyword = "\t";
                            string result = bankDto.bankAccountNumber.Replace(keyword, string.Empty);
                            bankDto.bankAccountNumber = result;
                        }

                        var options = new ExternalAccountCreateOptions
                        {
                            ExternalAccount = new AccountBankAccountOptions
                            {
                                AccountNumber = bankDto.bankAccountNumber,
                                AccountHolderName = bankDto.accountHolderName,
                                AccountHolderType = "individual",
                                Country = PaymentCountry.Country,
                                RoutingNumber = bankDto.routingNo,
                                Currency = _projectVariables.PaymentCurrency,
                            }
                        };
                        var service = new ExternalAccountService();

                        try
                        {
                            service.Create(bankDto.stripeAccountId, options);
                        }
                        catch (Exception ex)
                        {
                            GeneralPurpose.CreateLogger(_projectVariables, ex);
                            return BadRequest(GeneralPurpose.GenerateResponseCode(true, "400",
                                $"{ex.Message}"));
                        }
                    }
                    getUser.IsBankAccountAdded = 1;
                    if (!await _userRepo.UpdateUser(getUser))
                        return BadRequest(response);
                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Account details added successfully.", getUser));
                }
                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Bank account detail already attached.", getUser));
            }
            catch(Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(true, "400", GlobalMessages.SystemFailureMessage)); ;
            }
        }

        private Order InitializeOrder(CheckOutDTO obj)
        {
            return new Order
            {
                OrderTitle = obj.PaymentTitle,
                OrderDescription = obj.PaymentDescription,
                StartDateTime = DateTime.Parse(obj.FromDateTime!),
                EndDateTime = DateTime.Parse(obj.ToDateTime!),
                ValetId = int.Parse(obj.ValetId!),
                CustomerId = int.Parse(obj.CustomerId!),
                OfferId = obj.OfferId,
                PackageId = obj.PackageId,
                IsActive = 0,
                OrderStatus = 0,
                IsDelivered = 0,
                OrderPrice = 0,
                TotalAmountIncludedFee = 0,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
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
                    Email = checkOutData.StripeEmail,
                    Source = checkOutData.StripeToken,
                });
                
                var chargeOptions = new ChargeCreateOptions
                {
                    Amount = amountInCents,
                    Currency = _projectVariables.PaymentCurrency,
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

        private async Task<bool> ProcessWithdrawal(decimal amount, string stripeId)
        {
            try
            {
                int amountInCents = (int)(amount * 100);

                var payout_to_bank = new PayoutCreateOptions
                {
                    Amount = amountInCents,
                    Currency = _projectVariables.PaymentCurrency,
                };

                var requestOptions = new RequestOptions();
                requestOptions.StripeAccount = stripeId;

                var payoutService = new PayoutService();
                var payoutCreate = await payoutService.CreateAsync(payout_to_bank, requestOptions);
                return true;
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
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
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return false;
            }
        }

        private async Task<bool> UpdateOrder(string orderCharges, string actualOrderAmount,
            string paymentCharged, int orderId)
        {
            try
            {
                // Parse input strings to decimals with error handling
                if (!decimal.TryParse(orderCharges, out var orderChargesValue) ||
                    !decimal.TryParse(actualOrderAmount, out var actualOrderAmountValue))
                    return false;
                
                

                // Retrieve the order by ID
                var order = await _orderRepo.GetOrderById(orderId);
                if (order == null)
                    return false;

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
                    order.StripeChargeId = paymentCharged;

                // Save changes to the database
                var updateResult = await _orderRepo.UpdateOrder(order);
                return updateResult;
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return false;
            }
        }

        private async Task<string> CreateStripeChargeAsync(string email, string token, string description, string amount)
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
                    Currency = _projectVariables.PaymentCurrency,
                    Description = description,
                    Customer = customer.Id,
                };

                var charge = await chargeService.CreateAsync(chargeOptions);

                return charge.Paid ? charge.Id : string.Empty;
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return "";
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
