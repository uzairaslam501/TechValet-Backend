using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface ISearchLogRepo
    {
        Task<SearchLog?> GetSearchLogById(int id);
        Task<IEnumerable<SearchLog>> GetSearchLogByUserId(int userId);
        Task<IEnumerable<SearchLog>> GetSearchLogList();
        Task<SearchLog> GetSearchLogListBySearchingWord(string keyWord);
        Task<bool> AddSearchLog(SearchLog SearchLog);
        Task<bool> UpdateSearchLog(SearchLog SearchLog);
        Task<bool> DeleteSearchLog(int id);
    }

    public class SearchLogRepo : ISearchLogRepo
    {
        private readonly AppDbContext _context;
        public SearchLogRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }
        public async Task<bool> AddSearchLog(SearchLog SearchLog)
        {
            try
            {
                _context.SearchLog.Add(SearchLog);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteSearchLog(int id)
        {
            try
            {
                SearchLog? AvailableSlot = await GetSearchLogById(id);

                if (AvailableSlot != null)
                {
                    AvailableSlot.IsActive = 0;
                    AvailableSlot.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateSearchLog(AvailableSlot);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<SearchLog?> GetSearchLogById(int id)
        {
            return await _context.SearchLog.FindAsync(id);
        }

        public async Task<IEnumerable<SearchLog>> GetSearchLogByUserId(int userId)
        {
            var getListOfAvailableSlots = await _context.SearchLog.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.ValetProfileId == userId).ToListAsync();
            return getListOfAvailableSlots;
        }

        public async Task<IEnumerable<SearchLog>> GetSearchLogList()
        {
            return await _context.SearchLog.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<SearchLog> GetSearchLogListBySearchingWord(string keyWord)
        {
            return await _context.SearchLog.Where(x => x.IsActive == (int)EnumActiveStatus.Active &&
            x.SearchKeyword.ToLower().Equals(keyWord.ToLower())).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateSearchLog(SearchLog SearchLog)
        {
            try
            {
                _context.Entry(SearchLog).State = EntityState.Modified;
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
