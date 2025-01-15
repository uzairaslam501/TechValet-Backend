using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    public class GeneralPurposeController : ControllerBase
    {
        private readonly IUserRepo _userRepo;
        private readonly ProjectVariables _projectVariables;
        private readonly ISearchLogService _searchLogService;
        
        public GeneralPurposeController(IUserRepo userRepo, IOptions<ProjectVariables> options, 
            ISearchLogService searchLogService)
        {
            _userRepo = userRepo;
            _projectVariables = options.Value;
            _searchLogService = searchLogService;
        }

        [HttpGet("validateEmail")]
        public async Task<bool> validateEmail(string Email, string UserId = "")
        {
            int id = -1;
            if (!String.IsNullOrEmpty(UserId) && UserId != "-1")
            {
                id = StringCipher.DecryptId(UserId);
            }
            bool chkUser = await _userRepo.ValidateEmail(Email, id);
            return chkUser;
        }

        [HttpGet("validateUsername")]
        public async Task<bool> validateUsername(string username, string UserId = "")
        {
            int id = -1;
            if (!String.IsNullOrEmpty(UserId) && UserId != "-1")
            {
                id = StringCipher.DecryptId(UserId);
            }
            bool chkUser = await _userRepo.ValidateUsername(username, id);
            return chkUser;
        }

        [HttpGet("GetValetsBySkill/{skill}")]
        public async Task<IActionResult> GetValetsBySkill(string skill)
        {
            var response = await _searchLogService.SearchValetsBySkill(skill);
            return Ok(response);
        }
    }
}
