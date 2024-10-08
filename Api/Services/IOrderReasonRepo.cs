using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface IOrderReasonRepo
    {
        Task<OrderReason?> GetOrderReasonById(int id);
        Task<OrderReason?> GetOrderReasonByOrderId(int orderId);
        Task<IEnumerable<OrderReason>> GetOrderReasonListByOrderId(int orderId);
        Task<IEnumerable<OrderReason>> GetOrderReasonList();
        Task<OrderReason> AddOrderReason(OrderReason OrderReason);
        Task<OrderReason> UpdateOrderReasons(OrderReason OrderReason);
        Task<bool> UpdateOrderReason(OrderReason OrderReason);
        Task<bool> DeleteOrderReason(int id);
        Task<bool> DeleteOrderReasonWithoutSaveChanges(int id);
        Task<OrderReason?> GetOrderReasonByOrderReasonId(int id);
        Task<bool> SaveChanges();
       
    }

    public class OrderReasonRepo : IOrderReasonRepo
    {
        private readonly AppDbContext _context;
        
        public OrderReasonRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }

        public async Task<OrderReason> AddOrderReason(OrderReason OrderReason)
        {
            try
            {
                _context.OrderReason.Add(OrderReason);
                await _context.SaveChangesAsync();
                return OrderReason;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<OrderReason?> GetOrderReasonByOrderReasonId(int id)
        {
            return await _context.OrderReason.Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteOrderReason(int id)
        {
            try
            {
                OrderReason? AvailableSlot = await GetOrderReasonById(id);

                if (AvailableSlot != null)
                {
                    AvailableSlot.IsActive = 0;
                    AvailableSlot.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateOrderReason(AvailableSlot);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteOrderReasonWithoutSaveChanges(int id)
        {
            try
            {
                OrderReason? AvailableSlot = await GetOrderReasonById(id);

                if (AvailableSlot != null)
                {
                    AvailableSlot.IsActive = 0;
                    AvailableSlot.DeletedAt = GeneralPurpose.DateTimeNow();
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<OrderReason?> GetOrderReasonById(int id)
        {
            return await _context.OrderReason.FindAsync(id);
        }

        public async Task<OrderReason?> GetOrderReasonByOrderId(int orderId)
        {
            var getList = await _context.OrderReason.Where(x => 
            x.IsActive != 0 &&
            x.OrderId == orderId).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            return getList;
        }

        public async Task<IEnumerable<OrderReason>> GetOrderReasonListByOrderId(int orderId)
        {
            var getList = await _context.OrderReason.Where(x => 
            x.IsActive == (int)EnumActiveStatus.Active &&
            x.OrderId == orderId).ToListAsync();
            return getList;
        }

      
        public async Task<IEnumerable<OrderReason>> GetOrderReasonList()
        {
            return await _context.OrderReason.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<bool> UpdateOrderReason(OrderReason OrderReason)
        {
            try
            {
                _context.Entry(OrderReason).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<OrderReason> UpdateOrderReasons(OrderReason OrderReason)
        {
            try
            {
                _context.Entry(OrderReason).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return OrderReason;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> SaveChanges()
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
    }
}
