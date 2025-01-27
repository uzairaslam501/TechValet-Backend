using ITValet.HelpingClasses;
using ITValet.Services;
using Microsoft.Extensions.Options;
using Quartz;

namespace ITValet.Scheduler
{
    public class FundTransferJob : IJob
    {
        private readonly ILogger<FundTransferJob> _logger;
        private readonly IPayPalGateWayService _payPalGateWayService;
        private readonly IOrderRepo _orderService;
        private readonly IFundTransferService _fundTransferService;
        private readonly IUserRepo _userService;
        private readonly ProjectVariables _projectVariables;
        private readonly ReturnUrls _returnUrls;
        public FundTransferJob(ILogger<FundTransferJob> logger, IPayPalGateWayService payPalGateWayService, IFundTransferService fundTransferService,
            IOrderRepo orderService, IUserRepo userService, IOptions<ProjectVariables> projectVariables, IOptions<ReturnUrls> returnUrls)
        {
            _logger = logger;
            _payPalGateWayService = payPalGateWayService;
            _fundTransferService = fundTransferService;
            _orderService = orderService;
            _userService = userService;
            _projectVariables = projectVariables.Value;
            _returnUrls = returnUrls.Value;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var findValetForTransferFund = await _payPalGateWayService.FindValetForTransferringFundRecord();
                if (findValetForTransferFund != null && findValetForTransferFund.Any())
                {
                    foreach (var valetObj in findValetForTransferFund)
                    {
                        try
                        {
                            var valetFundRecord = new ValetFundRecord
                            {
                                OrderId = valetObj.OrderId,
                                PaymentId = valetObj.PaymentId,
                                ClientId = valetObj.ClientId,
                                PayPalAccEmail = valetObj.PayPalAccount,
                                ValetId = valetObj.ValetId,
                                OrderPrice = valetObj.OrderPrice ?? 0m,
                                OrderCheckOutId = valetObj.Id,
                            };

                            var isPaymentRefunded = await _fundTransferService.TransferFundToValetAccount(valetFundRecord);
                            if (isPaymentRefunded)
                            {
                                //Update Order Record
                                var orderObj = await _orderService.GetOrderById(valetObj.OrderId);
                                orderObj!.OrderStatus = 2;
                                bool updateOrder = await _orderService.UpdateOrder(orderObj);

                                //Update OrderCheckout Record
                                var checkoutObj = await _payPalGateWayService.GetOrderCheckOutById(valetObj.Id);
                                checkoutObj!.IsPaymentSentToValet = true;
                                bool updateCheckOutOrder = await _payPalGateWayService.UpdateOrderCheckOut(checkoutObj);
                            }
                            else
                            {
                                var orderObj = await _orderService.GetOrderById(valetObj.OrderId);
                                orderObj!.OrderStatus = 3;
                                bool updateOrderState = await _orderService.UpdateOrder(orderObj);
                                // Sent email
                                var userObj = await _userService.GetUserById(orderObj.ValetId ?? 0);
                                var url = $"{_projectVariables.ReactUrl}{_returnUrls.OrderDetailUrl}/{StringCipher.EncryptId(orderObj.Id)}";
                                bool isEmailSent = await MailSender.SendEmailForPaymentMaintenance(userObj?.Email!, userObj?.UserName!, url);
                            }
                        }
                        catch (Exception ex)
                        {
                            await MailSender.SendErrorMessage(ex.Message);
                            _logger.LogError(ex, "An error occurred while transferring funds for valet {ValetId}", valetObj.ValetId);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No valet records found for fund transfer.");
                }
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                _logger.LogError(ex, "An error occurred during the job execution");
            }
        }
    }
}
