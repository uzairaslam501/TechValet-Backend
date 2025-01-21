using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.NotificationHub;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

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
        private readonly IHubContext<NotificationHubSocket> _notificationHubSocket;

        public AuthController(IUserEducationRepo _userEducationRepo, IUserExperienceRepo _userExperienceRepo,
            IUserSkillRepo _userSkillRepo, IPayPalGateWayService payPalGateWayService,
            IUserRepo _userRepo, IUserAvailableSlotRepo _userAvailableSlotRepo, IJwtUtils _jwtUtils, IOptions<ProjectVariables> options,
            IHubContext<NotificationHubSocket> notificationHubSocket)
        {
            userRepo = _userRepo;
            jwtUtils = _jwtUtils;
            projectVariables = options.Value;
            _payPalGateWayService = payPalGateWayService;
            userExperienceRepo = _userExperienceRepo;
            userSkillRepo = _userSkillRepo;
            userEducationRepo = _userEducationRepo;
            userAvailableSlotRepo = _userAvailableSlotRepo;
            _notificationHubSocket = notificationHubSocket;
        }

        [HttpPost("Login")]
        public async Task<ActionResult<ResponseDto>> PostLogin(LoginDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return BadRequest(new ResponseDto
                {
                    Status = false,
                    StatusCode = "400",
                    Message = GlobalMessages.EmailPassword
                });
            }

            var user = await userRepo.GetUserByLogin(loginDto.Email, loginDto.Password);
            if (user == null)
            {
                return NotFound(new ResponseDto
                {
                    Status = false,
                    StatusCode = "404",
                    Message = GlobalMessages.LoginNotFound
                });
            }

            var isCompleteValetAccount = user.Role == 4
                ? await HandleValetAccountLogic(user)
                : 1;

            var loggedin = CreateUserClaims(user);
            loggedin.IsCompleteValetAccount = isCompleteValetAccount.ToString();

            return Ok(new ResponseDto
            {
                Data = loggedin,
                Status = true,
                StatusCode = "200"
            });
        }

        #region Registeration
        [HttpPost("Register")]
        public async Task<ActionResult> Register(RegisterUserDto user)
        {
            // Validate email
            if (!await userRepo.ValidateEmail(user.Email!))
                return Conflict(GlobalMessages.DuplicateEmail);

            // Validate username
            if (!await userRepo.ValidateUsername(user.Username!))
                return Conflict(GlobalMessages.DuplicateUsername);

            if (!MatchPassword(user.Password!, user.ConfirmPassword!))
                return BadRequest("Password and Confirm Password must be same.");

            // Map user details
            var obj = new User();
            MapUser(user, obj);
            SetRoles(user, obj);

            // Add user
            if (!await userRepo.AddUser(obj))
                return BadRequest(GlobalMessages.SystemFailureMessage);

            // Send verification email
            if (obj.Role == (int)EnumRoles.Customer || obj.Role == (int)EnumRoles.Valet)
                await MailSender.SendEmailForITValetAdminVerfication(obj.Email!, obj.UserName!, user.Role!);

            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.SuccessMessage, obj));
        }

        private bool MatchPassword(string password, string confirmPassword)
        {
            return password == confirmPassword;
        }

        private void MapUser(RegisterUserDto user, User obj)
        {
            obj = new User
            {
                FirstName = user.Firstname,
                LastName = user.Lastname,
                UserName = user.Username,
                Email = user.Email,
                Password = StringCipher.Encrypt(user.Password!),
                Country = user.Country,
                State = user.State,
                City = user.City,
                ZipCode = user.PostalCode,
                Timezone = user.Timezone,
                IsActive = 1,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
        }

        private void SetRoles(RegisterUserDto user, User obj)
        {
            
            if (!Enum.TryParse<EnumRoles>(user.Role, true, out var role))
                throw new ArgumentException("Invalid or missing role");

            obj.Role = (int)role;

            if (role == EnumRoles.Valet)
            {
                obj.PricePerHour = 24.99m;
                obj.HST = 13;
            }
        }

        #endregion

        #region Manage Profile
        [HttpPut]
        [Route("UpdateProfile/{userId}")]
        public async Task<ActionResult> PostUpdateProfile(string userId, UserViewModel user)
        {
            var decrypted = DecryptionId(userId);
            var obj = await userRepo.GetUserById(decrypted);

            if (obj == null)
            {
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "404",
                    Message = GlobalMessages.RecordNotFound
                });
            }

            UpdateUserProperties(user, obj);

            if (!await userRepo.ValidateEmail(obj.Email!, obj.Id))
            {
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "400",
                    Message = GlobalMessages.DuplicateEmail
                });
            }

            if (!await userRepo.UpdateUser(obj))
            {
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "400",
                    Message = GlobalMessages.SystemFailureMessage
                });
            }

            var loggedin = CreateUserClaims(obj);

            return Ok(new ResponseDto
            {
                Data = loggedin,
                Status = true,
                StatusCode = "200",
                Message = GlobalMessages.UpdateMessage
            });
        }

        [HttpPut("update-profile-image/{userId}")]
        public async Task<IActionResult> UploadPicture(string userId, [FromForm] IFormFile file)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                var decrypt = DecryptionId(userId);
                var user = await userRepo.GetUserById(decrypt);
                if (user == null)
                    return BadRequest(new ResponseDto() { Status = false, StatusCode = "404", Message = "User Not Found" });
                
                user.ProfilePicture = await UploadFiles(file, "profiles");
                if (!await userRepo.UpdateUser(user))
                    return BadRequest(new ResponseDto() { Status = false, StatusCode = "406", Message = "Database Update Failed" });
                
                var isCompleteValetAccount = user.Role == 4
                    ? await GeneralPurpose.CheckValuesNotEmpty(user, userExperienceRepo, userSkillRepo, _payPalGateWayService, userEducationRepo)
                    : 1;

                var loggedIn = CreateUserClaims(user);
                loggedIn.IsCompleteValetAccount = isCompleteValetAccount.ToString();

                return Ok(new ResponseDto() { Data = loggedIn, Status = true, StatusCode = "200", Message = "Image Updated Successfully" });
            }

            return Ok(new ResponseDto() { Status = false, StatusCode = "406", Message = "Invalid Request" });
        }


        [CustomAuthorize]
        [HttpPut]
        [Route("UpdatePassword/{userId}")]
        public async Task<ActionResult> PostUpdatePassword(string userId, UpdatePasswordDto user)
        {
            UserClaims? obj = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());

            if (obj == null)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = GlobalMessages.RecordNotFound });
            }

            var getLoggedInUser = await userRepo.GetUserById((int)obj.Id);

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
            var obj = await userRepo.GetUserById(Convert.ToInt32(DecryptionId(reset.UserId)));
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

        #region Account
        [HttpGet("ConfirmAccount")]
        public async Task<IActionResult> ConfirmAccount(string Id, long t)
        {
            var dt = DateTime.Now.Ticks;
            if (dt < t)
            {
                User obj = await userRepo.GetUserById(DecryptionId(Id));

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
                User obj = await userRepo.GetUserById(DecryptionId(Id));

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

        #region UserActivityStatus
        [HttpPut("user-activity-status/{userId}")]
        public async Task<IActionResult> UpdateUserAccountActivityStatus(string userId, string activityStatus)
        {
            var decryptId = DecryptionId(userId);
            if (activityStatus == "true")
                activityStatus = "1";
            else
                activityStatus = "0";

            var obj = await userRepo.UpdateUserAccountActivityStatus(decryptId, Convert.ToInt32(activityStatus));
            if (!obj)
                return Ok(new ResponseDto() { Data = activityStatus, Status = false, StatusCode = "406", Message = "Database Updation Failed" });
            
            await _notificationHubSocket.Clients.All.SendAsync("UpdateUserStatus", decryptId, activityStatus);
            return Ok(new ResponseDto() { Data = activityStatus, Status = true, StatusCode = "200", Message = "Record Updated Successfully" });
        }

        [HttpPut("user-availability/{userId}")]
        public async Task<IActionResult> UpdateUserAccountAvailabilityStatus(string userId, string availabilityOption)
        {
            var decryptId = DecryptionId(userId);
            if(availabilityOption == "true")
                availabilityOption = "1";
            else
                availabilityOption = "0";
            
            var obj = await userRepo.UpdateUserAccountAvailabilityStatus(decryptId, Convert.ToInt32(availabilityOption));

            if (!obj)
                return Ok(new ResponseDto() { Data = obj, Status = false, StatusCode = "406", Message = "Database Updation Failed" });
            
            return Ok(new ResponseDto() { Data = availabilityOption, Status = true, StatusCode = "200", Message = "Record Updated Successfully" });
        }

        #endregion


        #region Renew Token

        [HttpPost("RenewToken")]
        public async Task<ActionResult<ResponseDto>> PostRenewToken([FromHeader] string Authorization)
        {
            try
            {
                UserClaims? getUserFromToken = null;

                if (!string.IsNullOrEmpty(Authorization))
                {
                    getUserFromToken = jwtUtils.ValidateToken(Authorization);
                }
                else
                {
                    var tokenFromHeader = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    if (string.IsNullOrEmpty(tokenFromHeader))
                    {
                        return Unauthorized(new ResponseDto
                        {
                            Status = false,
                            StatusCode = "401",
                            Message = "Authorization header is missing or invalid."
                        });
                    }
                    getUserFromToken = jwtUtils.ValidateToken(tokenFromHeader);
                }

                if (getUserFromToken == null || string.IsNullOrEmpty(getUserFromToken.Email))
                {
                    return Unauthorized(new ResponseDto
                    {
                        Status = false,
                        StatusCode = "401",
                        Message = "Invalid or expired token."
                    });
                }


                // Parse expiration date
                if (!DateTime.TryParse(getUserFromToken.TokenExpire, out var tokenExpireDate))
                {
                    return BadRequest(new ResponseDto
                    {
                        Status = false,
                        StatusCode = "400",
                        Message = "Invalid token expiration date."
                    });
                }

                // Renew token if it is close to expiration
                var remainingTime = tokenExpireDate - GeneralPurpose.DateTimeNow();
                if (remainingTime.CompareTo(TimeSpan.FromMinutes(5)) <= 0)
                {
                    var user = await userRepo.GetUserByEmail(getUserFromToken.Email);
                    if (user == null)
                    {
                        return NotFound(new ResponseDto
                        {
                            Status = false,
                            StatusCode = "404",
                            Message = GlobalMessages.LoginNotFound
                        });
                    }

                    var isCompleteValetAccount = user.Role == 4
                        ? await HandleValetAccountLogic(user)
                        : 1;

                    getUserFromToken = CreateUserClaims(user);
                    getUserFromToken.IsCompleteValetAccount = isCompleteValetAccount.ToString();
                }

                // Return the renewed token
                return Ok(new ResponseDto
                {
                    Data = getUserFromToken,
                    Status = true,
                    StatusCode = "200",
                    Message = "Token renewed successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "An error occurred while renewing the token."
                });
            }
        }

        #endregion

        #region Helpers
        private async Task<int> HandleValetAccountLogic(User user)
        {
            var isCompleteValetAccount = await GeneralPurpose.CheckValuesNotEmpty(
                user, userExperienceRepo, userSkillRepo, _payPalGateWayService, userEducationRepo);

            if (isCompleteValetAccount == 1)
            {
                var availableSlots = await userAvailableSlotRepo.GetUserAvailableSlotByUserId(user.Id);
                if (!availableSlots.Any())
                {
                    await userAvailableSlotRepo.CreateEntriesForCurrentMonth(user.Id);
                }
            }

            return isCompleteValetAccount;
        }

        private void UpdateUserProperties(UserViewModel user, User obj)
        {
            obj.FirstName = !string.IsNullOrEmpty(user.FirstName) ? user.FirstName : obj.FirstName;
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
            obj.Description = !string.IsNullOrEmpty(user.Description) ? user.Description : obj.Description;
        }

        private UserClaims CreateUserClaims(User obj)
        {
            var baseUri = $"{projectVariables.BaseUrl}/profiles/";
            return new UserClaims
            {
                Id = obj.Id,
                UserEncId = StringCipher.EncryptId((int)obj.Id),
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                UserName = obj.UserName,
                Email = obj.Email,
                Role = Enum.GetName(typeof(EnumRoles), obj.Role!),
                Token = jwtUtils.GenerateToken(obj),
                TokenExpire = GeneralPurpose.DateTimeNow().AddDays(1).ToString(),
                ProfilePicture = !string.IsNullOrEmpty(obj.ProfilePicture)
                    ? baseUri + obj.ProfilePicture
                    : "",
                Contact = obj.Contact,
                BirthDate = obj.BirthDate?.ToString(),
                Country = obj.Country,
                State = obj.State,
                City = obj.City,
                ZipCode = obj.ZipCode,
                Timezone = obj.Timezone,
                Availability = obj.Availability,
                Gender = obj.Gender,
                StripeId = obj.StripeId,
                PricePerHour = obj.PricePerHour?.ToString(),
                IsActive = Enum.GetName(typeof(EnumActiveStatus), obj.IsActive!)
            };
        }

        private async Task<string> UploadFiles(IFormFile file, string? uploadedFiles = "")
        {
            string profileImagesPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\" + uploadedFiles);
            if (!Directory.Exists(profileImagesPath))
            {
                DirectoryInfo di = Directory.CreateDirectory(profileImagesPath);
            }
            var getFileName = Path.GetFileNameWithoutExtension(file.FileName);
            if (getFileName.Contains(" "))
            {
                getFileName = getFileName.Replace(" ", "-");
            }
            var getFileExtentions = Path.GetExtension(file.FileName);
            string imgName = getFileName + "_" + DateTime.Now.Ticks.ToString() + getFileExtentions;
            var rootDir = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\" + uploadedFiles, imgName);
            using (var stream = new FileStream(rootDir, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return uploadedFiles + "/" + imgName;
        }

        private int DecryptionId(string userId)
        {
            var decrypt = StringCipher.DecryptId(userId);
            return decrypt;
        }
        #endregion
    }
}

