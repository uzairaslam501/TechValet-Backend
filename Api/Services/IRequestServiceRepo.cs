using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface IRequestServiceRepo
    {
        Task<RequestService?> GetRequestServiceById(int id);
        Task<IEnumerable<RequestService>> GetRequestServiceByUserId(int userId);
        Task<IEnumerable<RequestService>> GetRequestServiceList();
        Task<bool> AddRequestService(RequestService requestService);
        Task<int> AddRequestServiceReturnId(RequestService requestService);
        Task<bool> UpdateRequestService(RequestService requestService);
        Task<bool> DeleteRequestService(int id);
        Task<bool> ValidateServiceTitle(string serviceTitle, int UserId);
        #region Skill
        Task<RequestServiceSkill?> GetRequestServiceSkillById(int id);
        Task<IEnumerable<RequestServiceSkill>> GetRequestServiceSkillByRequestServiceId(int requestServiceId);
        Task<IEnumerable<RequestServiceSkill>> GetRequestServiceSkillList();
        Task<bool> AddRequestServiceSkill(RequestServiceSkill requestServiceSkill);
        Task<int> AddRequestServiceSkillReturnId(RequestServiceSkill requestServiceSkill);
        Task<bool> UpdateRequestServiceSkill(RequestServiceSkill requestServiceSkill);
        Task<bool> DeleteRequestServiceSkill(int id);
        #endregion

        Task<bool> SaveChanges();
    }

    public class RequestServiceRepo : IRequestServiceRepo
    {
        private readonly AppDbContext _context;
        public RequestServiceRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }
        public async Task<bool> AddRequestService(RequestService requestService)
        {
            try
            {
                _context.RequestService.Add(requestService);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<int> AddRequestServiceReturnId(RequestService requestService)
        {
            try
            {
                _context.RequestService.Add(requestService);
                await _context.SaveChangesAsync();
                return requestService.Id;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public async Task<bool> DeleteRequestService(int id)
        {
            try
            {
                RequestService? requestService = await GetRequestServiceById(id);

                if (requestService != null)
                {
                    var getSkills = await GetRequestServiceSkillByRequestServiceId(id);
                    if(getSkills.Count() > 0)
                    {
                        foreach(var item in getSkills)
                        {
                            await DeleteRequestServiceSkill(item.Id);
                        }
                    }
                    requestService.IsActive = 0;
                    requestService.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateRequestService(requestService);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<RequestService?> GetRequestServiceById(int id)
        {
            return await _context.RequestService.FindAsync(id);
        }

        public async Task<IEnumerable<RequestService>> GetRequestServiceByUserId(int userId)
        {
            var getListOfRequestServices = await _context.RequestService.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.RequestedServiceUserId == userId).ToListAsync();
            return getListOfRequestServices;
        }

        public async Task<IEnumerable<RequestService>> GetRequestServiceList()
        {
            return await _context.RequestService.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<bool> UpdateRequestService(RequestService requestService)
        {
            try
            {
                _context.Entry(requestService).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> ValidateServiceTitle(string serviceTitle, int UserId)
        {
            var getService = await _context.RequestService.Where(x => x.IsActive == (int)EnumActiveStatus.Active)
                .Where(x => x.ServiceTitle.ToLower().Equals(serviceTitle.ToLower()))
                .ToListAsync(); ;
            if (getService != null)
            {
                return false;
            }
            return true;
        }

        #region Skill
        public async Task<bool> AddRequestServiceSkill(RequestServiceSkill requestServiceSkill)
        {
            try
            {
                _context.RequestServiceSkill.Add(requestServiceSkill);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }

        public async Task<int> AddRequestServiceSkillReturnId(RequestServiceSkill RequestServiceSkill)
        {
            try
            {
                _context.RequestServiceSkill.Add(RequestServiceSkill);
                await _context.SaveChangesAsync();
                return RequestServiceSkill.Id;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public async Task<bool> DeleteRequestServiceSkill(int id)
        {
            try
            {
                RequestServiceSkill? requestService = await GetRequestServiceSkillById(id);

                if (requestService != null)
                {
                    requestService.IsActive = 0;
                    requestService.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateRequestServiceSkill(requestService);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<RequestServiceSkill?> GetRequestServiceSkillById(int id)
        {
            return await _context.RequestServiceSkill.FindAsync(id);
        }

        public async Task<IEnumerable<RequestServiceSkill>> GetRequestServiceSkillByRequestServiceId(int requestServiceId)
        {
            var getListOfRequestServices = await _context.RequestServiceSkill.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.RequestServiceId == requestServiceId).ToListAsync();
            return getListOfRequestServices;
        }

        public async Task<IEnumerable<RequestServiceSkill>> GetRequestServiceSkillList()
        {
            return await _context.RequestServiceSkill.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<bool> UpdateRequestServiceSkill(RequestServiceSkill requestServiceSkill)
        {
            try
            {
                _context.Entry(requestServiceSkill).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion

        public async Task<bool> SaveChanges()
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
