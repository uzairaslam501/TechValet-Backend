using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ITValet.Services
{
    public interface IPayPalGateWayService
    {
        Task<bool> AddPayPalPackage(PackageCheckOutViewModel package);
        Task<PayPalOrderCheckOut?> GetOrderCheckOutById(int id);
        Task<bool> DeleteCheckOutOrderOfPackages(int orderId);
        Task<bool> UpdateOrderCheckOut(PayPalOrderCheckOut checkout);
        Task<PackageCheckOutViewModel> GetPackageByPaymentId(string paymentId);
        Task<bool> UpdatePackageRecord(PackageCheckOutViewModel package);
        Task<bool> AddPayPalOrder(OrderCheckOutViewModel order);
        Task<bool> CancelOrderAndRevertSessionAsync(int orderId);
        Task<OrderCheckOutViewModel> GetOrderByPaymentId(string paymentId);
        Task<bool> UpdateOrderRecord(OrderCheckOutViewModel order);
        Task<bool> PaymentRefunding(string captureId);
        Task<CaptureAmountViewModel> CapturedAmount(string capturedId);
        Task<ResponseDto> GetPayPalAccount(string userId);
        Task<ResponseDto> DeletePayPalAccount(string userId);
        Task<ResponseDto> AddPayPalAccountInformation(string userId, AddPayPalAccountViewModel paypalAccount);
        Task<bool> AddPayPalTransactionAdminToValet(PayPalFundToValetViewModel transferToValet);
        Task<PayPalFundToValetViewModel> GetPayPalTransactionRecord(string payOutItemId);
        Task<List<PayPalUnclaimedTransactionDetailsForAdminDB>> GetPayPalUnclaimedRecord();
        Task<bool> CancelUnclaimedPayment(string payoutItemId, PaymentCancelViewModel cancelObj);
        
        Task<bool> PayPalOrderCheckoutAccepted(OrderCheckOutAccepted orderCheckout);
        Task<List<PayPalTransactionDetailsForAdminDB>> GetPayPalTransactionsRecord();
        Task<List<PayPalOrderCheckOut>> FindValetForTransferringFundRecord();
        Task<List<PayPalOrderDetailsForAdminDB>> GetPayPalOrdersRecord();
        Task<bool> AddPayPalOrderForPackage(PayPalOrderCheckOutViewModel order);
        Task<bool> OrderCreatedByPayPalPackage(OrderAcceptedOfPackage packageOrder);
        Task<PayPalEarningInCome> GetPayPalEarnings(int valetId);
    }

    public class PayPalGateWayService : IPayPalGateWayService
    {
        private readonly AppDbContext _context;
        private readonly IUserRepo _userService;
        private readonly IOrderRepo _orderService;
        private readonly INotificationService _userPackageService;

        private readonly ProjectVariables _projectVariables;
        public PayPalGateWayService(AppDbContext context, IUserRepo userService, IOrderRepo orderService,
            INotificationService userPackageService, IOptions<ProjectVariables> options)
        {
            _context = context;
            _userService = userService;
            _orderService = orderService;
            _userPackageService = userPackageService;
            _projectVariables = options.Value;
        }

        #region RefactorCode
        public async Task<ResponseDto> GetPayPalAccount(string userId)
        {
            try
            {
                var decrypt = DecryptionId(userId);
                if (decrypt <= 0)
                    return GeneralPurpose.GenerateResponseCode(false, "404", "Record Not Found");
                var obj = await _context.PayPalAccount.FirstOrDefaultAsync(x => x.ValetId == decrypt & x.IsActive == 1);
                return GeneralPurpose.GenerateResponseCode(true, "200", "Record Found", obj);
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage);
            }

        }

        public async Task<ResponseDto> DeletePayPalAccount(string userId)
        {
            try
            {
                var decrypt = DecryptionId(userId);
                if (decrypt <= 0)
                    return GeneralPurpose.GenerateResponseCode(false, "404", "Record Not Found");

                var accountObj = _context.PayPalAccount.
                    FirstOrDefault(x => x.ValetId == decrypt && x.IsActive == 1);
                if (accountObj != null)
                {
                    DeactivatePayPalAccount(accountObj);
                    await _context.SaveChangesAsync();
                    if (!await UpdateUserWithPayPalAccount(accountObj.ValetId ?? 0, true))
                        return GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage);
                }
                return GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.DeletedMessage, null);
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage);
            }

        }

        public async Task<ResponseDto> AddPayPalAccountInformation(string userId, AddPayPalAccountViewModel paypalAccount)
        {
            try
            {
                var decrypt = DecryptionId(userId);

                if (await IsPayPalEmailExists(paypalAccount.PayPalEmail!))
                    return GeneralPurpose.GenerateResponseCode(false, "204", "Email already exists.");

                var existingPayPalAccount = await GetExistingPayPalAccount(decrypt);

                if (existingPayPalAccount != null)
                {
                    UpdatePayPalAccount(existingPayPalAccount, paypalAccount);
                }
                else
                {
                    var newPayPalAccount = MapNewPayPalAccount(decrypt, paypalAccount);
                    _context.PayPalAccount.Add(newPayPalAccount);
                }


                await _context.SaveChangesAsync();
                bool updateUser = await UpdateUserWithPayPalAccount(decrypt, false);
                return GeneralPurpose.GenerateResponseCode(true, "200", "Account information added/updated successfully.");
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return GeneralPurpose.GenerateResponseCode(false, "400", "An error occurred while adding/updating account information.");
            }
        }
        #endregion


        public async Task<bool> AddPayPalPackage(PackageCheckOutViewModel package)
        {
            try
            {
                PayPalPackagesCheckOut obj = new PayPalPackagesCheckOut();
                obj.PaymentId = package.PaymentId;
                obj.PackageType = package.PackageType;
                obj.PackagePrice = package.PackagePrice;
                obj.ClientId = package.ClientId;
                obj.StartDate = package.StartDate;
                obj.UserPackageId = package.UserPackageId;
                obj.EndDate = package.EndDate;
                obj.CreatedAt = DateTime.Now;
                _context.PayPalPackagesCheckOut.Add(obj);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateOrderCheckOut(PayPalOrderCheckOut checkout)
        {
            try
            {
                _context.Entry(checkout).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<PackageCheckOutViewModel> GetPackageByPaymentId(string paymentId)
        {
            try
            {
                var paymentPackage = await _context.PayPalPackagesCheckOut.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
                if (paymentPackage != null)
                {
                    var packageViewModel = new PackageCheckOutViewModel
                    {
                        PaymentId = paymentPackage.PaymentId,
                        PackageType = paymentPackage.PackageType,
                        PackagePrice = paymentPackage.PackagePrice,
                        UserPackageId = paymentPackage.UserPackageId,
                        ClientId = paymentPackage.ClientId,
                    };

                    return packageViewModel;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> UpdatePackageRecord(PackageCheckOutViewModel package)
        {
            try
            {
                var obj = await _context.PayPalPackagesCheckOut.FirstOrDefaultAsync(p => p.PaymentId == package.PaymentId);

                if (obj != null)
                {
                    obj.PaymentStatus = package.PaymentStatus;
                    obj.PayableAmount = package.PayableAmount;
                    obj.UserPackageId = package.UserPackageId;
                    obj.Currency = package.Currency;
                    obj.IsActive = package.IsActive;
                    obj.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> AddPayPalOrder(OrderCheckOutViewModel order)
        {
            try
            {
                PayPalOrderCheckOut obj = new PayPalOrderCheckOut();
                obj.ClientId = order.ClientId;
                obj.OrderPrice = order.OrderPrice;
                obj.OrderId = order.OrderId;
                obj.PaymentId = order.PaymentId;
                obj.ValetId = order.ValetId;
                obj.OrderId = order.OrderId;
                obj.CreatedAt = DateTime.Now;
                _context.PayPalOrderCheckOut.Add(obj);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> AddPayPalOrderForPackage(PayPalOrderCheckOutViewModel order)
        {
            try
            {
                PayPalOrderCheckOut obj = new PayPalOrderCheckOut();
                obj.ClientId = order.ClientId;
                obj.OrderPrice = order.OrderPrice;
                obj.OrderId = order.OrderId;
                obj.ValetId = order.ValetId;
                obj.PaidByPackage = order.PayByPackage;
                obj.OrderId = order.OrderId;
                obj.IsRefund = false;
                obj.PaymentStatus = "USED_SESSION";
                obj.CreatedAt = DateTime.Now;
                obj.IsActive = 1;
                _context.PayPalOrderCheckOut.Add(obj);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<OrderCheckOutViewModel> GetOrderByPaymentId(string paymentId)
        {
            try
            {
                var orderObj = await _context.PayPalOrderCheckOut.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
                if (orderObj != null)
                {
                    var orderCheckout = new OrderCheckOutViewModel
                    {
                        ValetId = orderObj.ValetId,
                        OrderId = orderObj.OrderId,
                        PaymentId = orderObj.PaymentId,
                        ClientId = orderObj.ClientId,
                        OrderPrice = orderObj.OrderPrice,
                        PayPalTransactionFee = orderObj.PayPalTransactionFee,
                        CaptureId = orderObj.CaptureId,
                        AuthorizationId = orderObj.AuthorizationId,
                    };
                    return orderCheckout;
                }
                return null;

            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public async Task<bool> UpdateOrderRecord(OrderCheckOutViewModel order)
        {
            try
            {
                var obj = await _context.PayPalOrderCheckOut.FirstOrDefaultAsync(p => p.PaymentId == order.PaymentId);

                if (obj != null)
                {
                    obj.PaymentStatus = order.PaymentStatus;
                    obj.PayableAmount = order.PayableAmount;
                    obj.Currency = order.Currency;
                    obj.CaptureId = order.CaptureId;
                    obj.AuthorizationId = order.AuthorizationId;
                    obj.PayPalTransactionFee = order.PayPalTransactionFee;
                    obj.IsRefund = order.IsRefund;
                    obj.IsActive = 1;
                    obj.UpdatedAt = DateTime.Now;
                    obj.IsPaymentSentToValet = false;

                    await _context.SaveChangesAsync();

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> PaymentRefunding(string captureId)
        {
            try
            {
                var orderObj = await _context.PayPalOrderCheckOut.FirstOrDefaultAsync(c => c.CaptureId == captureId);
                if (orderObj != null)
                {
                    orderObj.PaymentStatus = "REFUNDED";
                    orderObj.IsActive = 0;
                    orderObj.IsRefund = true;
                    orderObj.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<CaptureAmountViewModel> CapturedAmount(string capturedId)
        {
            try
            {
                var capturedObj = await _context.PayPalOrderCheckOut.FirstOrDefaultAsync(c => c.CaptureId == capturedId);
                if (capturedObj != null)
                {
                    var captureVM = new CaptureAmountViewModel
                    {
                        PayableAmount = capturedObj.PayableAmount,
                        Currency = capturedObj.Currency,
                    };

                    return captureVM;
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        
        public async Task<bool> AddPayPalTransactionAdminToValet(PayPalFundToValetViewModel transferToValet)
        {
            try
            {
                PayPalToValetTransactions obj = new PayPalToValetTransactions()
                {
                    ValetId = transferToValet.ValetId,
                    CustomerId = transferToValet.ClientId,
                    OrderId = transferToValet.OrderId,
                    RecipientEmail = transferToValet.PayPalAccEmail,
                    OrderCheckOutId = transferToValet.OrderCheckOutId,
                    BatchId = transferToValet.BatchId,
                    OrderPrice = transferToValet.OrderPrice,
                    TransactionStatus = transferToValet.TransactionStatus,
                    PayOutItemId = transferToValet.PayOutItemId,
                    PlatformFee = transferToValet.PlatformFee,
                    PaymentId = transferToValet.PaymentId,
                    CreatedAt = GeneralPurpose.DateTimeNow(),
                    IsActive = 1,
                    SentPayment = transferToValet.SentPayment,
                };

                _context.PayPalToValetTransactions.Add(obj);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<PayPalFundToValetViewModel> GetPayPalTransactionRecord(string payOutItemId)
        {
            try
            {
                // Retrieve the PayPal transaction record based on the payOutItemId
                var transactionRecord = await _context.PayPalToValetTransactions
                                       .FirstOrDefaultAsync(t => t.PayOutItemId == payOutItemId);

                if (transactionRecord != null)
                {
                    // Map the retrieved record to the view model and return it
                    PayPalFundToValetViewModel viewModel = new PayPalFundToValetViewModel()
                    {
                        ValetId = transactionRecord.ValetId,
                        ClientId = transactionRecord.CustomerId,
                        OrderId = transactionRecord.OrderId,
                        PayPalAccEmail = transactionRecord.RecipientEmail,
                        OrderCheckOutId = transactionRecord.OrderCheckOutId,
                        BatchId = transactionRecord.BatchId,
                        TransactionStatus = transactionRecord.TransactionStatus,
                        PayOutItemId = transactionRecord.PayOutItemId,
                        PaymentId = transactionRecord.PaymentId,
                    };

                    return viewModel;
                }
                else
                {
                    // No matching record found
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Handle the exception if needed
                return null;
            }
        }
        public async Task<bool> CancelUnclaimedPayment(string payoutItemId, PaymentCancelViewModel cancelObj)
        {
            var amountTransferObj = await _context.PayPalToValetTransactions.FirstOrDefaultAsync(t => t.PayOutItemId == payoutItemId);
            if (amountTransferObj != null)
            {
                amountTransferObj.CancelationReason = cancelObj.CancelationReason;
                amountTransferObj.CancelByAdmin = cancelObj.CancelByAdmin;
                amountTransferObj.ReturnedAmount = cancelObj.ReturnedAmount;
                amountTransferObj.CancelationStatus = cancelObj.CancelationStatus;
                amountTransferObj.TransactionStatus = "RETURNED";
                amountTransferObj.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        
        public async Task<bool> OrderCreatedByPayPalPackage(OrderAcceptedOfPackage packageOrder)
        {
            try
            {
                PayPalOrderCheckOut order = new PayPalOrderCheckOut();
                order.OrderId = packageOrder.OrderId;
                order.ValetId = packageOrder.ValetId;
                order.ClientId = packageOrder.CustomerId;
                order.PayPalAccount = packageOrder.PayPalAccount;
                order.PaidByPackage = packageOrder.PaidByPackage;
                order.OrderPrice = packageOrder.OrderPrice;
                order.PaymentStatus = "USED_SESSION";
                order.PaymentTransmitDateTime = GeneralPurpose.CalculatePayPalTransferFundDate();
                order.IsPaymentSentToValet = false;
                order.IsActive = (int)EnumActiveStatus.Active;
                order.CreatedAt = GeneralPurpose.DateTimeNow();
                await _context.PayPalOrderCheckOut.AddAsync(order);
                await _context.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<bool> PayPalOrderCheckoutAccepted(OrderCheckOutAccepted orderCheckout)
        {
            try
            {
                var checkOutObj = await _context.PayPalOrderCheckOut.FirstOrDefaultAsync(x => x.OrderId == orderCheckout.OrderId &&
                                           x.PaymentId == orderCheckout.PaymentId && x.PaymentStatus == "completed" && x.IsRefund == false);
                if (checkOutObj != null)
                {
                    checkOutObj.IsPaymentSentToValet = false;
                    checkOutObj.PaymentTransmitDateTime = GeneralPurpose.CalculatePayPalTransferFundDate();
                    checkOutObj.OrderPrice = orderCheckout.OrderPrice;
                    checkOutObj.PayPalAccount = orderCheckout.PayPalAccount;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return true;
            }
        }
        public async Task<List<PayPalOrderCheckOut>> FindValetForTransferringFundRecord()
        {
            try
            {
                var currentDate = DateTime.Now.Date; // Get the current date without the time component.

                var valetRecordsFromOrderCheckOut = await _context.PayPalOrderCheckOut
                    .Where(record =>
                        record.IsPaymentSentToValet == false && // Payment not sent to valet yet
                        record.PaymentTransmitDateTime.HasValue && // Check for non-null value
                        record.PaymentTransmitDateTime.Value.Date <= currentDate && // Compare with current date only
                        record.PaymentStatus == "completed" &&
                        record.IsActive == 1)
                    .ToListAsync();

                var valetRecordsFromPackage = await _context.PayPalOrderCheckOut
                    .Where(record =>
                        record.IsPaymentSentToValet == false &&
                        record.PaidByPackage == true &&
                        record.PaymentTransmitDateTime.HasValue && // Check for non-null value
                        record.PaymentTransmitDateTime.Value.Date <= currentDate &&
                        record.PaymentStatus == "USED_SESSION" &&
                        record.IsActive == 1)
                    .ToListAsync();

                // Combine the results from both queries
                var combinedRecords = valetRecordsFromOrderCheckOut.Concat(valetRecordsFromPackage).ToList();

                return combinedRecords;
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately, e.g., log it.
                return null;
            }
        }
        public async Task<PayPalOrderCheckOut?> GetOrderCheckOutById(int id)
        {
            return await _context.PayPalOrderCheckOut.FindAsync(id);
        }
        public async Task<List<PayPalOrderDetailsForAdminDB>> GetPayPalOrdersRecord()
        {
            try
            {
                var paypalOrderRecordObj = await _context.PayPalOrderCheckOut
                .Where(x => (x.CaptureId != null && x.AuthorizationId != null && x.PaymentId != null &&
                             x.IsActive == (int)EnumActiveStatus.Active) ||
                            (x.IsActive == (int)EnumActiveStatus.Active &&
                             x.PaidByPackage == true &&
                             x.PaymentStatus == "USED_SESSION") ||
                            (x.IsActive == (int)EnumActiveStatus.Deleted))
                .ToListAsync();


                var orderIds = paypalOrderRecordObj.Select(obj => obj.OrderId).ToList();

                var orderDetails = await _orderService.GetOrdersByIds(orderIds);

                var userIds = paypalOrderRecordObj.Select(obj => obj.ClientId).Concat(paypalOrderRecordObj.Select(obj => obj.ValetId)).Distinct().ToList();

                var userNames = await _userService.GetUserNames(userIds);

                var result = paypalOrderRecordObj
                    .Select(obj => new PayPalOrderDetailsForAdminDB
                    {
                        Id = obj.OrderId,
                        CustomerName = userNames[obj.ClientId],
                        ITValet = userNames[obj.ValetId],
                        CaptureId = obj.CaptureId,
                        OrderEncId = StringCipher.EncryptId(obj.OrderId),
                        OrderPrice = obj.OrderPrice.ToString(),
                        PaymentStatus = obj.PaymentStatus,
                        PaidByPackage = obj.PaidByPackage,
                        OrderTitle = orderDetails.FirstOrDefault(o => o.Id == obj.OrderId)?.OrderTitle,
                        OrderStatus = orderDetails.FirstOrDefault(o => o.Id == obj.OrderId)?.OrderStatus.ToString()
                    })
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {
                // Handle the exception or log it as needed
                return null;
            }
        }
        public async Task<List<PayPalTransactionDetailsForAdminDB>> GetPayPalTransactionsRecord()
        {
            try
            {
                var payPalTransactionRecord = await _context.PayPalToValetTransactions
                    .Where(x => (x.IsActive == (int)EnumActiveStatus.Active || x.IsActive == (int)EnumActiveStatus.Deleted) &&
                           (x.TransactionStatus == "PENDING" || x.TransactionStatus == "SUCCESS" || x.TransactionStatus == "UNCLAIMED" || x.TransactionStatus == "RETURNED" || x.TransactionStatus== "SESSION_REVERTED"))
                    .ToListAsync();

                var orderIds = payPalTransactionRecord.Select(obj => obj.OrderId).ToList();
                var orderDetails = await _orderService.GetOrdersByIds(orderIds);

                var userIds = payPalTransactionRecord.Select(obj => obj.ValetId)
                    .Concat(payPalTransactionRecord.Select(c => c.CustomerId))
                    .Distinct()
                    .ToList();
                var userNames = await _userService.GetUserNames(userIds);

                var paypalCheckoutIds = payPalTransactionRecord.Select(ck => ck.OrderCheckOutId).ToList();
                var CheckOutDetail = await GetPayPalOrderCheckOutByIds(paypalCheckoutIds);

                var result = payPalTransactionRecord
                    .Select(obj => new PayPalTransactionDetailsForAdminDB
                    {
                        CustomerName = userNames[obj.CustomerId],
                        ITValetName = userNames[obj.ValetId],
                        TransactionStatus = obj.TransactionStatus,
                        PayPalEmailAccount = obj.RecipientEmail,
                        PayOutItemId = obj.PayOutItemId,
                        SentAmount = obj.SentPayment.ToString(),
                        OrderEncId = StringCipher.EncryptId(obj.OrderId),
                        OrderPrice = obj.OrderPrice.ToString(),
                        PlatformFee = obj.PlatformFee.ToString(),
                        OrderTitle = orderDetails.FirstOrDefault(o => o.Id == obj.OrderId)?.OrderTitle,
                        ExpectedDateToTransmitPayment = CheckOutDetail.FirstOrDefault(o => o.Id == obj.OrderCheckOutId)?.PaymentTransmitDateTime.ToString(),
                    })
                    .OrderBy(o => o.ExpectedDateToTransmitPayment) // Sort by ExpectedDateToTransmitPayment in descending order
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                return null;
            }
        }    
        public async Task<List<PayPalUnclaimedTransactionDetailsForAdminDB>> GetPayPalUnclaimedRecord()
        {
            try
            {
                var payPalUnclaimedRecord = await _context.PayPalToValetTransactions
                                    .Where(x => x.IsActive == (int)EnumActiveStatus.Active &&
                                            x.TransactionStatus == "RETURNED" && x.CancelByAdmin == true)
                                    .ToListAsync();
                var orderIds = payPalUnclaimedRecord.Select(obj => obj.OrderId).ToList();
                var orderDetails = await _orderService.GetOrdersByIds(orderIds);

                var userIds = payPalUnclaimedRecord.Select(obj => obj.ValetId)
                   .Concat(payPalUnclaimedRecord.Select(c => c.CustomerId))
                   .Distinct()
                   .ToList();
                var userNames = await _userService.GetUserNames(userIds);
                var result = payPalUnclaimedRecord
                  .Select(obj => new PayPalUnclaimedTransactionDetailsForAdminDB
                  {
                      CustomerName = userNames[obj.CustomerId],
                      ITValetName = userNames[obj.ValetId],
                      OrderEncId = StringCipher.EncryptId(obj.OrderId),
                      PayPalEmailAccount = obj.RecipientEmail,
                      TransactionStatus = obj.TransactionStatus,
                      Reason = obj.CancelationReason,
                      UnclaimedAmountStatus = obj.ReturnedAmount,
                      OrderTitle = orderDetails.FirstOrDefault(o => o.Id == obj.OrderId)?.OrderTitle,
                  })
                  .ToList();

                return result;
            }
            catch(Exception ex)
            {
                return null;
            }
        }
        private async Task<List<PayPalOrderCheckOut>> GetPayPalOrderCheckOutByIds(List<int> orderCheckOutIds)
        {
            try
            {
                // Use EF Core to fetch orders by their IDs efficiently in a single query
                var paypalOrderCheckOutRecord = await _context.PayPalOrderCheckOut
                    .Where(checkout => orderCheckOutIds.Contains(checkout.Id))
                    .ToListAsync();

                return paypalOrderCheckOutRecord;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public async Task<bool> DeleteCheckOutOrderOfPackages(int orderId)
        {
            try
            {
                var orderObj = await _context.PayPalOrderCheckOut.FirstOrDefaultAsync(x => x.OrderId == orderId && x.IsActive == (int)EnumActiveStatus.Active);
                if (orderObj != null)
                {
                    orderObj.IsActive = (int)EnumActiveStatus.Deleted;
                    orderObj.IsRefund = true;
                    orderObj.PaymentStatus = "SESSION_REVERTED";
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<bool> CancelOrderAndRevertSessionAsync(int orderId)
        {
            // Retrieve the order by its ID
            var order = await _orderService.GetOrderById(orderId);

            if (order != null)
            {
                // Retrieve the current user's package
                var currentUserPackage = await _userPackageService.GetCurrentUserPackageByUserId(order.CustomerId);

                if (currentUserPackage != null)
                {
                    // Calculate the duration of the order in hours
                    TimeSpan orderDuration = order.EndDateTime.Value - order.StartDateTime.Value;
                    int numberOfSessionsToRevert = (int)Math.Ceiling(orderDuration.TotalHours);

                    // Revert the user's session count
                    currentUserPackage.RemainingSessions += numberOfSessionsToRevert;

                    // Update the user's package with the reverted session count
                    if (await _userPackageService.UpdateUserPackageSession(currentUserPackage))
                    {
                        // Update the order status to indicate cancellation
                        bool updateOrderStatus = await _orderService.UpdateOrderStatusForCancel(orderId);
                        return updateOrderStatus;
                    }
                }
            }
            return false;
        }
        public async Task<PayPalEarningInCome> GetPayPalEarnings(int valetId)
        {
            try
            {
                var totalEarnedAmount = await CalculateTotalPayPalEarning(valetId);
                var availableIncomeForWithdrawal = await GetAvailableIncomeForWithDrawl(valetId);
                var pendingClearance = await GetPendingAmountOfOrders(valetId);

                //For Stripe 
                var stripe_balancePrices = await _orderService.GetStripeEarnings(valetId);
                decimal? stripeBalance = CalculateEarnings(stripe_balancePrices);

                var stripeCompletedOrderBalance = await _orderService.CalculateStripeCompletedOrder(valetId);
                decimal? completedOrderBalance = CalculateEarnings(stripeCompletedOrderBalance);


                return new PayPalEarningInCome
                {
                    TotalEarnedAmount = totalEarnedAmount.HasValue ? totalEarnedAmount.Value.ToString("0.00") : null,
                    AvailableIncomeForWithDrawl = availableIncomeForWithdrawal.HasValue ? availableIncomeForWithdrawal.Value.ToString("0.00") : null,
                    PendingClearance = pendingClearance.HasValue ? pendingClearance.Value.ToString("0.00") : null,
                    StripeTotalBalance = stripeBalance.HasValue ? stripeBalance.Value.ToString("0.00") : null,
                    StripeCompletedOrderBalance = completedOrderBalance.HasValue ? completedOrderBalance.Value.ToString("0.00"): null,
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<decimal?> CalculateTotalPayPalEarning(int valetId)
        {
            var getOrderPrices = await _context.PayPalOrderCheckOut
                .Where(x => x.ValetId == valetId && (x.PaymentStatus == "completed" || x.PaymentStatus == "USED_SESSION"))
                .Select(x => x.OrderPrice)
                .ToListAsync();

            return CalculateEarnings(getOrderPrices);
        }

        private async Task<decimal?> GetAvailableIncomeForWithDrawl(int valetId)
        {
            var getOrderPrices = await _context.PayPalOrderCheckOut
                .Where(x => x.ValetId == valetId && x.IsPaymentSentToValet == true)
                .Select(x => x.OrderPrice)
                .ToListAsync();

            decimal? availableIncomeForWithdrawal = CalculateEarnings(getOrderPrices);
            return availableIncomeForWithdrawal;
        }

        private async Task<decimal?> GetPendingAmountOfOrders(int valetId)
        {
            List<int> getOrdersId = await _orderService.GetOrdersIdThatHasPendingAmount(valetId);

            var getPendingOrderPrice = await _context.PayPalOrderCheckOut
                .Where(x => x.ValetId == valetId && getOrdersId.Contains(x.OrderId) && x.IsPaymentSentToValet == false)
                .Select(x => x.OrderPrice)
                .ToListAsync();

            decimal? pendingClearanceAmount = CalculateEarnings(getPendingOrderPrice);

            if (pendingClearanceAmount > 0)
            {
                return pendingClearanceAmount;
            }
            else
            {
                return 0;
            }
        }

        private decimal? CalculateEarnings(IEnumerable<decimal?> orderPrices)
        {
            if (orderPrices.Any())
            {
                decimal? totalEarning = 0;
                foreach (var order in orderPrices)
                {
                    var hstFee = GeneralPurpose.CalculateHSTFee(order.Value);
                    var deductHSTFeeFromOrder = order - hstFee;
                    totalEarning += deductHSTFeeFromOrder;
                }
                return totalEarning;
            }
            return 0;
        }

        private async Task<bool> IsPayPalEmailExists(string email)
        {
            return await _context.PayPalAccount
                .AnyAsync(p => p.PayPalEmail == email && p.IsActive == (int)EnumActiveStatus.Active);
        }

        private async Task<PayPalAccountInformation> GetExistingPayPalAccount(int valetId)
        {
            var accountFind = await _context.PayPalAccount
                .FirstOrDefaultAsync(p => p.ValetId == valetId && p.IsActive == (int)EnumActiveStatus.Deleted);
            return accountFind;
        }

        private void UpdatePayPalAccount(PayPalAccountInformation existingAccount, AddPayPalAccountViewModel paypalAccount)
        {
            existingAccount.PayPalEmail = paypalAccount.PayPalEmail;
            existingAccount.IsPayPalAuthorized = paypalAccount.IsPayPalAuthorized;
            existingAccount.IsActive = paypalAccount.IsActive;
            existingAccount.DeletedAt = null;
            existingAccount.CreatedAt = GeneralPurpose.DateTimeNow();
            _context.PayPalAccount.Update(existingAccount);
        }

        private PayPalAccountInformation MapNewPayPalAccount(int valetId, AddPayPalAccountViewModel paypalAccount)
        {
            return new PayPalAccountInformation
            {
                ValetId = valetId,
                PayPalEmail = paypalAccount.PayPalEmail,
                IsPayPalAuthorized = paypalAccount.IsPayPalAuthorized,
                IsActive = paypalAccount.IsActive,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
        }

        private async Task<bool> UpdateUserWithPayPalAccount(int userId, bool isDelete)
        {
            try
            {
                var userObj = await _context.User.FirstOrDefaultAsync(x => x.Id == userId);
                if (userObj != null)
                {
                    userObj.IsPayPalAccount = isDelete ? 0 : 1;
                    userObj.UpdatedAt = GeneralPurpose.DateTimeNow();
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return false;
            }
        }

        private PayPalOrderCheckOut MappingOrderCheckout(OrderCheckOutViewModel model)
        {
            return new PayPalOrderCheckOut
            {
                OrderId = model.OrderId,
                ValetId = model.ValetId,
                ClientId = model.ClientId,
                OrderPrice = model.OrderPrice,
                PaymentId = model.PaymentId,
                CreatedAt = GeneralPurpose.DateTimeNow(),
                IsActive = (int)EnumActiveStatus.Active
            };
        }

        private PayPalOrderCheckOut MappingOrderCheckout(PayPalOrderCheckOutViewModel model)
        {
            return new PayPalOrderCheckOut
            {
                OrderId = model.OrderId,
                ValetId = model.ValetId,
                ClientId = model.ClientId,
                PaymentStatus = "USED_SESSION",
                OrderPrice = model.OrderPrice,
                PaidByPackage = model.PayByPackage,
                CreatedAt = GeneralPurpose.DateTimeNow(),
                IsActive = (int)EnumActiveStatus.Active
            };
        }

        private PayPalToValetTransactions MappingFundPaypal(PayPalFundToValetViewModel model)
        {
            return new PayPalToValetTransactions
            {
                OrderId = model.OrderId,
                BatchId = model.BatchId,
                ValetId = model.ValetId,
                CustomerId = model.ClientId,
                PaymentId = model.PaymentId,
                OrderPrice = model.OrderPrice,
                PlatformFee = model.PlatformFee,
                SentPayment = model.SentPayment,
                PayOutItemId = model.PayOutItemId,
                RecipientEmail = model.PayPalAccEmail,
                OrderCheckOutId = model.OrderCheckOutId,
                IsActive = (int)EnumActiveStatus.Active,
                TransactionStatus = model.TransactionStatus,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
        }

        private PayPalPackagesCheckOut MappingPaypal(PackageCheckOutViewModel model)
        {
            return new PayPalPackagesCheckOut
            {
                EndDate = model.EndDate,
                ClientId = model.ClientId,
                StartDate = model.StartDate,
                PaymentId = model.PaymentId,
                PackageType = model.PackageType,
                PackagePrice = model.PackagePrice,
                UserPackageId = model.UserPackageId,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
        }

        private PayPalToValetTransactions MappingUnClaimedPayments(PaymentCancelViewModel model)
        {
            return new PayPalToValetTransactions
            {
                CancelationReason = model.CancelationReason,
                CancelByAdmin = model.CancelByAdmin,
                ReturnedAmount = model.ReturnedAmount,
                CancelationStatus = model.CancelationStatus,
                TransactionStatus = "RETURNED",
                UpdatedAt = GeneralPurpose.DateTimeNow(),
                IsActive = (int)EnumActiveStatus.Active,
            };
        }

        private CaptureAmountViewModel MapToCaptureAmountViewModel(PayPalOrderCheckOut capturedObj)
        {
            return new CaptureAmountViewModel
            {
                PayableAmount = capturedObj.PayableAmount,
                Currency = capturedObj.Currency
            };
        }

        private async Task<bool> AddPayPalOrderCore(PayPalOrderCheckOut order)
        {
            try
            {
                _context.PayPalOrderCheckOut.Add(order);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return false;
            }
        }

        private int CalculateRevertableSessions(DateTime? startDateTime, DateTime? endDateTime)
        {
            if (startDateTime == null || endDateTime == null)
                throw new ArgumentException("Order start or end time cannot be null.");

            TimeSpan orderDuration = endDateTime.Value - startDateTime.Value;
            return (int)Math.Ceiling(orderDuration.TotalHours);
        }

        private async Task<bool> UpdateUserPackageAndCancelOrderAsync(UserPackage currentUserPackage, int orderId)
        {
            if (await _userPackageService.UpdateUserPackageSession(currentUserPackage))
            {
                return await _orderService.UpdateOrderStatusForCancel(orderId);
            }
            return false;
        }

        private async Task<bool> UpdateOrderStatus(int orderId, int isActiveStatus, bool isRefund,
            string paymentStatus)
        {
            try
            {
                var orderObj = await _context.PayPalOrderCheckOut
                    .FirstOrDefaultAsync(x => x.OrderId == orderId && x.IsActive == (int)EnumActiveStatus.Active);

                if (orderObj != null)
                {
                    orderObj.IsActive = isActiveStatus;
                    orderObj.IsRefund = isRefund;
                    orderObj.PaymentStatus = paymentStatus;
                    await _context.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
            }
            return false;
        }

        private int DecryptionId(string userId)
        {
            var validEncrypted = GeneralPurpose.ConversionEncryptedId(userId);
            return StringCipher.DecryptId(validEncrypted);
        }

        private void DeactivatePayPalAccount(PayPalAccountInformation accountObj)
        {
            accountObj.IsActive = (int)EnumActiveStatus.Deleted;
            accountObj.DeletedAt = GeneralPurpose.DateTimeNow();
        }
        
        private async void CreateLogger(Exception ex)
        {
            await MailSender.SendErrorMessage($"URL: {_projectVariables.BaseUrl}<br/> Exception Message:  {ex.Message} <br/> Stack Trace: {ex.StackTrace}");
        }
    }

    public interface IPayPalGatewayWriteService : IPayPalGatewayReadService
    {
        Task<bool> AddPayPalPackageAsync(PackageCheckOutViewModel package);
        Task<bool> UpdatePackageRecord(PackageCheckOutViewModel package);
        Task<bool> AddPayPalOrderForPackage(PayPalOrderCheckOutViewModel order);
        Task<bool> DeleteCheckOutOrderOfPackages(int orderId);
        Task<bool> OrderCreatedByPayPalPackage(OrderAcceptedOfPackage packageOrder);
        
        Task<bool> AddPayPalOrder(OrderCheckOutViewModel order);
        Task<bool> UpdateOrderCheckOut(PayPalOrderCheckOut checkout);
        
        Task<bool> CancelOrderAndRevertSessionAsync(int orderId);
        Task<bool> UpdateOrderRecord(OrderCheckOutViewModel order);
        Task<bool> PaymentRefunding(string captureId);
        
        Task<ResponseDto> AddPayPalAccountInformation(string valetId, AddPayPalAccountViewModel paypalAccount);
        Task<bool> AddPayPalTransactionAdminToValet(PayPalFundToValetViewModel transferToValet);
        
        Task<bool> CancelUnclaimedPayment(string payoutItemId, PaymentCancelViewModel cancelObj);
        
        Task<bool> DeletePayPalAccount(int id);
        Task<bool> PayPalOrderCheckoutAccepted(OrderCheckOutAccepted orderCheckout);
    }

    public interface IPayPalGatewayReadService
    {
        Task<PayPalOrderCheckOut?> GetOrderCheckOutById(int id);
        Task<PackageCheckOutViewModel> GetPackageByPaymentId(string paymentId);
        Task<OrderCheckOutViewModel> GetOrderByPaymentId(string paymentId);
        Task<CaptureAmountViewModel> CapturedAmount(string capturedId);
        Task<string> GetPayPalAccount(int valetId);
        Task<PayPalFundToValetViewModel> GetPayPalTransactionRecord(string payOutItemId);
        Task<List<PayPalUnclaimedTransactionDetailsForAdminDB>> GetPayPalUnclaimedRecord();
        Task<List<PayPalTransactionDetailsForAdminDB>> GetPayPalTransactionsRecord();
        Task<List<PayPalOrderCheckOut>> FindValetForTransferringFundRecord();
        Task<List<PayPalOrderDetailsForAdminDB>> GetPayPalOrdersRecord();
        Task<PayPalEarningInCome> GetPayPalEarnings(int valetId);
    }

    public class IPaypalServices : IPayPalGatewayWriteService
    {
        private readonly AppDbContext _context;
        private readonly IUserRepo _userService;
        private readonly IOrderRepo _orderService;
        private readonly INotificationService _userPackageService;
        private readonly ProjectVariables _projectVariables;
        public IPaypalServices(AppDbContext context, IUserRepo userService,
            IOrderRepo orderService, INotificationService userPackageService,
            IOptions<ProjectVariables> options)
        {
            _context = context;
            _userService = userService;
            _orderService = orderService;
            _userPackageService = userPackageService;
            _projectVariables = options.Value;
        }


        public async Task<ResponseDto> AddPayPalAccountInformation(string valetId, AddPayPalAccountViewModel paypalAccount)
        {
            try
            {
                var decrypt = DecryptionId(valetId);

                if (await IsPayPalEmailExists(paypalAccount.PayPalEmail!))
                    return GeneralPurpose.GenerateResponseCode(false, "204", "Email already exists.");

                var existingPayPalAccount = await GetExistingPayPalAccount(decrypt);

                if (existingPayPalAccount != null)
                {
                    UpdatePayPalAccount(existingPayPalAccount, paypalAccount);
                }
                else
                {
                    var newPayPalAccount = MapNewPayPalAccount(decrypt, paypalAccount);
                    _context.PayPalAccount.Add(newPayPalAccount);
                }
                

                await _context.SaveChangesAsync();
                bool updateUser = await UpdateUserWithPayPalAccount(decrypt, false);
                return GeneralPurpose.GenerateResponseCode(true, "200", "Account information added/updated successfully.");
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return GeneralPurpose.GenerateResponseCode(false, "400", "An error occurred while adding/updating account information.");
            }
        }

        public async Task<bool> AddPayPalOrder(OrderCheckOutViewModel order)
        {
            return await AddPayPalOrderCore(MappingOrderCheckout(order));
        }

        public async Task<bool> AddPayPalOrderForPackage(PayPalOrderCheckOutViewModel order)
        {
            return await AddPayPalOrderCore(MappingOrderCheckout(order));
        }

        public async Task<bool> AddPayPalPackageAsync(PackageCheckOutViewModel package)
        {
            try
            {
                var obj = MappingPaypal(package);
                _context.PayPalPackagesCheckOut.Add(obj);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return false;
            }
        }

        public async Task<bool> AddPayPalTransactionAdminToValet(PayPalFundToValetViewModel transferToValet)
        {
            try
            {
                var obj = MappingFundPaypal(transferToValet);
                _context.PayPalToValetTransactions.Add(obj);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return false;
            }
        }

        public async Task<bool> CancelOrderAndRevertSessionAsync(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderById(orderId);
                if (order == null)
                    return false;

                var currentUserPackage = await _userPackageService.GetCurrentUserPackageByUserId(order.CustomerId);
                if (currentUserPackage == null)
                    return false;

                int sessionsToRevert = CalculateRevertableSessions(order.StartDateTime, order.EndDateTime);
                currentUserPackage.RemainingSessions += sessionsToRevert;

                if (await UpdateUserPackageAndCancelOrderAsync(currentUserPackage, orderId))
                    return true;
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
            }
            return false;
        }

        public async Task<bool> CancelUnclaimedPayment(string payoutItemId, PaymentCancelViewModel cancelObj)
        {
            try
            {
                var amountTransferObj = await _context.PayPalToValetTransactions.FirstOrDefaultAsync(t => t.PayOutItemId == payoutItemId);
                if (amountTransferObj != null)
                {
                    amountTransferObj = MappingUnClaimedPayments(cancelObj);
                    await _context.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
            }

            return false;
        }

        public async Task<CaptureAmountViewModel> CapturedAmount(string capturedId)
        {
            try
            {
                var capturedObj = await _context.PayPalOrderCheckOut
                    .FirstOrDefaultAsync(c => c.CaptureId == capturedId);

                return capturedObj != null ? 
                    MapToCaptureAmountViewModel(capturedObj) : 
                    throw new Exception("Record Not Found");
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return null;
            }
        }

        public async Task<bool> DeleteCheckOutOrderOfPackages(int orderId)
        {
            return await UpdateOrderStatus(orderId, (int)EnumActiveStatus.Deleted, true, "SESSION_REVERTED");
        }

        public async Task<bool> DeletePayPalAccount(int id)
        {
            try
            {
                var accountObj = _context.PayPalAccount.
                    FirstOrDefault(t => t.ValetId == id && t.IsActive == 1);
                if (accountObj != null)
                {
                    DeactivatePayPalAccount(accountObj);
                    await _context.SaveChangesAsync();
                    await UpdateUserWithPayPalAccount(accountObj.ValetId ?? 0, true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
            }
            return false;
        }

        public async Task<List<PayPalOrderCheckOut>> FindValetForTransferringFundRecord()
        {
            try
            {
                var currentDate = DateTime.Now.Date;

                var valetRecords = await _context.PayPalOrderCheckOut
                    .Where(record =>
                        record.IsPaymentSentToValet == false && // Payment not sent to valet yet
                        record.PaymentTransmitDateTime.HasValue && // Check for non-null value
                        record.PaymentTransmitDateTime.Value.Date <= currentDate && // Compare with current date only
                        record.IsActive == 1 && // Record is active
                        (
                            (record.PaymentStatus == "completed" && record.PaidByPackage == false) || // Payment completed without package
                            (record.PaymentStatus == "USED_SESSION" && record.PaidByPackage == true) // Payment from package
                        )
                    )
                    .ToListAsync();

                return valetRecords;
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return null;
            }
        }

        public Task<OrderCheckOutViewModel> GetOrderByPaymentId(string paymentId)
        {
            throw new NotImplementedException();
        }

        public Task<PayPalOrderCheckOut?> GetOrderCheckOutById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<PackageCheckOutViewModel> GetPackageByPaymentId(string paymentId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPayPalAccount(int valetId)
        {
            throw new NotImplementedException();
        }

        public Task<PayPalEarningInCome> GetPayPalEarnings(int valetId)
        {
            throw new NotImplementedException();
        }

        public Task<List<PayPalOrderDetailsForAdminDB>> GetPayPalOrdersRecord()
        {
            throw new NotImplementedException();
        }

        public Task<PayPalFundToValetViewModel> GetPayPalTransactionRecord(string payOutItemId)
        {
            throw new NotImplementedException();
        }

        public Task<List<PayPalTransactionDetailsForAdminDB>> GetPayPalTransactionsRecord()
        {
            throw new NotImplementedException();
        }

        public Task<List<PayPalUnclaimedTransactionDetailsForAdminDB>> GetPayPalUnclaimedRecord()
        {
            throw new NotImplementedException();
        }

        public Task<bool> OrderCreatedByPayPalPackage(OrderAcceptedOfPackage packageOrder)
        {
            throw new NotImplementedException();
        }

        public Task<bool> PaymentRefunding(string captureId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> PayPalOrderCheckoutAccepted(OrderCheckOutAccepted orderCheckout)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateOrderCheckOut(PayPalOrderCheckOut checkout)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateOrderRecord(OrderCheckOutViewModel order)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdatePackageRecord(PackageCheckOutViewModel package)
        {
            throw new NotImplementedException();
        }

        #region Helpers

        private int DecryptionId(string userId)
        {
            var validEncrypted = GeneralPurpose.ConversionEncryptedId(userId);
            return StringCipher.DecryptId(validEncrypted);
        }

        private async Task<bool> IsPayPalEmailExists(string email)
        {
            return await _context.PayPalAccount
                .AnyAsync(p => p.PayPalEmail == email && p.IsActive == (int)EnumActiveStatus.Active);
        }

        private async Task<PayPalAccountInformation> GetExistingPayPalAccount(int valetId)
        {
            var accountFind = await _context.PayPalAccount
                .FirstOrDefaultAsync(p => p.ValetId == valetId && p.IsActive == (int)EnumActiveStatus.Deleted);
            return accountFind;
        }

        private void UpdatePayPalAccount(PayPalAccountInformation existingAccount, AddPayPalAccountViewModel paypalAccount)
        {
            existingAccount.PayPalEmail = paypalAccount.PayPalEmail;
            existingAccount.IsPayPalAuthorized = paypalAccount.IsPayPalAuthorized;
            existingAccount.IsActive = paypalAccount.IsActive;
            existingAccount.DeletedAt = null;
            existingAccount.CreatedAt = GeneralPurpose.DateTimeNow();
            _context.PayPalAccount.Update(existingAccount);
        }

        private PayPalAccountInformation MapNewPayPalAccount(int valetId, AddPayPalAccountViewModel paypalAccount)
        {
            return new PayPalAccountInformation
            {
                ValetId = valetId,
                PayPalEmail = paypalAccount.PayPalEmail,
                IsPayPalAuthorized = paypalAccount.IsPayPalAuthorized,
                IsActive = paypalAccount.IsActive,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
        }

        private async Task<bool> UpdateUserWithPayPalAccount(int userId, bool isDelete)
        {
            try
            {
                var userObj = await _context.User.FirstOrDefaultAsync(x => x.Id == userId);
                if (userObj != null)
                {
                    userObj.IsPayPalAccount = isDelete ? 0 : 1;
                    userObj.UpdatedAt = GeneralPurpose.DateTimeNow();
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return false;
            }
        }

        private PayPalOrderCheckOut MappingOrderCheckout(OrderCheckOutViewModel model)
        {
            return new PayPalOrderCheckOut
            {
                OrderId = model.OrderId,
                ValetId = model.ValetId,
                ClientId = model.ClientId,
                OrderPrice = model.OrderPrice,
                PaymentId = model.PaymentId,
                CreatedAt = GeneralPurpose.DateTimeNow(),
                IsActive = (int)EnumActiveStatus.Active
            };
        }

        private PayPalOrderCheckOut MappingOrderCheckout(PayPalOrderCheckOutViewModel model)
        {
            return new PayPalOrderCheckOut
            {
                OrderId = model.OrderId,
                ValetId = model.ValetId,
                ClientId = model.ClientId,
                PaymentStatus = "USED_SESSION",
                OrderPrice = model.OrderPrice,
                PaidByPackage = model.PayByPackage,
                CreatedAt = GeneralPurpose.DateTimeNow(),
                IsActive = (int)EnumActiveStatus.Active
            };
        }

        private PayPalToValetTransactions MappingFundPaypal(PayPalFundToValetViewModel model)
        {
            return new PayPalToValetTransactions
            {
                OrderId = model.OrderId,
                BatchId = model.BatchId,
                ValetId = model.ValetId,
                CustomerId = model.ClientId,
                PaymentId = model.PaymentId,
                OrderPrice = model.OrderPrice,
                PlatformFee = model.PlatformFee,
                SentPayment = model.SentPayment,
                PayOutItemId = model.PayOutItemId,
                RecipientEmail = model.PayPalAccEmail,
                OrderCheckOutId = model.OrderCheckOutId,
                IsActive = (int)EnumActiveStatus.Active,
                TransactionStatus = model.TransactionStatus,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
        }

        private PayPalPackagesCheckOut MappingPaypal(PackageCheckOutViewModel model)
        {
            return new PayPalPackagesCheckOut
            {
                EndDate = model.EndDate,
                ClientId = model.ClientId,
                StartDate = model.StartDate,
                PaymentId = model.PaymentId,
                PackageType = model.PackageType,
                PackagePrice = model.PackagePrice,
                UserPackageId = model.UserPackageId,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
        }

        private PayPalToValetTransactions MappingUnClaimedPayments(PaymentCancelViewModel model)
        {
            return new PayPalToValetTransactions {
                CancelationReason = model.CancelationReason,
                CancelByAdmin = model.CancelByAdmin,
                ReturnedAmount = model.ReturnedAmount,
                CancelationStatus = model.CancelationStatus,
                TransactionStatus = "RETURNED",
                UpdatedAt = GeneralPurpose.DateTimeNow(),
                IsActive = (int)EnumActiveStatus.Active,
            };
        }

        private CaptureAmountViewModel MapToCaptureAmountViewModel(PayPalOrderCheckOut capturedObj)
        {
            return new CaptureAmountViewModel
            {
                PayableAmount = capturedObj.PayableAmount,
                Currency = capturedObj.Currency
            };
        }

        private async Task<bool> AddPayPalOrderCore(PayPalOrderCheckOut order)
        {
            try
            {
                _context.PayPalOrderCheckOut.Add(order);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return false;
            }
        }

        private int CalculateRevertableSessions(DateTime? startDateTime, DateTime? endDateTime)
        {
            if (startDateTime == null || endDateTime == null)
                throw new ArgumentException("Order start or end time cannot be null.");

            TimeSpan orderDuration = endDateTime.Value - startDateTime.Value;
            return (int)Math.Ceiling(orderDuration.TotalHours);
        }

        private async Task<bool> UpdateUserPackageAndCancelOrderAsync(UserPackage currentUserPackage, int orderId)
        {
            if (await _userPackageService.UpdateUserPackageSession(currentUserPackage))
            {
                return await _orderService.UpdateOrderStatusForCancel(orderId);
            }
            return false;
        }

        private async Task<bool> UpdateOrderStatus(int orderId, int isActiveStatus, bool isRefund,
            string paymentStatus)
        {
            try
            {
                var orderObj = await _context.PayPalOrderCheckOut
                    .FirstOrDefaultAsync(x => x.OrderId == orderId && x.IsActive == (int)EnumActiveStatus.Active);

                if (orderObj != null)
                {
                    orderObj.IsActive = isActiveStatus;
                    orderObj.IsRefund = isRefund;
                    orderObj.PaymentStatus = paymentStatus;
                    await _context.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
            }
            return false;
        }

        private void DeactivatePayPalAccount(PayPalAccountInformation accountObj)
        {
            accountObj.IsActive = (int)EnumActiveStatus.Deleted;
            accountObj.DeletedAt = GeneralPurpose.DateTimeNow();
        }

        private async void CreateLogger(Exception ex)
        {
            await MailSender.SendErrorMessage($"URL: {_projectVariables.BaseUrl}<br/> Exception Message:  {ex.Message} <br/> Stack Trace: {ex.StackTrace}");
        }
        #endregion
    }
}
