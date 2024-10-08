using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface IUserTagRepo
    {
        Task<UserTag?> GetUserTagById(int id);
        Task<IEnumerable<UserTag>> GetUserTagByUserId(int userId);
        Task<IEnumerable<UserTag>> GetUserTagList();
        Task<bool> AddUserTag(UserTag userTag);
        Task<bool> UpdateUserTag(UserTag userTag);
        Task<bool> DeleteUserTag(int id);
    }

    public class UserTagRepo : IUserTagRepo
    {
        private readonly AppDbContext _context;
        public UserTagRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }
        public async Task<bool> AddUserTag(UserTag userTag)
        {
            try
            {
                _context.UserTag.Add(userTag);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserTag(int id)
        {
            try
            {
                UserTag? Tag = await GetUserTagById(id);

                if (Tag != null)
                {
                    Tag.IsActive = 0;
                    Tag.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateUserTag(Tag);
                }
                return false;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }

        public async Task<UserTag?> GetUserTagById(int id)
        {
            return await _context.UserTag.FindAsync(id);
        }

        public async Task<IEnumerable<UserTag>> GetUserTagByUserId(int userId)
        {
            var getListOfTags = await _context.UserTag.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.UserId == userId).ToListAsync();
            return getListOfTags;
        }

        public async Task<IEnumerable<UserTag>> GetUserTagList()
        {
            return await _context.UserTag.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<bool> UpdateUserTag(UserTag userTag)
        {
            try
            {
                _context.Entry(userTag).State = EntityState.Modified;
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
