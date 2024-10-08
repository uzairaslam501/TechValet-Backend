using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ITValet.Services
{
    public interface INotificationService
    {
        Task<int> AddUserPackageAndGetId(UserPackage package);
        Task<bool> UpdateUserPackage(int id, string paidBy);
        Task<int?> GetRemainingSessionCount(int customerId);
        Task<UserPackage?> GetUserPackageById(int id);
        Task<IEnumerable<UserPackage>> GetUserPackageList();
        Task<IEnumerable<UserPackage>> GetUserPackageListByUserId(int? Id);
        Task<UserPackage?> GetUserPackageByUserId(int? id);
        Task<UserPackage?> GetCurrentUserPackageByUserId(int? id);
        Task<bool> UpdateUserPackageSession(UserPackage package);
        Task<List<UserPackageListDto>> GetUserPackageLists();
    }

    public class UserPackageService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IUserRepo _userService;
        public UserPackageService(AppDbContext _appDbContext, IUserRepo userService)
        {
            _context = _appDbContext;
            _userService = userService;
        }

        public async Task<int> AddUserPackageAndGetId(UserPackage package)
        {
            try
            {
                package.CreatedAt = DateTime.UtcNow;
                _context.UserPackage.Add(package);
                await _context.SaveChangesAsync();
                return package.Id; // Assuming UserPackage has an Id property
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public async Task<bool> UpdateUserPackage(int id, string paidBy)
        {
            try
            {
                var packageObj = await _context.UserPackage.FirstOrDefaultAsync(x => x.Id == id);
                if (packageObj != null)
                {
                    packageObj.PaidBy = paidBy;
                    packageObj.IsActive = 1;
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
        public async Task<int?> GetRemainingSessionCount(int customerId)
        {
            try
            {
                var latestPackageWithSessions = await _context.UserPackage
                    .Where(x => x.IsActive == 1 && x.CustomerId == customerId && x.RemainingSessions > 0)
                    .FirstOrDefaultAsync();

                if (latestPackageWithSessions != null)
                {
                    return latestPackageWithSessions.RemainingSessions;
                }

                return 0;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }
        
        public async Task<IEnumerable<UserPackage>> GetUserPackageList()
        {
            try
            {
                return await _context.UserPackage.Where(x => x.IsActive == (int)EnumActiveStatus.Active)
                    .Include(x=>x.User).ToListAsync();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<UserPackageListDto>> GetUserPackageLists()
        {
            try
            {
                var userPackages = await _context.UserPackage
                    .Where(x => x.IsActive == (int)EnumActiveStatus.Active)
                    .Include(x => x.User)
                    .ToListAsync();

                var userPackageListDto = new List<UserPackageListDto>();

                foreach (var userPackage in userPackages)
                {
                    var obj = new UserPackageListDto
                    {
                        PackageName = userPackage.PackageName,
                        PackageType = userPackage.PackageType,
                        RemainingSessions = userPackage.RemainingSessions,
                        StartDateTime = userPackage.StartDateTime,
                        EndDateTime = userPackage.EndDateTime
                    };

                    var customer = await _userService.GetUserById((int)userPackage.CustomerId);
                    obj.Customer = customer != null ? $"{customer.FirstName} {customer.LastName}" : "N/A";

                    userPackageListDto.Add(obj);
                }

                return userPackageListDto;
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                return null;
            }
        }


        public async Task<IEnumerable<UserPackage>> GetUserPackageListByUserId(int? UserId)
        {
            try
            {
                return await _context.UserPackage.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.CustomerId== UserId).ToListAsync();

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<UserPackage?> GetUserPackageById(int id)
        {
            return await _context.UserPackage.FindAsync(id);
        }

        public async Task<UserPackage?> GetUserPackageByUserId(int? id)
        {
            return await _context.UserPackage.FirstOrDefaultAsync(x => x.CustomerId == id && x.IsActive == 1 && x.RemainingSessions != 0);
        }
        public async Task<UserPackage?> GetCurrentUserPackageByUserId(int? id)
        {
            try
            {
                var getCustomerRecentPackage = await _context.UserPackage.OrderByDescending(x => x.Id).FirstOrDefaultAsync(x => x.CustomerId == id && x.IsActive == 1);
                return getCustomerRecentPackage;
            }
            catch (Exception ex)
            {
                return null;
            }

        }
        public async Task<bool> UpdateUserPackageSession(UserPackage package)
        {
            try
            {
                _context.Entry(package).State = EntityState.Modified;
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
