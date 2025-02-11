using Google.Apis.Auth;
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
        private readonly IConfiguration _config;
        private readonly GoogleAuth _googleAuth;
        private readonly ProjectVariables projectVariables;
        private readonly IPayPalGateWayService _payPalGateWayService;
        private readonly IUserExperienceRepo userExperienceRepo;
        private readonly IUserEducationRepo userEducationRepo;
        private readonly IUserSkillRepo userSkillRepo;
        private readonly IUserAvailableSlotRepo userAvailableSlotRepo;
        private readonly IHubContext<NotificationHubSocket> _notificationHubSocket;

        public AuthController(IUserRepo _userRepo, IUserEducationRepo _userEducationRepo, IUserExperienceRepo _userExperienceRepo,
            IUserSkillRepo _userSkillRepo, IPayPalGateWayService payPalGateWayService, 
            IUserAvailableSlotRepo _userAvailableSlotRepo, IJwtUtils _jwtUtils, IOptions<ProjectVariables> options,
            IHubContext<NotificationHubSocket> notificationHubSocket, IConfiguration config, IOptions<GoogleAuth> googleOptions)
        {
            userRepo = _userRepo;
            jwtUtils = _jwtUtils;
            _config = config;
            projectVariables = options.Value;
            _googleAuth = googleOptions.Value;
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
            if(user.IsActive != 1)
            {
                return BadRequest(new ResponseDto
                {
                    Status = false,
                    StatusCode = "205",
                    Message = "Email Verification Pending, you have to verify your email before login",
                    Data = "EmailVerfication"
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

        #region GoogleAuth
        [HttpPost("google/callback/{token}")]
        public async Task<IActionResult> GoogleCallback(string token)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { _googleAuth.ClientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);

                if (payload == null)
                {
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", "Invalid Google token."));
                }

                // Extract user details from token
                var user = new
                {
                    Email = payload.Email,
                    Name = payload.Name,
                    Picture = payload.Picture
                };

                // Here, you can check if the user exists in your DB, create a session, etc.

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Successfully LogenIn With Google.", user));
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage)); //"Authentication failed" });
            }
        }
        #endregion

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

            if (!GeneralPurpose.MatchPassword(user.Password!, user.ConfirmPassword!))
                return BadRequest("Password and Confirm Password must be same.");

            // Map user details
            var obj = new User();
            obj = GeneralPurpose.MapUser(user, obj);
            obj = GeneralPurpose.SetRoles(user.Role, obj);

            // Add user
            if (!await userRepo.AddUser(obj))
                return BadRequest(GlobalMessages.SystemFailureMessage);

            // Send verification email
            if (obj.Role == (int)EnumRoles.Customer || obj.Role == (int)EnumRoles.Valet)
                await MailSender.EmailAccountVerification(StringCipher.EncryptId(obj.Id), obj.UserName!,
                    obj.Email!, (int)obj.Role, projectVariables.ReactUrl);

            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Vertification Mail has been sent to your E-Mail.", obj));
        }

        #endregion

        #region Manage Profile
        [HttpPut]
        [Route("UpdateProfile/{userId}")]
        public async Task<ActionResult> PostUpdateProfile(string userId, UserViewModel user)
        {
            var decrypted = StringCipher.DecryptionId(userId);
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
                var decrypt = StringCipher.DecryptionId(userId);
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
        public async Task<ActionResult> PostUpdatePassword(string userId, UpdatePasswordDto passwordDto)
        {
            int id = StringCipher.DecryptionId(userId);

            var getLoggedInUser = await userRepo.GetUserById(id);
            if (getLoggedInUser == null)
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

            if (StringCipher.Decrypt(getLoggedInUser.Password!) != passwordDto.OldPassword)
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.OldPassword));

            if (!GeneralPurpose.MatchPassword(passwordDto.NewPassword!, passwordDto.ConfirmPassword!))
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", "Password and Confirm Password must be same."));

            getLoggedInUser.Password = StringCipher.Encrypt(passwordDto.NewPassword.Trim());

            if (!await userRepo.UpdateUser(getLoggedInUser))
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));

            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Password Updated Successfully!", getLoggedInUser));
        }
        #endregion

        #region Account
        [HttpGet("EmailVerification/{Id}")]
        public async Task<IActionResult> EmailVerification(string Id, long t)
        {
            var dt = DateTime.Now.Ticks;
            if (dt < t)
            {
                var userId = StringCipher.DecryptionId(Id);
                var obj = await userRepo.GetUserById(userId);

                if (obj == null)
                    return NotFound(GeneralPurpose.GenerateResponse( false, "400", "No User Found"));
                
                
                obj.IsActive = (int)EnumActiveStatus.AdminVerificationPending;

                if (!await userRepo.SaveChanges())
                    return BadRequest(GeneralPurpose.GenerateResponse(false, "400", GlobalMessages.SystemFailureMessage));
            }
            return Ok(GeneralPurpose.GenerateResponse(true, "200", "Email has been verified successfully, need admin aprroval", dt));
        }

        [HttpGet("ForgotPassword/{email}")]
        public async Task<IActionResult> PostForgotPassword(string email)
        {

            User obj = await userRepo.GetUserInfoByNameOrEmail(email);

            if (obj == null || obj.IsActive == 0)
                return BadRequest(GeneralPurpose.GenerateResponse(false, "400", GlobalMessages.RecordNotFound)); 

            bool chkIfMailSent = await MailSender.EmailForgetPassword(StringCipher.EncryptId(obj.Id),
                obj.UserName!, obj.Email!, 
                projectVariables.ReactUrl);
            if (!chkIfMailSent)
                return BadRequest(GeneralPurpose.GenerateResponse(false, "400", "Failed to Send Forget Password Recovery Mail"));

            return Ok(GeneralPurpose.GenerateResponse(true, "200", "Password reset email sent. If you don't see it within an hour, please contact support.\r\n"));
        }

        [HttpPost("PostRenewPassword")]
        public async Task<IActionResult> PostRenewPassword(ResetPasswordDto passwordDto)
        {
            var dt = GeneralPurpose.DateTimeNow().Ticks;
            if (dt < passwordDto.Validity)
            {
                User? obj = await userRepo.GetUserById(StringCipher.DecryptionId(passwordDto.Id));

                if (obj == null)
                {
                    return BadRequest(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.RecordNotFound });
                }

                if (passwordDto.NewPassword != passwordDto.ConfirmPassword)
                {
                    return BadRequest(new ResponseDto() { Status = false, StatusCode = "404", Message = GlobalMessages.PasswordNotMatched });
                }

                obj.Password = StringCipher.Encrypt(passwordDto.NewPassword);

                if (!await userRepo.SaveChanges())
                {
                    return BadRequest(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
                }
                return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Password Updated Successfully" });
            }
            return BadRequest(new ResponseDto() { Status = false, StatusCode = "400", Message = "Reset link has expired or is invalid, please request forgot link again!" });
        }

        #endregion

        #region UserActivityStatus
        [HttpPut("user-activity-status/{userId}")]
        public async Task<IActionResult> UpdateUserAccountActivityStatus(string userId, string activityStatus)
        {
            var decryptId = StringCipher.DecryptionId(userId);
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
            var decryptId = StringCipher.DecryptionId(userId);
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

        #region Emails
        [HttpPost("ResendVerificationEmail/{email}")]
        public async Task<ActionResult<ResponseDto>> ResendVerificationEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new ResponseDto
                {
                    Status = false,
                    StatusCode = "400",
                    Message = GlobalMessages.InvalidEmail // Updated for clarity.
                });
            }

            // Try to find user by username or email
            var getUser = await userRepo.GetUserInfoByNameOrEmail(email);

            if (getUser == null)
            {
                return BadRequest(new ResponseDto
                {
                    Status = false,
                    StatusCode = "400",
                    Message = GlobalMessages.InvalidEmail
                });
            }

            // Send verification email
            await MailSender.EmailAccountVerification(
                StringCipher.EncryptId(getUser.Id),
                getUser.UserName ?? string.Empty,
                getUser.Email ?? string.Empty,
                (int)getUser.Role,
                projectVariables.ReactUrl
            );

            return Ok(new ResponseDto
            {
                Status = true,
                StatusCode = "200",
                Message = "Verification email has been sent.",
            });
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
            var baseUri = $"{projectVariables.BaseUrl}";
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
        #endregion
    }
}

