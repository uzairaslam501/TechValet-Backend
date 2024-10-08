using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface IUserAvailableSlotRepo
    {
        Task<UserAvailableSlot?> GetUserAvailableSlotById(int id);
        Task<IEnumerable<UserAvailableSlot>> GetUserAvailableSlotByUserId(int userId);
        Task<UserAvailableSlot?> GetUserAvailableSlotByUserIdAndDateOrDay(int userId ,string day);
        Task<IEnumerable<UserAvailableSlot>> GetUserAvailableSlotList();
        Task<bool> AddUserAvailableSlot(UserAvailableSlot userAvailableSlot);
        Task<bool> UpdateUserAvailableSlot(UserAvailableSlot userAvailableSlot);
        Task<bool> DeleteUserAvailableSlot(int id);
        Task<bool> DeactivateExpiredSlots();
        Task<bool> DeleteUserAvailableSlotWithoutSavingDatabase(int id);
        Task<bool> UpdateUserAvailableSlotWithoutSavingInDatabase(UserAvailableSlot userAvailableSlot);
        Task<bool> CreateEntriesForCurrentMonth(int? userId);
    }

    public class UserAvailableSlotRepo : IUserAvailableSlotRepo
    {
        private readonly AppDbContext _context;
        
        public UserAvailableSlotRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }
        
        public async Task<bool> AddUserAvailableSlot(UserAvailableSlot userAvailableSlot)
        {
            try
            {
                _context.UserAvailableSlot.Add(userAvailableSlot);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserAvailableSlot(int id)
        {
            try
            {
                UserAvailableSlot? AvailableSlot = await GetUserAvailableSlotById(id);

                if (AvailableSlot != null)
                {
                    AvailableSlot.IsActive = 0;
                    AvailableSlot.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateUserAvailableSlot(AvailableSlot);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<UserAvailableSlot?> GetUserAvailableSlotById(int id)
        {
            return await _context.UserAvailableSlot.FindAsync(id);
        }

        public async Task<IEnumerable<UserAvailableSlot>> GetUserAvailableSlotByUserId(int userId)
        {
            try { 
            var getListOfAvailableSlots = await _context.UserAvailableSlot.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.UserId == userId).ToListAsync();
            return getListOfAvailableSlots;
            }
            catch(Exception ex)
            {
                var x = ex.Message.ToString();
                return null;
            }
        }

        public async Task<UserAvailableSlot?> GetUserAvailableSlotByUserIdAndDateOrDay(int userId, string day)
        {
            try
            {
                var getCompleteList = await _context.UserAvailableSlot.
                    Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.UserId == userId
                    ).ToListAsync();
                var getSpecificSortsByDate = getCompleteList.FirstOrDefault(x => Convert.ToDateTime(x.DateTimeOfDay).Date == Convert.ToDateTime(day).Date);
                return getSpecificSortsByDate;
            }
            catch (Exception ex)
            {
                var x = ex.Message.ToString();
                return null;
            }
        }

        public async Task<IEnumerable<UserAvailableSlot>> GetUserAvailableSlotList()
        {
            return await _context.UserAvailableSlot.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<bool> UpdateUserAvailableSlot(UserAvailableSlot userAvailableSlot)
        {
            try
            {
                _context.Entry(userAvailableSlot).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserAvailableSlotWithoutSavingDatabase(int id)
        {
            try
            {
                UserAvailableSlot? AvailableSlot = await GetUserAvailableSlotById(id);

                if (AvailableSlot != null)
                {
                    AvailableSlot.IsActive = 0;
                    AvailableSlot.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateUserAvailableSlotWithoutSavingInDatabase(AvailableSlot);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateUserAvailableSlotWithoutSavingInDatabase(UserAvailableSlot userAvailableSlot)
        {
            try
            {
                _context.Entry(userAvailableSlot).State = EntityState.Modified;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeactivateExpiredSlots()
        {
            try
            {
                var currentDate = DateTime.Now;
                var slotsToDeactivate = await _context.UserAvailableSlot
                    .Where(x => x.IsActive == (int)EnumActiveStatus.Active &&
                                x.DateTimeOfDay.Value.Date < currentDate.Date)
                    .ToListAsync();

                if (slotsToDeactivate.Count > 0)
                {
                    foreach (var slot in slotsToDeactivate)
                    {
                        slot.IsActive = 2;
                    }

                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> CreateEntriesForCurrentMonth(int? userId)
        {
            try
            {

                // Determine the start date of the last month
                var currentDate = DateTime.Now;
                var startDateOfLastMonth = new DateTime(currentDate.Year, currentDate.Month, 1).AddMonths(-1);

                // Get all records of the last month
                var recordsOfLastMonth = _context.UserAvailableSlot
                    .Where(x => x.UserId == userId)
                    .ToList();
                
                // Determine the start date of the current month
                var startDateOfCurrentMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
                var daysInCurrentMonth = DateTime.DaysInMonth(startDateOfCurrentMonth.Year, startDateOfCurrentMonth.Month);

                if (recordsOfLastMonth.Any())
                {
                    recordsOfLastMonth = recordsOfLastMonth.Where(x => Convert.ToDateTime(x.DateTimeOfDay).Year == startDateOfLastMonth.Year &&
                    Convert.ToDateTime(x.DateTimeOfDay).Month == startDateOfLastMonth.Month).ToList();
                    var daysInLastMonth = DateTime.DaysInMonth(startDateOfLastMonth.Year, startDateOfLastMonth.Month);
                    // Create entries for the whole current month
                    for (int day = 1; day <= daysInCurrentMonth; day++)
                    {
                        var getUserId = userId;
                        var newRecord = new UserAvailableSlot();
                        if (day <= daysInLastMonth)
                        {
                            var lastRecordIndex = day - 1; // Index in recordsOfLastMonth

                            newRecord = new UserAvailableSlot
                            {
                                DateTimeOfDay = startDateOfCurrentMonth.AddDays(day - 1),
                                Slot1 = recordsOfLastMonth[lastRecordIndex].Slot1,
                                Slot2 = recordsOfLastMonth[lastRecordIndex].Slot2,
                                Slot3 = recordsOfLastMonth[lastRecordIndex].Slot3,
                                Slot4 = recordsOfLastMonth[lastRecordIndex].Slot4,
                                IsActive = 1,
                                UserId = recordsOfLastMonth[lastRecordIndex].UserId,
                                CreatedAt = DateTime.Now,
                            };
                        }
                        else
                        {
                            // Handle the case where the current month has more days than the last month
                            newRecord = new UserAvailableSlot
                            {
                                DateTimeOfDay = startDateOfCurrentMonth.AddDays(day - 1),
                                Slot1 = 1,
                                Slot2 = 2,
                                Slot3 = 3,
                                Slot4 = 4,
                                IsActive = 1,
                                UserId = getUserId,
                                CreatedAt = DateTime.Now,
                            };
                        }
                        _context.UserAvailableSlot.Add(newRecord);
                    }

                }
                else
                {
                    for (int day = 1; day <= daysInCurrentMonth; day++)
                    {
                        var getUserId = userId;
                        var newRecord = new UserAvailableSlot();
                        newRecord = new UserAvailableSlot
                        {
                            DateTimeOfDay = startDateOfCurrentMonth.AddDays(day - 1),
                            Slot1 = 1,
                            Slot2 = 2,
                            Slot3 = 3,
                            Slot4 = 4,
                            IsActive = 1,
                            UserId = getUserId,
                            CreatedAt = DateTime.Now,
                        };
                        
                        _context.UserAvailableSlot.Add(newRecord);
                    }
                }
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
