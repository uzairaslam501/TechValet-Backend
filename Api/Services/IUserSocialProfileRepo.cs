using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface IUserSocialProfileRepo
    {
        Task<UserSocialProfile?> GetUserSocialProfileById(int id);
        Task<IEnumerable<UserSocialProfile>> GetUserSocialProfileByUserId(int userId);
        Task<IEnumerable<UserSocialProfile>> GetUserSocialProfileList();
        Task<bool> AddUserSocialProfile(UserSocialProfile userSocialProfile);
        Task<bool> UpdateUserSocialProfile(UserSocialProfile userSocialProfile);
        Task<bool> DeleteUserSocialProfile(int id);
    }

    public class UserSocialProfileRepo : IUserSocialProfileRepo
    {
        private readonly AppDbContext _context;
        public UserSocialProfileRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }
        public async Task<bool> AddUserSocialProfile(UserSocialProfile userSocialProfile)
        {
            try
            {
                _context.UserSocialProfile.Add(userSocialProfile);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserSocialProfile(int id)
        {
            try
            {
                UserSocialProfile? SocialProfile = await GetUserSocialProfileById(id);

                if (SocialProfile != null)
                {
                    SocialProfile.IsActive = 0;
                    SocialProfile.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateUserSocialProfile(SocialProfile);
                }
                return false;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }

        public async Task<UserSocialProfile?> GetUserSocialProfileById(int id)
        {
            return await _context.UserSocialProfile.FindAsync(id);
        }

        public async Task<IEnumerable<UserSocialProfile>> GetUserSocialProfileByUserId(int userId)
        {
            var getListOfSocialProfiles = await _context.UserSocialProfile.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.UserId == userId).ToListAsync();
            return getListOfSocialProfiles;
        }

        public async Task<IEnumerable<UserSocialProfile>> GetUserSocialProfileList()
        {
            return await _context.UserSocialProfile.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<bool> UpdateUserSocialProfile(UserSocialProfile userSocialProfile)
        {
            try
            {
                _context.Entry(userSocialProfile).State = EntityState.Modified;
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
