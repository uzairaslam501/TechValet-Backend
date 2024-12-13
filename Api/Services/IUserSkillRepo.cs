using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ITValet.Services
{
    public interface IUserSkillRepo
    {
        Task<UserSkill?> GetUserSkillByIdAsync(string userId);
        Task<IEnumerable<UserSkill>> GetUserSkillsByUserIdAsync(int userId);
        Task<IEnumerable<UserSkill>> GetAllActiveUserSkillsAsync();
        Task<IEnumerable<UserSkill>> GetUsersBySkillNameAsync(string skillName);
        Task<bool> AddUserSkillAsync(string userId, string skill);
        Task<bool> UpdateUserSkillAsync(UserSkill userSkill);
        Task<bool> SoftDeleteUserSkillAsync(string id);
        Task<int?> GetUserSkillCountByIdAsync(int id);
        Task<bool> SaveChangesAsync();
    }

    public class UserSkillRepo : IUserSkillRepo
    {
        private readonly AppDbContext _context;
        private readonly ProjectVariables _projectVariables;

        public UserSkillRepo(AppDbContext context, IOptions<ProjectVariables> options)
        {
            _context = context;
            _projectVariables = options.Value;
        }

        public async Task<UserSkill?> GetUserSkillByIdAsync(string userId)
        {
            userId = GeneralPurpose.ConversionEncryptedId(userId);
            var decryptedUserId = DecryptionId(userId);

            return await _context.UserSkill.FindAsync(decryptedUserId);
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

        public async Task<bool> AddUserSkillAsync(string userId, string skill)
        {
            try
            {
                userId = GeneralPurpose.ConversionEncryptedId(userId);
                var decryptedUserId = DecryptionId(userId);
                var obj = MappingSkills(decryptedUserId, skill);
                await _context.UserSkill.AddAsync(obj);
                return true;
            }
            catch(Exception ex) 
            {
                CreateLogger(ex);
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
            catch(Exception ex)
            {
                CreateLogger(ex);
                return false;
            }
        }

        public async Task<bool> SoftDeleteUserSkillAsync(string id)
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
                CreateLogger(ex);
                return false;
            }
        }

        public async Task<int?> GetUserSkillCountByIdAsync(int id)
        {
            try
            {
                return await _context.UserSkill
                    .Where(x => x.Id == id)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return 0;
            }
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

        private UserSkill MappingSkills(int decryptedUserId, string skill)
        {
            return new UserSkill
            {
                SkillName = skill,
                UserId = decryptedUserId,
                IsActive = 1,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
        }

        private UserSkillDto MapToUserSkillDto(UserSkill skill)
        {
            return new UserSkillDto
            {
                Id = skill.Id,
                UserSkillEncId = StringCipher.EncryptId(skill.Id),
                SkillName = skill.SkillName,
                UserId = skill.UserId
            };
        }

        private int DecryptionId(string userId)
        {
            var decrypt = StringCipher.DecryptId(userId);
            return decrypt;
        }

        private async void CreateLogger(Exception ex)
        {
            await MailSender.SendErrorMessage($"URL: {_projectVariables.BaseUrl}<br/> Exception Message:  {ex.Message} <br/> Stack Trace: {ex.StackTrace}");
        }
    }
}
