using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface IUserExperienceRepo
    {
        Task<UserExperience?> GetUserExperienceById(int id);
        Task<IEnumerable<UserExperience>> GetUserExperienceByUserId(int userId);
        Task<IEnumerable<UserExperience>> GetUserExperienceList();
        Task<bool> AddUserExperience(UserExperience userExperience);
        Task<bool> UpdateUserExperience(UserExperience userExperience);
        Task<bool> DeleteUserExperience(int id);
        Task<int> GetUserExperienceCountByUserId(int userId);
        Task<List<UserExperiencedViewModel>> UserExperiencedRecordById(int Id);
    }

    public class UserExperienceRepo : IUserExperienceRepo
    {
        private readonly AppDbContext _context;
        public UserExperienceRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }
        public async Task<bool> AddUserExperience(UserExperience userExperience)
        {
            try
            {
                _context.UserExperience.Add(userExperience);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserExperience(int id)
        {
            try
            {
                UserExperience? Experience = await GetUserExperienceById(id);

                if (Experience != null)
                {
                    Experience.IsActive = 0;
                    Experience.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateUserExperience(Experience);
                }
                return false;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }
        public async Task<int> GetUserExperienceCountByUserId(int userId)
        {
            int getCountOfExperiences = await _context.UserExperience.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.UserId == userId).CountAsync();
            return getCountOfExperiences;
        }

        public async Task<UserExperience?> GetUserExperienceById(int id)
        {
            return await _context.UserExperience.FindAsync(id);
        }

        public async Task<IEnumerable<UserExperience>> GetUserExperienceByUserId(int userId)
        {
            var getListOfExperiences = await _context.UserExperience.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.UserId == userId).ToListAsync();
            return getListOfExperiences;
        }

        public async Task<IEnumerable<UserExperience>> GetUserExperienceList()
        {
            return await _context.UserExperience.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<bool> UpdateUserExperience(UserExperience userExperience)
        {
            try
            {
                _context.Entry(userExperience).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<List<UserExperiencedViewModel>> UserExperiencedRecordById(int Id)
        {
            List<UserExperiencedViewModel> userExperiencedList = new List<UserExperiencedViewModel>();
            try
            {
                var userExperiencedRecord = await GetUserExperienceByUserId(Id);
                if (userExperiencedRecord.Any())
                {
                    foreach (var item in userExperiencedRecord)
                    {
                        UserExperiencedViewModel experienced = new UserExperiencedViewModel();
                        experienced.Title = item.Title;
                        experienced.Description = item.Description;
                        experienced.WebSite = item.Website;
                        experienced.Organization = item.Organization;
                        experienced.ExperienceFrom = item.ExperienceFrom.ToString();
                        experienced.ExperienceTo = item.ExperienceTo.ToString();
                        userExperiencedList.Add(experienced);

                    }
                    return userExperiencedList;
                }
                return new List<UserExperiencedViewModel>();
            }
            catch (Exception ex)
            {
                return new List<UserExperiencedViewModel>();
            }
        }
    }
}
