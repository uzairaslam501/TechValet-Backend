using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ITValet.Services
{
    public interface IUserEducationRepo
    {
        Task<UserEducation?> GetUserEducationById(int id);
        Task<IEnumerable<UserEducation>> GetUserEducationByUserId(int userId);
        Task<IEnumerable<UserEducation>> GetUserEducationList();
        Task<bool> AddUserEducation(UserEducation userEducation);
        Task<bool> UpdateUserEducation(UserEducation userEducation);
        Task<bool> DeleteUserEducation(int id);
        Task<int> GetUserEducationCountByUserId(int userId);
        Task<List<UserEducationViewModel>> UserEducationRecordById(int Id);
    }

    public class UserEducationRepo : IUserEducationRepo
    {
        private readonly AppDbContext _context;
        public UserEducationRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }
        public async Task<bool> AddUserEducation(UserEducation userEducation)
        {
            try
            {
                _context.UserEducation.Add(userEducation);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserEducation(int id)
        {
            try
            {
                UserEducation? education = await GetUserEducationById(id);

                if (education != null)
                {
                    education.IsActive = 0;
                    education.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateUserEducation(education);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<int> GetUserEducationCountByUserId(int userId)
        {
            int getCountOfEducations = await _context.UserEducation.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.UserId == userId).CountAsync();
            return getCountOfEducations;
        }

        public async Task<UserEducation?> GetUserEducationById(int id)
        {
            return await _context.UserEducation.FindAsync(id);
        }

        public async Task<IEnumerable<UserEducation>> GetUserEducationByUserId(int userId)
        {
            var getListOfEducations = await _context.UserEducation.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.UserId == userId).ToListAsync();
            return getListOfEducations;
        }

        public async Task<IEnumerable<UserEducation>> GetUserEducationList()
        {
            return await _context.UserEducation.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<bool> UpdateUserEducation(UserEducation userEducation)
        {
            try
            {
                _context.Entry(userEducation).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }

        public async Task<List<UserEducationViewModel>> UserEducationRecordById(int Id)
        {
            List<UserEducationViewModel> userEducationList = new List<UserEducationViewModel>();
            try
            {
                var userEducationRecord = await GetUserEducationByUserId(Id);
                if (userEducationRecord.Any())
                {
                    foreach (var item in userEducationRecord)
                    {
                        UserEducationViewModel education = new UserEducationViewModel();
                        education.DegreeName = item.DegreeName;
                        education.InstituteName = item.InstituteName;
                        education.FromDate = item.StartDate.Value.Date.ToString("MM/dd/yyyy");
                        education.ToDate = item.EndDate.Value.Date.ToString("MM/dd/yyyy");
                        userEducationList.Add(education);
                    }
                    return userEducationList;
                }
                return new List<UserEducationViewModel>();
            }
            catch (Exception ex)
            {
                return new List<UserEducationViewModel>();
            }     
        }
    }
}
