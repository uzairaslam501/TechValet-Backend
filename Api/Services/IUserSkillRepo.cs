using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface IUserSkillRepo
    {
        Task<UserSkill?> GetUserSkillByIdAsync(int id);
        Task<IEnumerable<UserSkill>> GetUserSkillsByUserIdAsync(int userId);
        Task<IEnumerable<UserSkill>> GetAllActiveUserSkillsAsync();
        Task<IEnumerable<UserSkill>> GetUsersBySkillNameAsync(string skillName);
        Task<bool> AddUserSkillAsync(UserSkill userSkill);
        Task<bool> UpdateUserSkillAsync(UserSkill userSkill);
        Task<bool> SoftDeleteUserSkillAsync(int id);
        Task<int?> GetUserSkillCountByIdAsync(int id);
        Task<bool> SaveChangesAsync();
    }

    public class UserSkillRepo : IUserSkillRepo
    {
        private readonly AppDbContext _context;

        public UserSkillRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserSkill?> GetUserSkillByIdAsync(int id)
        {
            return await _context.UserSkill.FindAsync(id);
        }

        public async Task<IEnumerable<UserSkill>> GetUserSkillsByUserIdAsync(int userId)
        {
            return await _context.UserSkill
                .Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserSkill>> GetAllActiveUserSkillsAsync()
        {
            return await _context.UserSkill
                .Where(x => x.IsActive == (int)EnumActiveStatus.Active)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserSkill>> GetUsersBySkillNameAsync(string skillName)
        {
            return await _context.UserSkill
                .Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.SkillName == skillName)
                .ToListAsync();
        }

        public async Task<bool> AddUserSkillAsync(UserSkill userSkill)
        {
            try
            {
                await _context.UserSkill.AddAsync(userSkill);
                return await SaveChangesAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateUserSkillAsync(UserSkill userSkill)
        {
            try
            {
                _context.Entry(userSkill).State = EntityState.Modified;
                return await SaveChangesAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SoftDeleteUserSkillAsync(int id)
        {
            try
            {
                var userSkill = await GetUserSkillByIdAsync(id);
                if (userSkill == null) return false;

                userSkill.IsActive = 0;
                userSkill.DeletedAt = GeneralPurpose.DateTimeNow();
                return await UpdateUserSkillAsync(userSkill);
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message);
                return false;
            }
        }

        public async Task<int?> GetUserSkillCountByIdAsync(int id)
        {
            return await _context.UserSkill
                .Where(x => x.Id == id)
                .CountAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
