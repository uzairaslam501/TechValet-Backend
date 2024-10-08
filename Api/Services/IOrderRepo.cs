using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ITValet.Services
{
    public interface IOrderRepo
    {
        Task<Order?> GetOrderById(int id);
        Task<List<Order>> GetOrdersByIds(List<int> orderIds);
        Task<Order?> GetOrderByIdUsingIncludes(int id);
        Task<IEnumerable<Order>> GetOrderByUserId(int userId);
        Task<IEnumerable<Order>> GetOrderByPackageId(int? userId);
        Task<IEnumerable<Order>> GetOrderList();
        Task<bool> AddOrder(Order Order);
        Task<int> GetOrderId(Order Order);
        Task<bool> AddOrder2(Order Order);
        Task<bool> UpdateOrder(Order Order);
        Task<bool> UpdateOrderStatusForCancel(int id);
        Task<bool> DeleteOrder(int id);
        Task<bool> DeleteOrder2(int id);
        Task<List<OrderEventsViewModal>> GetOrderEventRecordForValet(int id);
        Task<bool> saveChangesFunction();
        Task<List<StripeOrderDetailForAdminDb>> GetStripeOrdersRecord();
        Task<IEnumerable<Order>> getInProgressUserOrders(int userId);
        Task<int> AddOrderByPayPal(PayPalOrderCheckOutViewModel orderObj);
        Task<List<BookedSlotTiming>> GetBookedSlotsTime(int valetId);
        Task<bool> ChangeStripePaymentStatus(int orderId, StripePaymentStatus status);
        Task<Order?> GetOrderByPaymentId(string paymentId);
        Task<bool> CheckAvailability(int valetId, DateTime startDate, DateTime endDate);
        Task<Order?> UpdateOrderByPayPal(string paymentId, string CapturedID);
        Task<Order?> UpdateOrderStatusForPayPal(AcceptOrder orderDetail);
        Task<List<int>> GetOrdersIdThatHasPendingAmount(int valetId);
        Task<List<CompletedOrderRecord>> GetCompletedOrderRecord(int valetId);
        Task<List<decimal?>> CalculateStripeCompletedOrder(int valetId);
        Task<List<decimal?>> GetStripeEarnings(int valetId);
        Task<List<OrderEventsViewModal>> GetOrderEventRecord(int id, int? Role);
        Task<List<OrderEventsViewModal>> GetOrderEventRecordByOrderStatus(int id, int? role, bool InProgress, bool cancelled, bool completed);
    }

    public class OrderRepo : IOrderRepo
    {
        private readonly AppDbContext _context;
        private readonly ProjectVariables projectVariables;
        private readonly IUserRepo _userService;

        public OrderRepo(AppDbContext _appDbContext, IOptions<ProjectVariables> options, IUserRepo userService)
        {
            _context = _appDbContext;
            projectVariables = options.Value;
            _userService = userService;
        }

        public async Task<bool> AddOrder(Order Order)
        {
            try
            {
                _context.Order.Add(Order);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<IEnumerable<Order>> getInProgressUserOrders(int userId)
        {
            try
            {
                var getOrdersList = await _context.Order
                .Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.IsDelivered != 2 && x.OrderStatus != 4 &&
                (x.CustomerId == userId || x.ValetId == userId)).ToListAsync();

                return getOrdersList;
            }
            catch (Exception ex)
            {
                var x = ex.Message.ToString();
                return null;
            }
        }

        public async Task<int> GetOrderId(Order order)
        {
            try
            {
                _context.Order.Add(order);
                await _context.SaveChangesAsync();
                return order.Id;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public async Task<bool> AddOrder2(Order Order)
        {
            try
            {
                await _context.Order.AddAsync(Order);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteOrder(int id)
        {
            try
            {
                Order? Skill = await GetOrderById(id);

                if (Skill != null)
                {
                    Skill.IsActive = 0;
                    Skill.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateOrder(Skill);
                }
                return false;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }

        public async Task<bool> UpdateOrderStatusForCancel(int id)
        {
            try
            {
                var orderObj = await _context.Order
                            .Where(x => x.Id == id)
                             .SingleOrDefaultAsync();

                if (orderObj != null)
                {
                    orderObj.OrderStatus = 4;
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

        public async Task<List<Order>> GetOrdersByIds(List<int> orderIds)
        {
            try
            {
                // Use EF Core to fetch orders by their IDs efficiently in a single query
                var orders = await _context.Order
                    .Where(order => orderIds.Contains(order.Id))
                    .ToListAsync();

                return orders;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        
        public async Task<bool> DeleteOrder2(int id)
        {
            try
            {
                Order? Order = await GetOrderById(id);
                Order.IsActive = 0;
                Order.DeletedAt = GeneralPurpose.DateTimeNow();
                _context.Entry(Order).State = EntityState.Modified;
                return true;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }

        public async Task<Order?> GetOrderById(int id)
        {
            return await _context.Order.FindAsync(id);
        }

        public async Task<Order?> GetOrderByPaymentId(string paymentId)
        {
            return await _context.Order.FirstOrDefaultAsync(x => x.PayPalPaymentId == paymentId);
        }

        public async Task<int> AddOrderByPayPal(PayPalOrderCheckOutViewModel orderObj)
        {
            try
            {
                Order orderinfo = new Order();
                orderinfo.OrderPrice = orderObj.OrderPrice;
                orderinfo.TotalAmountIncludedFee = orderObj.TotalPrice;
                orderinfo.PayPalPaymentId = orderObj.PaymentId;
                orderinfo.IsActive = 0;
                orderinfo.IsDelivered = 0;
                orderinfo.OrderTitle = orderObj.OrderTitle;
                orderinfo.OrderDescription = orderObj.OrderDescription;
                orderinfo.OrderStatus = orderObj.OrderStatus;
                orderinfo.CustomerId = orderObj.ClientId;
                orderinfo.ValetId = orderObj.ValetId;
                orderinfo.OfferId = orderObj.OfferId;
                orderinfo.CreatedAt = DateTime.Now;
                orderinfo.StartDateTime = orderObj.StartDate;
                orderinfo.EndDateTime = orderObj.EndDate;

                _context.Order.Add(orderinfo);
                await _context.SaveChangesAsync();

                return orderinfo.Id; // Return the newly generated OrderId
            }
            catch (Exception ex)
            {
                return -1; // Return a negative value or throw an exception here to indicate failure
            }
        }

        public async Task<Order?> UpdateOrderByPayPal(string paymentId, string CapturedID)
        {
            try
            {
                var orderobj = await _context.Order.FirstOrDefaultAsync(x => x.PayPalPaymentId == paymentId);
                if (orderobj != null)
                {
                    orderobj.IsActive = 1;
                    orderobj.UpdatedAt = DateTime.Now;
                    orderobj.CapturedId = CapturedID;
                    await _context.SaveChangesAsync();
                    return orderobj;
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<BookedSlotTiming>> GetBookedSlotsTime(int valetId)
        {
            try
            {
                var bookedSlots = await _context.Order
                    .Where(x => x.ValetId == valetId && x.OrderStatus == 0 && x.IsActive == 1)
                    .Select(x => new BookedSlotTiming
                    {
                        StartDate = x.StartDateTime,
                        EndDate = x.EndDateTime,
                    })
                    .ToListAsync();
                if (bookedSlots.Count > 0)
                {
                    return bookedSlots;
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> CheckAvailability(int valetId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var overlappingBookedSlots = await _context.Order
                    .Where(x =>
                        x.ValetId == valetId &&
                        x.OrderStatus == 0 &&
                        x.IsActive == 1 &&
                        (startDate < x.EndDateTime && endDate > x.StartDateTime)
                    )
                    .ToListAsync();

                return overlappingBookedSlots.Count == 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        public async Task<Order?> UpdateOrderStatusForPayPal(AcceptOrder orderDetail)
        {
            try
            {
                int orderId = StringCipher.DecryptId(orderDetail.OrderId);
                var orderObj = await _context.Order.FirstOrDefaultAsync(x => x.Id == orderId && x.IsActive == 1);
                if (orderObj != null)
                {
                    orderObj.OrderStatus = 1;
                    orderObj.IsDelivered = 2;
                    orderObj.UpdatedAt = GeneralPurpose.DateTimeNow();
                    await _context.SaveChangesAsync();
                    return orderObj;
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        
        public async Task<Order?> GetOrderByIdUsingIncludes(int id)
        {
            return await _context.Order.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.Id == id).Include(x => x.OrderReason).FirstOrDefaultAsync();
        }

        public async Task<bool> ChangeStripePaymentStatus(int orderId, StripePaymentStatus status)
        {
            try
            {
                var orderObj = await _context.Order.FirstOrDefaultAsync(x => x.Id == orderId);
                if (orderObj != null)
                {
                    orderObj.StripeStatus = (int)status;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            } catch (Exception ex)
            {
                return false;
            }
        }
        
        public async Task<IEnumerable<Order>> GetOrderByUserId(int userId)
        {
            try
            {
                var getListOfSkills = await _context.Order.Where(x => x.IsActive == (int)EnumActiveStatus.Active &&
                (x.CustomerId == userId || x.ValetId == userId))
                    .Include(x => x.OrderReason).OrderByDescending(x => x.Id)
                    .ToListAsync();
                return getListOfSkills;
            }
            catch (Exception ex)
            {
                var x = ex.Message.ToString();
                return null;
            }
        }

        public async Task<IEnumerable<Order>> GetOrderByPackageId(int? PackageId)
        {
            try
            {
                var getListOfOrder = await _context.Order.Where(x => x.IsActive == (int)EnumActiveStatus.Active &&
                (x.PackageId == PackageId))
                    .Include(x => x.OrderReason).OrderByDescending(x => x.Id)
                    .ToListAsync();
                return getListOfOrder;
            }
            catch (Exception ex)
            {
                var x = ex.Message.ToString();
                return null;
            }
        }

        public async Task<IEnumerable<Order>> GetOrderList()
        {
            return await _context.Order.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<bool> UpdateOrder(Order Order)
        {
            try
            {
                _context.Entry(Order).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        public async Task<bool> saveChangesFunction()
        {
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        public async Task<List<OrderEventsViewModal>> GetOrderEventRecord(int id, int? Role)
        {
            try
            {
                List<OrderEventsViewModal> orderEvents = new List<OrderEventsViewModal>();
                List<Order> orders = null;

                if (Role == 3)
                {
                    orders = await _context.Order.Where(x => x.CustomerId == id &&  x.IsActive == (int)EnumActiveStatus.Active &&
                         (x.OrderStatus == 0 || x.OrderStatus == 1 ||
                        x.OrderStatus == 2)).ToListAsync();
                }
                else if (Role == 4)
                {
                    orders = await _context.Order.Where(x => x.ValetId == id && x.IsActive == (int)EnumActiveStatus.Active &&
                        (x.OrderStatus == 0 || x.OrderStatus == 1 ||
                        x.OrderStatus == 2)).ToListAsync();
                }
                else if (Role == 5)
                {
                    orders = await _context.Order.Where(x => x.ValetId == id &&  x.IsActive == (int)EnumActiveStatus.Active &&
                        (x.OrderStatus == 0 || x.OrderStatus == 1 || x.OrderStatus == 4)).ToListAsync();
                }

                if (orders != null && orders.Any())
                {
                    foreach (var order in orders)
                    {
                        OrderEventsViewModal obj = new OrderEventsViewModal();
                        obj.OrderEncId = StringCipher.EncryptId(order.Id);
                        obj.OrderTitle = order.OrderTitle;
                        obj.OrderDescription = order.OrderDescription;
                        obj.OrderStatus = order.OrderStatus;
                        obj.StartDateTime = order.StartDateTime;
                        obj.OrderStatusDescription = await GetOrderStatus(obj.OrderStatus);
                        obj.EndDateTime = order.EndDateTime;
                        obj.OrderDetailUrl = projectVariables.BaseUrl + "User/OrderDetail?orderId=" + obj.OrderEncId;
                        orderEvents.Add(obj);
                    }
                }

                return orderEvents;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<string?> GetOrderStatus(int? orderStatus)
        {
            if (orderStatus != null)
            {
                if (orderStatus == 0)
                {
                    return "IN-PROGRESS";
                }
                else if (orderStatus == 1 || orderStatus == 2)
                {
                    return "COMPLETED";
                }
                else if (orderStatus == 4)
                {
                    return "CANCELED";
                }
                return "NOT-FOUND";
            }
            return null;
        }

        public async Task<List<OrderEventsViewModal>> GetOrderEventRecordForValet(int id)
        {
            try
            {
                List<OrderEventsViewModal> orderEvents = new List<OrderEventsViewModal>();
                DateTime currentDate = DateTime.Now.Date; // Extract date portion from current date

                List<Order> orders = await _context.Order
                    .Where(x => x.IsActive == (int)EnumActiveStatus.Active &&
                                x.ValetId == id &&
                                x.OrderStatus == 0 &&
                                x.StartDateTime.Value.Date >= currentDate) // Extract date portion from StartDateTime
                    .ToListAsync();

                if (orders != null && orders.Any())
                {
                    foreach (var order in orders)
                    {
                        OrderEventsViewModal obj = new OrderEventsViewModal();
                        obj.OrderEncId = StringCipher.EncryptId(order.Id);
                        obj.OrderTitle = order.OrderTitle;
                        obj.OrderDescription = order.OrderDescription;
                        obj.OrderStatus = order.OrderStatus;
                        obj.StartDateTime = order.StartDateTime;
                        obj.EndDateTime = order.EndDateTime;
                        obj.OrderDetailUrl = projectVariables.BaseUrl + "User/OrderDetail?orderId=" + obj.OrderEncId;
                        orderEvents.Add(obj);
                    }
                }

                return orderEvents;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<OrderEventsViewModal>> GetOrderEventRecordByOrderStatus(int id, int? role, bool inProgress, bool cancelled, bool completed)
        {
            try
            {
                List<OrderEventsViewModal> orderEvents = new List<OrderEventsViewModal>();

                var query = _context.Order
                    .Where(x =>
                        x.IsActive == (int)EnumActiveStatus.Active &&
                        ((role == 4 && x.ValetId == id) || (role == 3 && x.CustomerId == id)) &&
                        ((inProgress && x.OrderStatus == 0) || (cancelled && x.OrderStatus == 4) || (completed && x.OrderStatus == 1))
                    );

                var orders = await query.ToListAsync();

                foreach (var order in orders)
                {
                    OrderEventsViewModal obj = new OrderEventsViewModal
                    {
                        OrderEncId = StringCipher.EncryptId(order.Id),
                        OrderTitle = order.OrderTitle,
                        OrderDescription = order.OrderDescription,
                        OrderStatus = order.OrderStatus,
                        StartDateTime = order.StartDateTime,
                        EndDateTime = order.EndDateTime,
                        OrderDetailUrl = projectVariables.BaseUrl + "User/OrderDetail?orderId=" + StringCipher.EncryptId(order.Id)
                    };

                    orderEvents.Add(obj);
                }

                return orderEvents;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<StripeOrderDetailForAdminDb>> GetStripeOrdersRecord()
        {
            try
            {
                var stripeOrdersRecord = await _context.Order.
                    Where(x => (x.StripeStatus == (int)StripePaymentStatus.PaymentReceived && x.OrderStatus == 0 || x.StripeStatus == (int)StripePaymentStatus.Refunded && x.OrderStatus == 4 ||
                         x.StripeStatus == (int)StripePaymentStatus.SessionUsed && x.OrderStatus == 0 || x.StripeStatus == (int)StripePaymentStatus.SessionReverted && x.OrderStatus == 4 || x.StripeStatus == (int)StripePaymentStatus.SentToValet && x.OrderStatus == 1)
                         && (x.StripeChargeId != null || x.PackageBuyFrom == "STRIPE") && x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();

                var userIds = stripeOrdersRecord.Select(obj => obj.ValetId).
                              Concat(stripeOrdersRecord.Select(obj => obj.CustomerId)).
                              Where(id => id.HasValue).Select(id => id.Value).
                              Distinct().ToList();
                var userNames = await _userService.GetUserNames(userIds);
                var result = stripeOrdersRecord
                .Select(obj => new StripeOrderDetailForAdminDb
                {
                    Id = obj.Id,
                    CustomerName = obj.CustomerId.HasValue ? userNames.GetValueOrDefault(obj.CustomerId.Value, "Unknown") : "Unknown",
                    ITValet = obj.ValetId.HasValue ? userNames.GetValueOrDefault(obj.ValetId.Value, "Unknown") : "Unknown",
                    OrderEncId = StringCipher.EncryptId(obj.Id),
                    StripeId = obj.StripeChargeId,
                    StripeStatus = obj.StripeStatus.ToString(),
                    OrderPrice = obj.OrderPrice.ToString(),
                    OrderStatus = obj.OrderStatus.ToString(),
                    PaidByPackage = obj.PackageBuyFrom,
                    OrderTitle = obj.OrderTitle,
                    IsDelivered = obj.IsDelivered.ToString(),
                    PaymentStatus = CalculatePaymentStatus(obj.StripeChargeId, obj.PackageBuyFrom, obj.OrderStatus, obj.StripeStatus)
                })
                .ToList();

                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private string CalculatePaymentStatus(string? stripeChargeId, string? PackageBuyFrom, int? OrderStatus, int? stripeStatus)
        {
            if (!string.IsNullOrEmpty(stripeChargeId) && (OrderStatus == 0 || OrderStatus == 1) && (stripeStatus == (int)StripePaymentStatus.PaymentReceived || stripeStatus == (int)StripePaymentStatus.SentToValet))
            {
                return "COMPLETED";
            }
            else if (OrderStatus == 4 && (stripeStatus == (int)StripePaymentStatus.Refunded || stripeStatus == (int)StripePaymentStatus.SessionReverted))
            {
                return "REFUNDED";
            }
            else if (PackageBuyFrom == "STRIPE" && (OrderStatus == 0 || OrderStatus == 1) && (stripeStatus == (int)StripePaymentStatus.SessionUsed || stripeStatus == (int)StripePaymentStatus.SentToValet))
            {
                return "PAID-BY-PACKAGE";
            }
            else
            {
                return "NULL";
            }
        }
        
        public async Task<List<CompletedOrderRecord>> GetCompletedOrderRecord(int valetId)
        {
            try
            {
                List<CompletedOrderRecord> records = new List<CompletedOrderRecord>();
                var completedOrder = await _context.Order.Where(x => x.ValetId == valetId
                                   && (x.OrderStatus == 1 || x.OrderStatus == 2)).ToListAsync();
                if (completedOrder.Any())
                {
                    foreach (var item in completedOrder)
                    {
                        CompletedOrderRecord record = new CompletedOrderRecord();
                        record.EncOrderId = StringCipher.EncryptId(item.Id);
                        record.OrderTitle = item.OrderTitle;
                        record.OrderPrice = item.OrderPrice.ToString();
                        record.EarnedFromOrder = await EarnedAmountFromOrder(item.OrderPrice.Value);
                        record.OrderPaidBy = await OrderPaidBy(item.PayPalPaymentId, item.CapturedId, item.StripeChargeId, item.PackageBuyFrom);
                        record.CompletedAt = item.EndDateTime.ToString();

                        records.Add(record);
                    }
                }
                return records;
            }
            catch (Exception ex)
            {
                return new List<CompletedOrderRecord>();
            }
        }

        private async Task<string> OrderPaidBy(string? PaymentId, string? CaptureId, string? StripeChargeId, string? PackageBuyFrom)
        {
            string orderPaidBy = string.Empty; 

            if (!string.IsNullOrEmpty(PackageBuyFrom))
            {
                orderPaidBy = PackageBuyFrom;
            }
            else if (!string.IsNullOrEmpty(PaymentId) && !string.IsNullOrEmpty(CaptureId))
            {
                orderPaidBy = "PAYPAL";
            }
            else if (!string.IsNullOrEmpty(StripeChargeId))
            {
                orderPaidBy = "STRIPE";
            }

            return orderPaidBy;
        }

        private async Task<string> EarnedAmountFromOrder(decimal OrderPrice)
        {
            var orderHstFee = GeneralPurpose.CalculateHSTFee(OrderPrice);
            decimal earnedAmount = OrderPrice - orderHstFee;
            return earnedAmount.ToString("0.00");
        }

        public async Task<List<int>> GetOrdersIdThatHasPendingAmount(int valetId)
        {
            var orders = await _context.Order
                .Where(x => x.ValetId == valetId
                    && x.IsActive == 1
                    && x.OrderStatus == 1
                    && (!string.IsNullOrEmpty(x.CapturedId) || x.PackageBuyFrom == "PAYPAL"))
                .Select(x => x.Id)
                .ToListAsync();

            if (orders.Any())
            {
                return orders;
            }
            else
            {
                // If there are no orders that meet the criteria, return an empty list.
                return new List<int>();
            }
        }

        public async Task<List<decimal?>> CalculateStripeCompletedOrder(int valetId)
        {
            try
            {
                var stripeCompletedOrders = await _context.Order
                    .Where(x => x.ValetId == valetId &&
                        (!string.IsNullOrEmpty(x.StripeChargeId) || x.PackageBuyFrom == "STRIPE") &&
                        x.OrderStatus == 1 && x.StripeStatus == 5)
                    .Select(x => x.OrderPrice)
                    .ToListAsync();

                if (stripeCompletedOrders.Any())
                {
                    return stripeCompletedOrders;
                }
                else
                {
                    return new List<decimal?>();
                }
            }
            catch (Exception ex)
            {
                return new List<decimal?>();
            }
        }
        
        public async Task<List<decimal?>> GetStripeEarnings(int valetId)
        {
            try
            {
                var getOrderPrices = await _context.Order
                    .Where(x => x.ValetId == valetId && (x.PackageBuyFrom == "STRIPE" || (!string.IsNullOrEmpty(x.CapturedId) && x.OrderStatus != 4)))
                    .Select(x => x.OrderPrice)
                    .ToListAsync();

                return getOrderPrices;
            }
            catch (Exception ex)
            {
                return new List<decimal?>();
            }
        }
    }
}
