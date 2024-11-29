using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using System.Diagnostics.Eventing.Reader;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepo userRepo;
        private readonly IJwtUtils jwtUtils;
        private readonly ProjectVariables projectVariables;
        private readonly IPayPalGateWayService _payPalGateWayService;
        private readonly IUserExperienceRepo userExperienceRepo;
        private readonly IUserEducationRepo userEducationRepo;
        private readonly IUserSkillRepo userSkillRepo;
        private readonly IUserAvailableSlotRepo userAvailableSlotRepo;

        public AuthController(IUserEducationRepo _userEducationRepo, IUserExperienceRepo _userExperienceRepo,
            IUserSkillRepo _userSkillRepo, IPayPalGateWayService payPalGateWayService,
            IUserRepo _userRepo, IUserAvailableSlotRepo _userAvailableSlotRepo, IJwtUtils _jwtUtils, IOptions<ProjectVariables> options)
        {
            userRepo = _userRepo;
            jwtUtils = _jwtUtils;
            projectVariables = options.Value;
            _payPalGateWayService = payPalGateWayService;
            userExperienceRepo = _userExperienceRepo;
            userSkillRepo = _userSkillRepo;
            userEducationRepo = _userEducationRepo;
            userAvailableSlotRepo = _userAvailableSlotRepo;
        }

        [HttpPost("Login")]
        public async Task<ActionResult<UserClaims>> PostLogin(LoginDto loginDto)
        {
            if(string.IsNullOrEmpty(loginDto.Email) || string.IsNullOrEmpty(loginDto.Password))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.EmailPassword });
            }
            User? user = await userRepo.GetUserByLogin(loginDto.Email, loginDto.Password);

            if (user == null)
            {
                return NotFound(new ResponseDto() { Status = false, StatusCode = "404", Message = GlobalMessages.LoginNotFound });
            }
            int IsCompleteValetAccount = 1;
            if (user.Role == 4)
            {
                IsCompleteValetAccount = await GeneralPurpose.CheckValuesNotEmpty(user, userExperienceRepo, userSkillRepo, _payPalGateWayService, userEducationRepo);
                if (IsCompleteValetAccount == 1)
                {
                    var record = await userAvailableSlotRepo.GetUserAvailableSlotByUserId((int)user.Id);
                    if (record.Count() <= 0)
                    {
                        await userAvailableSlotRepo.CreateEntriesForCurrentMonth(user.Id);
                    }
                }
            }

            UserClaims obj = new UserClaims()
            {
                Id = user.Id,
                UserEncId = StringCipher.EncryptId(user.Id),
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                IsActive = Enum.GetName(typeof(EnumActiveStatus), user.IsActive),
                Role = Enum.GetName(typeof(EnumRoles), user.Role),
                Token = jwtUtils.GenerateToken(user),
                ProfilePicture = user.ProfilePicture != null ? projectVariables.BaseUrl + user.ProfilePicture : null,
                IsCompleteValetAccount = IsCompleteValetAccount.ToString(),
            };

            return Ok(new ResponseDto() { Data = obj, Status = true, StatusCode = "200" });
        }

        [CustomAuthorize]
        [HttpGet("GetLoggedinUser")]
        public async Task<ActionResult<UserListDto>> LoggedinUser(int id)
        {
            var user = await userRepo.GetUserById(id);

            if (user == null)
            {
                return NotFound(new ResponseDto() { Status = false, StatusCode = "404", Message = GlobalMessages.RecordNotFound });
            }
            var baseUri = $"{Request.Scheme}://{Request.Host}/profiles/";

            UserListDto obj = new UserListDto()
            {
                Id = user.Id,
                UserEncId = StringCipher.EncryptId(user.Id),
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Contact = user.Contact,
                Email = user.Email,
                Password = StringCipher.Decrypt(user.Password),
                Gender = user.Gender,
                Country = user.Country,
                State = user.State,
                City = user.City,
                ZipCode = user.ZipCode,
                Timezone = user.Timezone,
                Availability = user.Availability.ToString(),
                Status = user.Status.ToString(),
                BirthDate = user.BirthDate.ToString(),
                Role = Enum.GetName(typeof(EnumRoles), user.Role),
                IsActive = Enum.GetName(typeof(EnumActiveStatus), user.IsActive),
                Language = user.Language,
                ProfilePicture = user.ProfilePicture != null ? baseUri + user.ProfilePicture : null,
            };

            return Ok(new ResponseDto() { Data = obj , Status = true, StatusCode = "200"});
        }

        #region Registeration
        [HttpPost("UserRegisteration")]
        public async Task<ActionResult> UserRegisteration(PostAddUserDto user)
        {
            var obj = new User();

            if (!await userRepo.ValidateEmail(user.Email))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.DuplicateEmail });
            }
            obj.FirstName = user.FirstName;
            obj.LastName = user.LastName;
            obj.UserName = user.UserName;
            obj.Contact = user.Contact;
            obj.Email = user.Email;
            obj.Password = StringCipher.Encrypt(user.Password);
            obj.BirthDate = Convert.ToDateTime(user.BirthDate);
            obj.Country = user.Country;
            obj.State = user.Status;
            obj.City = user.City;
            obj.ZipCode = user.ZipCode;
            obj.Timezone = user.Timezone;
            obj.Availability = Convert.ToInt32(user.Availability);
            obj.Status = Convert.ToInt32(user.Status);
            obj.Gender = user.Gender;
            obj.StripeId = user.StripeId;
            obj.IsActive = 1;
            obj.CreatedAt = GeneralPurpose.DateTimeNow();
            if (user.SignUpOption == "Customer")
            {
                obj.Role = 3;
                obj.IsActive = 2;
            }
            if (user.SignUpOption == "ITValet")
            {
                obj.Role = 4;
                obj.PricePerHour = Convert.ToDecimal(24.99);
                obj.IsActive = 2;
                obj.HST = 13;
            }
            bool chkUserAdded = await userRepo.AddUser(obj);
            if (chkUserAdded == false)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
            if (obj.Role == 4 || obj.Role == 3)
            {
                bool chkMailSent = await MailSender.SendEmailForITValetAdminVerfication(obj.Email, obj.UserName, (int)obj.Role);
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = GlobalMessages.SuccessMessage });
        }
        #endregion

        #region Manage Profile
        [CustomAuthorize]
        [HttpPut]
        [Route("UpdateProfile")]
        public async Task<ActionResult> PostUpdateProfile(PostUpdateUserDto user)
        {
            UserClaims? getUsetFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
            User? obj = await userRepo.GetUserById((int)getUsetFromToken.Id!);

            if (obj == null)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = GlobalMessages.RecordNotFound });
            }

            obj.FirstName = !string.IsNullOrEmpty(user.FirstName)  ? user.FirstName : obj.FirstName;
            obj.LastName = !string.IsNullOrEmpty(user.LastName) ? user.LastName : obj.LastName;
            obj.Contact = !string.IsNullOrEmpty(user.Contact) ? user.Contact : obj.Contact;
            obj.BirthDate = !string.IsNullOrEmpty(user.BirthDate) ? Convert.ToDateTime(user.BirthDate) : obj.BirthDate;
            obj.Country = !string.IsNullOrEmpty(user.Country) ? user.Country : obj.Country;
            obj.State = !string.IsNullOrEmpty(user.State) ? user.State : obj.State;
            obj.City = !string.IsNullOrEmpty(user.City) ? user.City : obj.City;
            obj.ZipCode = !string.IsNullOrEmpty(user.ZipCode) ? user.ZipCode : obj.ZipCode;
            obj.Timezone = !string.IsNullOrEmpty(user.Timezone) ? user.Timezone : obj.Timezone;
            obj.Availability = !string.IsNullOrEmpty(user.Availability) ? Convert.ToInt32(user.Availability) : obj.Availability;
            obj.Status = !string.IsNullOrEmpty(user.Status) ? Convert.ToInt32(user.Status) : obj.Status;
            obj.Gender = !string.IsNullOrEmpty(user.Gender) ? user.Gender : obj.Gender;
            obj.PricePerHour = !string.IsNullOrEmpty(user.PricePerHour) ? Convert.ToDecimal(user.PricePerHour) : obj.PricePerHour;
            obj.Language = !string.IsNullOrEmpty(user.Language) ? user.Language : obj.Language;

            if (!await userRepo.ValidateEmail(obj.Email, obj.Id))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.DuplicateEmail });
            }

            if (!await userRepo.UpdateUser(obj))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
            var baseUri = $"{Request.Scheme}://{Request.Host}/profiles/";

            UserClaims loggedin = new UserClaims()
            {
                Id = obj.Id,
                UserEncId = StringCipher.EncryptId((int)obj.Id),
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = obj.UserName,
                Email = obj.Email,
                Role = Enum.GetName(typeof(EnumRoles), obj.Role!),
                Token = jwtUtils.GenerateToken(obj),
                ProfilePicture = user.ProfilePicture != null ? baseUri + user.ProfilePicture : obj.ProfilePicture,
                Contact = obj.Contact,
                BirthDate = obj.BirthDate.ToString(),
                Country = obj.Country,
                State = obj.State,
                City = obj.City,
                ZipCode = obj.ZipCode,
                Timezone = obj.Timezone,
                Availability = obj.Availability,
                Gender = obj.Gender,
                StripeId = obj.StripeId,
                PricePerHour = obj.PricePerHour.ToString(),
                IsActive = Enum.GetName(typeof(EnumActiveStatus), obj.IsActive!),
            };

            return Ok(new ResponseDto() { Data = loggedin,  Status = true, StatusCode = "200", Message = GlobalMessages.UpdateMessage});
        }


        [CustomAuthorize]
        [HttpPut]
        [Route("UpdatePassword")]
        public async Task<ActionResult> PostUpdatePassword(UpdatePasswordDto user)
        {
            UserClaims? obj = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());

            if (obj == null)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = GlobalMessages.RecordNotFound });
            }
            
            var getLoggedInUser= await userRepo.GetUserById((int)obj.Id);

            if (StringCipher.Decrypt(getLoggedInUser.Password) != user.OldPassword)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = GlobalMessages.OldPassword });
            }

            getLoggedInUser.Password = StringCipher.Encrypt(user.Password.Trim());


            if (!await userRepo.UpdateUser(getLoggedInUser))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }

            return Ok(new ResponseDto() { Data = getLoggedInUser, Status = true, StatusCode = "200", Message = GlobalMessages.PasswordUpdated });
        }
        #endregion

        #region Manage Forgot Password
        [HttpPost("ForgotPassword")]
        public async Task<ActionResult> PostForgotPassword(ForgotPasswordDto forgot)
        {
            var BaseUrl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/";
            var obj = await userRepo.GetUserByEmail(forgot.Email);

            if (obj == null)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = GlobalMessages.EmailNotFound });
            }
            BaseUrl = BaseUrl + "ResetPassword/" + StringCipher.EncryptId(obj.Id);
            MailSender mailSender = new MailSender();
            if (!await mailSender.SendForgotEmail(obj.Email, BaseUrl))
            {
                return BadRequest(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.EmailSendFailed });
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Mail sent, please check at " + obj.Email + "." });
        }

        [HttpPost("ResetPassword")]
        public async Task<ActionResult> PostResetPassword(ResetPasswordDto reset)
        {
            var obj = await userRepo.GetUserById(Convert.ToInt32(StringCipher.DecryptId(reset.UserId)));
            if (reset.NewPassword != reset.ConfirmPassword)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = GlobalMessages.PasswordNotMatched });
            }

            if (obj == null)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = GlobalMessages.SystemFailureMessage });
            }

            obj.Password = StringCipher.Encrypt(reset.NewPassword.Trim());

            if (!await userRepo.UpdateUser(obj))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = GlobalMessages.PasswordUpdated });
        }
        #endregion

        #region
        [HttpGet("ConfirmAccount")]
        public async Task<IActionResult> ConfirmAccount(string Id, long t)
        {
            var dt = DateTime.Now.Ticks;
            if (dt < t)
            {
                User obj = await userRepo.GetUserById(StringCipher.DecryptId(Id));

                if (obj == null)
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "No User Found" });
                }
                obj.IsActive = (int)EnumActiveStatus.Active;

                if (!await userRepo.SaveChanges())
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Failed Activate Your Account" });
                }
            }
            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Your Account Activated Successfully" });
        }
            
        [HttpGet("PostForgotPassword")]
        public async Task<IActionResult> PostForgotPassword(string Email)
        {

            User obj = await userRepo.GetUserByEmail(Email);

            if (obj == null || obj.IsActive == 0)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "No User Found" });
            }

            bool chkIfMailSent = await MailSender.EmailForgetPassword(StringCipher.EncryptId(obj.Id), obj.UserName, obj.Email, projectVariables.BaseUrl);
            if (!chkIfMailSent)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Failed to Send Forget Password Recovery Mail" });
            }
           
            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Password Recovery Mail Sent Successfully" });
        }
            
        [HttpGet("PostRenewPassword")]
        public async Task<IActionResult> PostRenewPassword(string Id, string Password, long t)
        {
            var dt = DateTime.Now.Ticks;
            if (dt < t)
            {
                User obj = await userRepo.GetUserById(StringCipher.DecryptId(Id));

                if (obj == null)
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "No User Found" });
                }
                obj.Password = StringCipher.Encrypt(Password);

                if (!await userRepo.SaveChanges())
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Failed To Change Your Password" });
                }
            }
            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Password Updated Successfully" });
        }

        #endregion
    }
}
