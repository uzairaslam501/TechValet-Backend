using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace ITValet.Services
{
    public interface ISearchLogService
    {
        Task<bool> InsertOrUpdateSearchValue(string keyword);
        Task<List<SearchedUserList>> SearchValetsAndSkillsByKey(string searchKeyword);
        Task<List<string?>> GetHighSearchVolumeKeys();
    }

    public class SearchLogService : ISearchLogService
    {
        private readonly IUserSkillRepo _userSkillService;
        private readonly IUserRepo _userService;
        private readonly AppDbContext _context;
        private readonly IUserRatingRepo _userRatingRepo;
		private readonly ProjectVariables projectVariables;

		public SearchLogService(IUserRepo userService, IUserSkillRepo userSkillService, AppDbContext context, IUserRatingRepo userRatingRepo, IOptions<ProjectVariables> options)
        {       
            _userSkillService = userSkillService;
            _userService = userService;
            _context = context;  
            _userRatingRepo = userRatingRepo;
			projectVariables = options.Value;

		}

		public async Task<List<SearchedUserList>> SearchValetsAndSkillsByKey(string searchKeyword)
        {
            try
            {
                bool updateSearchLog = await InsertOrUpdateSearchValue(searchKeyword);
                var findUsersFromSkills = await _userSkillService.GetUserBySkillName(searchKeyword);

                List<SearchedUserList> searchedUsers = new List<SearchedUserList>();

                if (findUsersFromSkills.Any())
                {
                    List<int?> userIds = findUsersFromSkills.Select(userSkill => userSkill.UserId).ToList();
                    var skilledUsers = await _userService.GetSkilledUsersByIds(userIds);

                    // Transform the skilledUsers to SearchedUserList view model
                    searchedUsers = skilledUsers.Select(user => new SearchedUserList
                    {
                        UserProfile = projectVariables.BaseUrl + user.ProfilePicture,
                        UserName = user.UserName,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Status = user.Status,
                        UserDescription = user.Description,
                        City = user.City,
                        Country = user.Country,
                        AverageStars = _userRatingRepo.CalculateAverageStars(user.Id),
                        EncUserId = StringCipher.EncryptId(user.Id),
                        PricePerHours = user.PricePerHour
                    }).ToList();
                }
                else
                {
                    var searchUserByName = await _userService.GetUsersByName(searchKeyword);
                    searchedUsers = searchUserByName.Select(user => new SearchedUserList
                    {
                        UserProfile = projectVariables.BaseUrl + user.ProfilePicture,
                        UserName = user.UserName,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Status = user.Status,
                        UserDescription = user.Description,
                        City = user.City,
                        Country = user.Country,
                        AverageStars = _userRatingRepo.CalculateAverageStars(user.Id),
                        EncUserId = StringCipher.EncryptId(user.Id),
                        PricePerHours = user.PricePerHour
                    }).ToList();
                }

                // Sort searchedUsers by AverageStars in descending order
                searchedUsers = searchedUsers.OrderByDescending(user => double.Parse(user.AverageStars)).ToList();

                return searchedUsers;
            }
            catch (Exception ex)
            {
                // Handle exceptions if needed
                return new List<SearchedUserList>();
            }
        }


        public async Task<bool> InsertOrUpdateSearchValue(string keyword)
        {
            try
            {
                // Check if the keyword already exists in the database
                var existingSearchLog = await _context.SearchLog.FirstOrDefaultAsync(sl => sl.SearchKeyword == keyword);

                if (existingSearchLog != null)
                {
                    // Keyword already exists, update the search count
                    existingSearchLog.SearchKeywordCount++;
                    existingSearchLog.UpdatedAt = GeneralPurpose.DateTimeNow();
                }
                else
                {
                    // Keyword is new, create a new SearchLog entry
                    SearchLog searchLog = new SearchLog();
                    searchLog.SearchKeyword = keyword;
                    searchLog.IsActive = (int)EnumActiveStatus.Active;
                    searchLog.CreatedAt = DateTime.Now;
                    searchLog.SearchKeywordCount = 1;
                    _context.SearchLog.Add(searchLog);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<List<string?>> GetHighSearchVolumeKeys()
        {
            try
            {
                var highlySearchedKey = await _context.SearchLog.OrderByDescending(x => x.SearchKeywordCount).
                    Select(x => x.SearchKeyword).Take(5).ToListAsync();
                return highlySearchedKey
;           }
            catch (Exception ex)
            {
                return null;
            }
        }

    }
}
