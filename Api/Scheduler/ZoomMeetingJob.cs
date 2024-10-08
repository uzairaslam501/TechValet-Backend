using ITValet.HelpingClasses;
using ITValet.Models;
using ITValet.Services;
using Quartz;
using Stripe;

namespace ITValet.Scheduler
{
    public class ZoomMeetingJob :IJob
    {

        private readonly ILogger<ZoomMeetingJob> _logger;
        private readonly IUserRepo _userService;
        private readonly INotificationRepo _notificationService;
        private readonly IMessagesRepo messagesRepo;
        private readonly IOrderRepo _orderRepo;
        private readonly IOrderReasonRepo _orderReasonRepo;
        private readonly IFundTransferService _fundTransferService;
        private readonly IPayPalGateWayService _payPalGateWayService;
        private readonly INotificationService userPackageService;
        public ZoomMeetingJob(ILogger<ZoomMeetingJob> logger, IOrderReasonRepo orderReasonRepo,
            IOrderRepo orderRepo, IMessagesRepo _messagesRepo,
            IUserRepo userService, INotificationRepo notificationService,
            IFundTransferService _fundTransferService, IPayPalGateWayService _payPalGateWayService,
            INotificationService userPackageService
            )
        {
            _logger = logger;
            _userService = userService;
            _notificationService = notificationService;
            messagesRepo = _messagesRepo;
            _orderRepo = orderRepo;
            _orderReasonRepo = orderReasonRepo;
            this._fundTransferService = _fundTransferService;   
            this._payPalGateWayService = _payPalGateWayService;
            this.userPackageService = userPackageService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                List<Message> ZoomMessages = await messagesRepo.GetZoomMessageList();
                foreach (Message zoomMessage in ZoomMessages)
                {
                   Order getOrder = await _orderRepo.GetOrderById((int)zoomMessage.OrderId);
                    if(getOrder.OrderStatus != 2 && getOrder.OrderStatus != 4) 
                    {
                        var OrderReason = new OrderReason();
                        OrderReason.OrderId = getOrder.Id;
                        OrderReason.ReasonType = 3;
                        OrderReason.ReasonExplanation = "Zoom Meeting not accepted";
                        OrderReason.IsActive = 1;
                        getOrder.OrderStatus = 4;
                        if (getOrder.PackageId == null)
                        {
                            if (getOrder.CapturedId != null)
                            {
                                bool isRefundStatus = await _fundTransferService.RefundPayment(getOrder.CapturedId, getOrder.Id);
                            }
                            else
                            {
                                var RefundOrderPayment = await RefundPayment(getOrder.StripeChargeId, getOrder.Id);
                                if (RefundOrderPayment)
                                {
                                    bool updateCancelOrderStatus = await _orderRepo.UpdateOrderStatusForCancel(getOrder.Id);
                                }
                            }
                        }
                        else
                        {
                            if (getOrder.PackageBuyFrom == "PAYPAL")
                            {
                                bool cancelOrderInCheckOut = await _payPalGateWayService.DeleteCheckOutOrderOfPackages(getOrder.Id);
                            }
                            await postUpdatePackage(getOrder.Id, getOrder.PackageId.Value, getOrder.CustomerId, getOrder.StartDateTime, getOrder.EndDateTime);
                        }
                        var orderReasons = await _orderReasonRepo.AddOrderReason(OrderReason);
                        var chkOrderUpdated = await _orderRepo.UpdateOrder(getOrder);
                    }
                }
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message.ToString());
                // Consider adding local or centralized logging here
            }
        }
        private async Task<bool> RefundPayment(string chargeId, int OrderId)
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
                return true;
            }
            return false;
        }
        private async Task<bool> postUpdatePackage(int orderId, int? packageId, int? CustomerId, DateTime? startDate, DateTime? endDate)
        {
            // Getting User Current Package


            var getCurrentUserPackage = await userPackageService.GetCurrentUserPackageByUserId(CustomerId);

            if (getCurrentUserPackage != null)
            {

                TimeSpan timeDifference = endDate.Value - startDate.Value;

                // Calculate the ceiling number of hours
                int NoOfSessionRevert = (int)Math.Ceiling(timeDifference.TotalHours);
                var revertUserSession = getCurrentUserPackage.RemainingSessions.Value + NoOfSessionRevert;
                getCurrentUserPackage.RemainingSessions = revertUserSession;

                if (await userPackageService.UpdateUserPackageSession(getCurrentUserPackage))
                {
                    bool updateOrderStatusForCancel = await _orderRepo.UpdateOrderStatusForCancel(orderId);
                    bool updateStripeStatus = await _orderRepo.ChangeStripePaymentStatus(orderId, StripePaymentStatus.SessionReverted);
                    return updateOrderStatusForCancel;
                }

            }

            return false;
        }


    }
}
