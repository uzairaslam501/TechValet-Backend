using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface IUserSkillRepo
    {
        Task<UserSkill?> GetUserSkillById(int id);
        Task<IEnumerable<UserSkill>> GetUserSkillByUserId(int userId);
        Task<IEnumerable<UserSkill>> GetUserSkillList();
        Task<IEnumerable<UserSkill>> GetUserBySkillName(string skillName);
        Task<bool> AddUserSkill(UserSkill userSkill);
        Task<bool> AddUserSkill2(UserSkill userSkill);
        Task<bool> UpdateUserSkill(UserSkill userSkill);
        Task<bool> DeleteUserSkill(int id);
        Task<bool> DeleteUserSkill2(int id);
        Task<bool> saveChangesFunction();
        Task<int?> GetUserSkillCountById(int id);
    }

    public class UserSkillRepo : IUserSkillRepo
    {
        private readonly AppDbContext _context;
        public UserSkillRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }
        public async Task<bool> AddUserSkill(UserSkill userSkill)
        {
            try
            {
                _context.UserSkill.Add(userSkill);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<int?> GetUserSkillCountById(int id)
        {
            return await _context.UserSkill.Where(x => x.Id == id).CountAsync();
        }
        public async Task<bool> AddUserSkill2(UserSkill userSkill)
        {
            try
            {
                _context.UserSkill.Add(userSkill);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserSkill(int id)
        {
            try
            {
                UserSkill? Skill = await GetUserSkillById(id);

                if (Skill != null)
                {
                    Skill.IsActive = 0;
                    Skill.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateUserSkill(Skill);
                }
                return false;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }

        public async Task<bool> DeleteUserSkill2(int id)
        {
            try
            {
                UserSkill? userSkill = await GetUserSkillById(id);
                userSkill.IsActive = 0;
                userSkill.DeletedAt = GeneralPurpose.DateTimeNow();
                _context.Entry(userSkill).State = EntityState.Modified;
                return true;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }

        public async Task<UserSkill?> GetUserSkillById(int id)
        {
            return await _context.UserSkill.FindAsync(id);
        }

        public async Task<IEnumerable<UserSkill>> GetUserSkillByUserId(int userId)
        {
            var getListOfSkills = await _context.UserSkill.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.UserId == userId).ToListAsync();
            return getListOfSkills;
        }

        public async Task<IEnumerable<UserSkill>> GetUserBySkillName(string skillName)
        {
            var getListOfSkills = await _context.UserSkill.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.SkillName == skillName).ToListAsync();
            return getListOfSkills;
        }

        public async Task<IEnumerable<UserSkill>> GetUserSkillList()
        {
            return await _context.UserSkill.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<bool> UpdateUserSkill(UserSkill userSkill)
        {
            try
            {
                _context.Entry(userSkill).State = EntityState.Modified;
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



    }
}
