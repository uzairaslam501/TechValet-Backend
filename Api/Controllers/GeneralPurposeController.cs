using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
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
        private readonly IUserRepo userRepo;
        private readonly IJwtUtils jwtUtils;
        private readonly ProjectVariables projectVariables;
        public GeneralPurposeController(IUserRepo _userRepo, IOptions<ProjectVariables> options)
        {
            userRepo = _userRepo;
            projectVariables = options.Value;
        }

        [HttpGet("validateEmail")]
        public async Task<bool> validateEmail(string Email, string UserId = "")
        {
            int id = -1;
            if (!String.IsNullOrEmpty(UserId) && UserId != "-1")
            {
                id = StringCipher.DecryptId(UserId);
            }
            bool chkUser = await userRepo.ValidateEmail(Email, id);
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
            bool chkUser = await userRepo.ValidateUsername(username, id);
            return chkUser;
        }
    }
}
