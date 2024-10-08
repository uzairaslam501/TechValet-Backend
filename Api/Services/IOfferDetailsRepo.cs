using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface IOfferDetailsRepo
    {
        Task<OfferDetail?> GetOfferDetailById(int id);
        Task<IEnumerable<OfferDetail>> GetOfferDetailByUserId(int userId);
        Task<IEnumerable<OfferDetail>> GetOfferDetailList();
        Task<OfferDetail> GetOfferDetailByMessageId(int messageId);
        Task<bool> AddOfferDetail(OfferDetail OfferDetail);
        Task<bool> UpdateOfferDetail(OfferDetail OfferDetail);
        Task<bool> DeleteOfferDetail(int id);
        Task<bool> DeleteOfferDetailWithoutSaveChanges(int id);
        Task<bool> SaveChanges();
        Task<bool> UpdateOfferStatus(int orderId, int? Id);
    }

    public class OfferDetailsRepo : IOfferDetailsRepo
    {
        private readonly AppDbContext _context;
        public OfferDetailsRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }

        public async Task<bool> AddOfferDetail(OfferDetail OfferDetail)
        {
            try
            {
                _context.OfferDetail.Add(OfferDetail);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateOfferStatus(int orderId, int? Id)
        {
            try
            {
                var offerObj = await _context.OfferDetail.FirstOrDefaultAsync(x => x.Id == Id);
                offerObj.OfferStatus = 2;
                offerObj.OrderId = orderId;
                offerObj.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        public async Task<bool> DeleteOfferDetail(int id)
        {
            try
            {
                OfferDetail? AvailableSlot = await GetOfferDetailById(id);

                if (AvailableSlot != null)
                {
                    AvailableSlot.IsActive = 0;
                    AvailableSlot.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateOfferDetail(AvailableSlot);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteOfferDetailWithoutSaveChanges(int id)
        {
            try
            {
                OfferDetail? AvailableSlot = await GetOfferDetailById(id);

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

        public async Task<OfferDetail?> GetOfferDetailById(int id)
        {
            return await _context.OfferDetail.FindAsync(id);
        }

        public async Task<IEnumerable<OfferDetail>> GetOfferDetailByUserId(int userId)
        {
            var getListOfAvailableSlots = await _context.OfferDetail.Where(x => x.IsActive == (int)EnumActiveStatus.Active &&
            (x.CustomerId == userId || x.ValetId == userId)).ToListAsync();
            return getListOfAvailableSlots;
        }

        public async Task<IEnumerable<OfferDetail>> GetOfferDetailList()
        {
            return await _context.OfferDetail.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<OfferDetail> GetOfferDetailByMessageId(int messageId)
        {
            return await _context.OfferDetail.Where(x => x.IsActive == (int)EnumActiveStatus.Active &&
            x.MessageId == messageId).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateOfferDetail(OfferDetail OfferDetail)
        {
            try
            {
                _context.Entry(OfferDetail).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
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
