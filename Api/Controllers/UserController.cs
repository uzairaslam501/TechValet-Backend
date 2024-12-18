using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using System.Text;
using ITValet.NotificationHub;
using Microsoft.AspNetCore.SignalR;
using System.Net;
using ITValet.ViewModel;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    [CustomAuthorize]
    [LogApiRequestResponseFilter]
    public class UserController : ControllerBase
    {
        private readonly IUserRepo userRepo;
        private readonly IUserEducationRepo userEducationRepo;
        private readonly IUserSocialProfileRepo userSocialProfileRepo;
        private readonly IUserExperienceRepo userExperienceRepo;
        private readonly IUserSkillRepo userSkillRepo;
        private readonly IUserTagRepo userTagRepo;
        private readonly IUserAvailableSlotRepo userAvailableSlotRepo;
        private readonly ISearchLogRepo searchLogRepo;
        private readonly IRequestServiceRepo requestServiceRepo;
        private readonly IOrderRepo orderRepo;
        private readonly IOrderReasonRepo orderReasonRepo;
        private readonly IJwtUtils jwtUtils;
        private readonly ProjectVariables projectVariables;
        private readonly INotificationService _packageService;
        private readonly IUserRatingRepo ratingRepo;
        private readonly IMessagesRepo MessageRepo;
        private readonly ISearchLogService _searchLogService;
        private readonly IPayPalGateWayService _payPalGateWayService;
        private readonly ILogger<UserController> _logger;
        private readonly IHubContext<NotificationHubSocket> _notificationHubSocket;
        public UserController(IHubContext<NotificationHubSocket> notificationHubSocket, IUserRepo _userRepo, IJwtUtils _jwtUtils, IOptions<ProjectVariables> options,
            IUserEducationRepo _userEducationRepo, IUserSocialProfileRepo _userSocialProfileRepo,
            IUserExperienceRepo _userExperienceRepo, IPayPalGateWayService payPalGateWayService, IUserSkillRepo _userSkillRepo, IUserTagRepo _userTagRepo,
            IUserAvailableSlotRepo _userAvailableSlotRepo, ISearchLogRepo _searchLogRepo,
            IRequestServiceRepo _requestServiceRepo, IOrderRepo _orderRepo, IOrderReasonRepo _orderReasonRepo,
            INotificationService userPackageService, ILogger<UserController> logger, IMessagesRepo messageRepo, IUserRatingRepo _ratingRepo, ISearchLogService searchLogService)
        {
            _notificationHubSocket = notificationHubSocket;
            userRepo = _userRepo;
            jwtUtils = _jwtUtils;
            ratingRepo = _ratingRepo;
            projectVariables = options.Value;
            userEducationRepo = _userEducationRepo;
            userSocialProfileRepo = _userSocialProfileRepo;
            userExperienceRepo = _userExperienceRepo;
            userSkillRepo = _userSkillRepo;
            userTagRepo = _userTagRepo;
            userAvailableSlotRepo = _userAvailableSlotRepo;
            searchLogRepo = _searchLogRepo;
            requestServiceRepo = _requestServiceRepo;
            orderRepo = _orderRepo;
            _packageService = userPackageService;
            orderReasonRepo = _orderReasonRepo;
            MessageRepo = messageRepo;
            _searchLogService = searchLogService;
            _payPalGateWayService = payPalGateWayService;
            _logger = logger;
        }

        #region User
        [HttpPut("PostUpdateUser")]
        public async Task<IActionResult> PostUpdateUser(PostUpdateUserDto user)
        {
            try
            {
                if (!string.IsNullOrEmpty(user.UserEncId))
                {
                    user.Id = StringCipher.DecryptId(user.UserEncId);
                }

                User? obj = await userRepo.GetUserById((int)user.Id);

                if (obj == null)
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = "No record found." });
                }

                obj.FirstName = !string.IsNullOrEmpty(user.FirstName) ? user.FirstName : obj.FirstName;
                obj.LastName = !string.IsNullOrEmpty(user.LastName) ? user.LastName : obj.LastName;
                obj.Contact = !string.IsNullOrEmpty(user.Contact) ? user.Contact : obj.Contact;
                obj.BirthDate = !string.IsNullOrEmpty(user.BirthDate) ? Convert.ToDateTime(user.BirthDate) : obj.BirthDate;
                obj.Country = !string.IsNullOrEmpty(user.Country) ? user.Country : obj.Country;
                obj.State = !string.IsNullOrEmpty(user.State) ? user.State : obj.State;
                obj.City = !string.IsNullOrEmpty(user.City) ? user.City : obj.City;
                obj.ZipCode = !string.IsNullOrEmpty(user.ZipCode) ? user.ZipCode : obj.ZipCode;
                obj.Timezone = !string.IsNullOrEmpty(user.Timezone) ? user.Timezone : obj.Timezone;
                obj.Status = !string.IsNullOrEmpty(user.Status) ? Convert.ToInt32(user.Status) : obj.Status;
                obj.Gender = !string.IsNullOrEmpty(user.Gender) ? user.Gender : obj.Gender;
                obj.PricePerHour = !string.IsNullOrEmpty(user.PricePerHour) ? Convert.ToDecimal(user.PricePerHour) : obj.PricePerHour;
                obj.Language = !string.IsNullOrEmpty(user.Language) ? user.Language : obj.Language;
                obj.Description = !string.IsNullOrEmpty(user.Description) ? user.Description : obj.Description;

                if (!await userRepo.UpdateUserWithoutSavingInDatabase(obj))
                {
                    return Ok(new ResponseDto() { Data = obj, Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
                }
                if (!await userRepo.SaveChanges())
                {
                    return Ok(new ResponseDto() { Data = obj, Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
                }

                int IsCompleteValetAccount = 1;
                if (obj.Role == 4)
                {
                    IsCompleteValetAccount = await GeneralPurpose.CheckValuesNotEmpty(obj, userExperienceRepo, userSkillRepo, _payPalGateWayService, userEducationRepo);
                    if(IsCompleteValetAccount == 1)
                    {
                        if (await GetUserSlotByUserId((int)user.Id) <= 0)
                        {
                            await userAvailableSlotRepo.CreateEntriesForCurrentMonth(user.Id);
                        }
                    }
                }

                UserListDto UserDto = new UserListDto()
                {
                    Id = obj.Id,
                    UserEncId = StringCipher.EncryptId(obj.Id),
                    FirstName = obj.FirstName,
                    LastName = obj.LastName,
                    UserName = obj.UserName,
                    Contact = obj.Contact,
                    Email = obj.Email,
                    Password = StringCipher.Decrypt(obj.Password),
                    Gender = obj.Gender,
                    ProfilePicture = obj.ProfilePicture != null ? projectVariables.BaseUrl + obj.ProfilePicture : null,
                    Country = obj.Country,
                    State = obj.State,
                    City = obj.City,
                    ZipCode = obj.ZipCode,
                    Timezone = obj.Timezone,
                    Availability = obj.Availability.ToString(),
                    Status = obj.Status.ToString(),
                    BirthDate = user.BirthDate.ToString(),
                    Role = Enum.GetName(typeof(EnumRoles), obj.Role),
                    IsActive = Enum.GetName(typeof(EnumActiveStatus), obj.IsActive),
                    Language = obj.Language,
                    Description = obj.Description,
                    StripeId = obj.StripeId,
                    IsVerify_StripeAccount = obj.IsVerify_StripeAccount,
                    IsBankAccountAdded = obj.IsBankAccountAdded,
                    IsCompleteValetAccount = IsCompleteValetAccount.ToString()
                };

                //obj.IsCompleteValetAccount
                return Ok(new ResponseDto() { Data = UserDto, Status = true, StatusCode = "200", Message = GlobalMessages.UpdateMessage });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto() { Status = true, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpGet("GetUserListAsync")]
        public async Task<IActionResult> GetUserListAsync()
        {
            try
            {
                var userList = await userRepo.GetOnlyActiveUserList(4);

                userList = userList.Take(5).ToList();
                List<UserListDto> udto = new List<UserListDto>();

                foreach (var user in userList)
                {
                    var getUserRatings = await ratingRepo.GetUserRatingStarsListByUserId(user.Id);
                    var AverageUserRating = 0.0;

                    if (getUserRatings != null)
                    {
                        AverageUserRating = GeneralPurpose.CalculateUserRatingPercentage(getUserRatings);

                    }
                    UserListDto obj = new UserListDto()
                    {
                        Id = user.Id,
                        UserEncId = StringCipher.EncryptId(user.Id),
                        UserName = user.UserName,
                        Email = user.Email,
                        ProfilePicture = user.ProfilePicture != null ? projectVariables.BaseUrl + user.ProfilePicture : null,
                        Status = user.Status.ToString(),
                        Description = user.Description,
                        Country = user.Country,
                        State = user.State,
                        PricePerHour = user.PricePerHour.ToString(),
                        City = user.City,
                        UserRating = AverageUserRating.ToString(),
                        UserRatingCount = getUserRatings?.Count.ToString()
                    };

                    udto.Add(obj);
                }
                return Ok(new ResponseDto() { Data = udto, Status = true, StatusCode = "200", Message = "User list retrieved successfully." });

            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message.ToString());
                return Ok(new ResponseDto() { Status = false, StatusCode = "200", Message = "Something went wrong!" });
            }
        }

        [HttpGet("GetItValetsByRequestSkills")]
        public async Task<IActionResult> GetItValetsByRequestSkills(string requestSkills)
        {
            var userDtos = new List<UserListDto>();

            // Fetch users matching the requested skills
            var usersWithMatchingSkills = await userRepo.GetUsersListByRequestSkills(requestSkills);

            if (usersWithMatchingSkills != null && usersWithMatchingSkills.Any())
            {
                foreach (var user in usersWithMatchingSkills)
                {
                    // Fetch user ratings
                    var userRatings = await ratingRepo.GetUserRatingStarsListByUserId(user.Id);
                    var averageRating = userRatings != null
                        ? GeneralPurpose.CalculateUserRatingPercentage(userRatings)
                        : 0.0;

                    // Build the DTO
                    userDtos.Add(new UserListDto
                    {
                        Id = user.Id,
                        UserEncId = StringCipher.EncryptId(user.Id),
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        UserName = user.UserName,
                        Contact = user.Contact,
                        Email = user.Email,
                        City = user.City,
                        ZipCode = user.ZipCode,
                        Description = user.Description,
                        PricePerHour = user.PricePerHour.ToString(),
                        ProfilePicture = $"{projectVariables.BaseUrl}{user.ProfilePicture}",
                        UserRating = averageRating.ToString("F2"), // Format rating to 2 decimal places
                        UserRatingCount = userRatings?.Count.ToString() ?? "0"
                    });
                }
            }
            else
            {
                // Fallback: If no users match skills, fetch general user list
                var userResponse = await GetUserListAsync();

                if (userResponse is OkObjectResult responseResult && 
                    responseResult.Value is ResponseDto responseDto &&
                    responseDto.Status == true)
                {
                    return Ok(new ResponseDto
                    {
                        Status = true,
                        StatusCode = "200",
                        Message = "users found.",
                        Data = responseDto.Data
                    });
                }

                return NotFound(new ResponseDto
                {
                    Status = false,
                    StatusCode = "404",
                    Message = "No users found."
                });
            }

            return Ok(new ResponseDto {
                Status = true,
                StatusCode = "200",
                Message = "users found.",
                Data = userDtos
            });
        }


        [HttpDelete("delete-stripe-account/{userId}")]
        public async Task<IActionResult> DeleteStripeAccount(string userId)
        {
            try
            {
                var decrypt = DecryptionId(userId);
                var userObj = await userRepo.GetUserById(decrypt);
                userObj.StripeId = null;
                userObj.IsVerify_StripeAccount = null;
                userObj.IsBankAccountAdded = null;

                var getUserOrderStatus = await orderRepo.getInProgressUserOrders(decrypt);
                if (getUserOrderStatus.Count() > 0)
                    return Conflict(GeneralPurpose.GenerateResponseCode(false, "409", "Can't delete this account some orders are In-Progress"));

                if (!await userRepo.UpdateUser(userObj))
                    throw new Exception(GlobalMessages.RecordNotFound);

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.UpdateMessage));
            }
            catch (Exception ex)
            {                
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", ex.Message));
            }
        }
        #endregion

        #region UserEducation
        [HttpPost("add-education/{userId}")]
        public async Task<IActionResult> PostAddUserEducation(string userId, EducationViewModel model)
        {
            var decrypt = DecryptionId(userId);
            var obj = new UserEducation();

            obj.DegreeName = model.DegreeName;
            obj.InstituteName = model.InstituteName;
            obj.StartDate = Convert.ToDateTime(model.StartDate);
            obj.EndDate = Convert.ToDateTime(model.EndDate);
            obj.UserId = decrypt;
            obj.IsActive = 1;
            obj.CreatedAt = GeneralPurpose.DateTimeNow();

            if (!await userEducationRepo.AddUserEducation(obj))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Education has been added to your account" });
        }

        [HttpPut("update-education/{educationId}")]
        public async Task<IActionResult> PostUpdateUserEducation(string educationId, EducationViewModel model)
        {
            if (string.IsNullOrEmpty(educationId))
                throw new Exception(GlobalMessages.RecordNotFound);

            var decrypt = DecryptionId(educationId);
            var obj = await userEducationRepo.GetUserEducationById(decrypt);
            
            obj.DegreeName = !string.IsNullOrEmpty(model.DegreeName) ? model.DegreeName : obj.DegreeName;
            obj.InstituteName = !string.IsNullOrEmpty(model.InstituteName) ? model.InstituteName : obj.DegreeName;
            obj.StartDate = !string.IsNullOrEmpty(model.StartDate) ? Convert.ToDateTime(model.StartDate) : obj.StartDate;
            obj.EndDate = !string.IsNullOrEmpty(model.EndDate) ? Convert.ToDateTime(model.EndDate) : obj.EndDate;
            obj.UpdatedAt = GeneralPurpose.DateTimeNow();

            if (!await userEducationRepo.UpdateUserEducation(obj))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = GlobalMessages.UpdateMessage });
        }

        [HttpDelete("delete/{educationId}")]
        public async Task<IActionResult> DeleteUserEducation(string educationId)
        {
            if (string.IsNullOrEmpty(educationId))
                return NotFound(GeneralPurpose.GenerateResponseCode( false, "400", GlobalMessages.RecordNotFound));

            var decrypt = DecryptionId(educationId);
            if (!await userEducationRepo.DeleteUserEducation(decrypt))
                return NotFound(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));
                
            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.DeletedMessage));
        }

        [HttpGet("education/{educationId}")]
        public async Task<IActionResult> GetUserEducationById(string educationId)
        {
            if (string.IsNullOrEmpty(educationId))
                return NotFound(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

            var decrypt = DecryptionId(educationId);
            var obj = await userEducationRepo.GetUserEducationById(decrypt);
            if (obj != null) { 
                UserEducationDto userEducationDto = new UserEducationDto()
                {
                    Id = obj.Id,
                    UserEducationEncId = StringCipher.EncryptId(obj.Id),
                    DegreeName = obj.DegreeName,
                    InstituteName = obj.InstituteName,
                    StartDate = obj.StartDate.Value.ToString("yyyy-MM-dd"),
                    EndDate = obj.EndDate.Value.ToString("yyyy-MM-dd"),
                    UserId = obj.UserId
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Education Fetch Successfully"));
            }
            return BadRequest(GeneralPurpose.GenerateResponseCode (false, "400", GlobalMessages.RecordNotFound));
        }

        [HttpGet("education-by-userId/{userId}")]
        public async Task<IActionResult> GetUserEducationByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return NotFound(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

            var decrypt = DecryptionId(userId);

            var listOfEducation = await userEducationRepo.GetUserEducationByUserId(decrypt);
            List<UserEducationDto> dtos = new List<UserEducationDto>();
            foreach (var obj in listOfEducation)
            {
                UserEducationDto userEducationDto = new UserEducationDto()
                {
                    Id = obj.Id,
                    UserEducationEncId = StringCipher.EncryptId(obj.Id),
                    DegreeName = obj.DegreeName,
                    InstituteName = obj.InstituteName,
                    StartDate = obj.StartDate.Value.ToString("MM/dd/yyyy"),
                    EndDate = obj.EndDate.Value.ToString("MM/dd/yyyy"),
                    UserId = obj.UserId
                };
                dtos.Add(userEducationDto);
            }
            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Education Fetch Successfully", dtos));
        }
        #endregion

        #region UserExperience
        [HttpPost("add-service-experience/{userId}")]
        public async Task<IActionResult> PostAddUserExperience(string userId, string? Description)
        {
            try
            {
                var decrypt = DecryptionId(userId);
                var obj = new UserExperience();

                obj.Description = Description;
                obj.UserId = decrypt;
                obj.IsActive = 1;
                obj.CreatedAt = GeneralPurpose.DateTimeNow();

                if (!await userExperienceRepo.AddUserExperience(obj))
                    throw new Exception(GlobalMessages.SystemFailureMessage);
                
                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Experience has been added to your account", obj));
            }
            catch (Exception ex)
            {
                
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", ex.Message));
            }
        }

        [HttpPut("update-service-experience/{serviceId}")]
        public async Task<IActionResult> PostUpdateUserExperience(string serviceId, string? Description)
        {
            try
            {
                var decrypt = DecryptionId(serviceId);
                var obj = await userExperienceRepo.GetUserExperienceById(decrypt);

                obj.Description = !string.IsNullOrEmpty(Description) ? Description : obj.Description;
                obj.UpdatedAt = GeneralPurpose.DateTimeNow();

                if (!await userExperienceRepo.UpdateUserExperience(obj))
                    throw new Exception(GlobalMessages.SystemFailureMessage);

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.UpdateMessage, obj));
            }
            catch (Exception ex)
            {
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", ex.Message));
            }
        }

        [HttpGet("GetUserExperienceById/{serviceId}")]
        public async Task<IActionResult> GetUserExperienceById(string serviceId)
        {
            try
            {
                var decrypt = DecryptionId(serviceId);
                var obj = await userExperienceRepo.GetUserExperienceById(decrypt);
                var userExperienceDto = MapToUserExperienceDto(obj);

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Experience Fetch Successfully", userExperienceDto));
            }
            catch (Exception ex)
            {
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", ex.Message));
            }
        }

        [HttpGet("user-services/{userId}")]
        public async Task<IActionResult> GetUserExperienceByUserId(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new Exception(GlobalMessages.RecordNotFound);

                var decrypt = DecryptionId(userId);
                var listOfExperience = await userExperienceRepo.GetUserExperienceByUserId(decrypt);
                List<UserExperienceDto> dtos = new List<UserExperienceDto>();
                foreach (var obj in listOfExperience)
                {
                    dtos.Add(MapToUserExperienceDto(obj));
                }
                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Experience Fetch Successfully", dtos));
            }
            catch (Exception ex)
            {
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", ex.Message));
            }
        }

        [HttpGet("GetUserExperienceList")]
        public async Task<IActionResult> GetUserExperienceList(string? Title = "", string? Description = "", string? Organization = "")
        {
            var listOfExperience = await userExperienceRepo.GetUserExperienceList();

            if (!string.IsNullOrEmpty(Title))
            {
                listOfExperience = listOfExperience.Where(x => x.Title.ToLower().Contains(Title.ToLower())).ToList();
            }
            if (!string.IsNullOrEmpty(Description))
            {
                listOfExperience = listOfExperience.Where(x => x.Description.ToLower().Contains(Description.ToLower())).ToList();
            }
            if (!string.IsNullOrEmpty(Organization))
            {
                listOfExperience = listOfExperience.Where(x => x.Organization.ToLower().Contains(Organization.ToLower())).ToList();
            }

            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            if (sortColumn != "" && sortColumn != null)
            {
                if (sortColumn != "0")
                {
                    if (sortColumnDirection == "asc")
                    {
                        listOfExperience = listOfExperience.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                    else
                    {
                        listOfExperience = listOfExperience.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                }
            }
            int totalrows = listOfExperience.Count();

            if (!string.IsNullOrEmpty(searchValue))
            {
                listOfExperience = listOfExperience.Where(x => x.Title.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.Description != null && x.Description.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.Organization != null && x.Organization.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.Website != null && x.Website.Trim().ToLower().Contains(searchValue.Trim().ToLower())
                                    ).ToList();
            }
            int totalrowsafterfilterinig = listOfExperience.Count();

            listOfExperience = listOfExperience.Skip(skip).Take(pageSize).ToList();
            List<UserExperienceDto> dtos = new List<UserExperienceDto>();
            foreach (var obj in listOfExperience)
            {
                UserExperienceDto userExperienceDto = new UserExperienceDto()
                {
                    Id = obj.Id,
                    UserExperienceEncId = StringCipher.EncryptId(obj.Id),
                    Title = obj.Title,
                    Description = obj.Description,
                    ExperienceFrom = obj.ExperienceFrom.ToString(),
                    ExperienceTo = obj.ExperienceTo.ToString(),
                    Organization = obj.Organization,
                    Website = obj.Website,
                    UserId = obj.UserId
                };
                dtos.Add(userExperienceDto);
            }
            return Ok(new ResponseDto() { Data = new { data = dtos, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig }, Status = true, StatusCode = "200" });
        }

        [HttpDelete("delete-service/{serviceId}")]
        public async Task<IActionResult> DeleteUserExperience(string serviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(serviceId))
                    throw new Exception(GlobalMessages.RecordNotFound);

                var decrypt = DecryptionId(serviceId);
                if (!await userExperienceRepo.DeleteUserExperience(decrypt))
                    throw new Exception(GlobalMessages.SystemFailureMessage);

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.DeletedMessage, serviceId));
            }
            catch (Exception ex)
            {
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", ex.Message));
            }
        }
        #endregion

        #region UserSocialProfile
        [HttpPost("PostAddUserSocialProfile")]
        public async Task<IActionResult> PostAddUserSocialProfile(PostAddUserSocialProfile userSocialProfile)
        {
            var obj = new UserSocialProfile();

            obj.Title = userSocialProfile.Title;
            obj.Link = userSocialProfile.Link;
            obj.UserId = userSocialProfile.UserId;
            obj.IsActive = 1;
            obj.CreatedAt = GeneralPurpose.DateTimeNow();

            if (!await userSocialProfileRepo.AddUserSocialProfile(obj))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "SocialProfile has been added to your account" });
        }

        [HttpPut("PostUpdateUserSocialProfile")]
        public async Task<IActionResult> PostUpdateUserSocialProfile(PostUpdateUserSocialProfile userSocialProfile)
        {
            var obj = await userSocialProfileRepo.GetUserSocialProfileById(StringCipher.DecryptId(userSocialProfile.UserSocialProfileEncId));

            obj.Title = !string.IsNullOrEmpty(userSocialProfile.Title) ? userSocialProfile.Title : obj.Title;
            obj.Link = !string.IsNullOrEmpty(userSocialProfile.Link) ? userSocialProfile.Link : obj.Link;
            obj.UpdatedAt = GeneralPurpose.DateTimeNow();

            if (!await userSocialProfileRepo.AddUserSocialProfile(obj))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "SocialProfile has been added to your account" });
        }

        [HttpGet("GetUserSocialProfileById")]
        public async Task<IActionResult> GetUserSocialProfileById(string userSocialProfileId)
        {
            var obj = await userSocialProfileRepo.GetUserSocialProfileById(StringCipher.DecryptId(userSocialProfileId));
            UserSocialProfileDto userSocialProfileDto = new UserSocialProfileDto()
            {
                Id = obj.Id,
                UserSocialProfileEncId = StringCipher.EncryptId(obj.Id),
                Title = obj.Title,
                Link = obj.Link,
                UserId = obj.UserId
            };

            return Ok(new ResponseDto() { Data = userSocialProfileDto, Status = true, StatusCode = "200", Message = "SocialProfile Fetch Successfully" });
        }

        [HttpGet("GetUserSocialProfileByUserId")]
        public async Task<IActionResult> GetUserSocialProfileByUserId(string userId)
        {
            var listOfSocialProfile = await userSocialProfileRepo.GetUserSocialProfileByUserId(StringCipher.DecryptId(userId));
            List<UserSocialProfileDto> dtos = new List<UserSocialProfileDto>();
            foreach (var obj in listOfSocialProfile)
            {
                UserSocialProfileDto userSocialProfileDto = new UserSocialProfileDto()
                {
                    Id = obj.Id,
                    UserSocialProfileEncId = StringCipher.EncryptId(obj.Id),
                    Title = obj.Title,
                    Link = obj.Link,
                    UserId = obj.UserId
                };
                dtos.Add(userSocialProfileDto);
            }

            return Ok(new ResponseDto() { Data = dtos, Status = true, StatusCode = "200", Message = "SocialProfile Fetch Successfully" });
        }

        [HttpGet("GetUserSocialProfileList")]
        public async Task<IActionResult> GetUserSocialProfileList(string? Title = "", string? Link = "", string? Organization = "")
        {
            var listOfSocialProfile = await userSocialProfileRepo.GetUserSocialProfileList();

            if (!string.IsNullOrEmpty(Title))
            {
                listOfSocialProfile = listOfSocialProfile.Where(x => x.Title.ToLower().Contains(Title.ToLower())).ToList();
            }
            if (!string.IsNullOrEmpty(Link))
            {
                listOfSocialProfile = listOfSocialProfile.Where(x => x.Link.ToLower().Contains(Link.ToLower())).ToList();
            }

            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            if (sortColumn != "" && sortColumn != null)
            {
                if (sortColumn != "0")
                {
                    if (sortColumnDirection == "asc")
                    {
                        listOfSocialProfile = listOfSocialProfile.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                    else
                    {
                        listOfSocialProfile = listOfSocialProfile.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                }
            }
            int totalrows = listOfSocialProfile.Count();

            if (!string.IsNullOrEmpty(searchValue))
            {
                listOfSocialProfile = listOfSocialProfile.Where(x => x.Title.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.Link != null && x.Link.Trim().ToLower().Contains(searchValue.Trim().ToLower())
                                    ).ToList();
            }
            int totalrowsafterfilterinig = listOfSocialProfile.Count();

            listOfSocialProfile = listOfSocialProfile.Skip(skip).Take(pageSize).ToList();
            List<UserSocialProfileDto> dtos = new List<UserSocialProfileDto>();
            foreach (var obj in listOfSocialProfile)
            {
                UserSocialProfileDto userSocialProfileDto = new UserSocialProfileDto()
                {
                    Id = obj.Id,
                    UserSocialProfileEncId = StringCipher.EncryptId(obj.Id),
                    Title = obj.Title,
                    Link = obj.Link,
                    UserId = obj.UserId
                };
                dtos.Add(userSocialProfileDto);
            }
            return Ok(new ResponseDto() { Data = new { data = dtos, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig }, Status = true, StatusCode = "200" });
        }
        #endregion

        #region UserSkill
        [HttpPost("PostAddSkills/{userId}")]
        public async Task<IActionResult> PostAddUserSkill(string userId, string? skillsName)
        {
            if (string.IsNullOrEmpty(skillsName))
                return BadRequest(new ResponseDto { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });

            // Add new skills.
            var skillsArray = skillsName?.Split(",") ?? Array.Empty<string>();
            foreach (var skill in skillsArray)
            {
                if (!await userSkillRepo.AddUserSkillAsync(userId, skill))
                    return BadRequest(new ResponseDto { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }

            if (!await userSkillRepo.SaveChangesAsync())
                return BadRequest(new ResponseDto { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            
            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Skills have been added to your account.", skillsName));
        }

        [HttpGet("GetSkill/{userSkillId}")]
        public async Task<IActionResult> GetUserSkillById(string userSkillId)
        {
            var obj = await userSkillRepo.GetUserSkillByIdAsync(userSkillId);
            var userSkill = MapToUserSkillDto(obj);

            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Skill fetch successfully.", userSkill));
        }

        [HttpGet("GetSkills/{userId}")]
        public async Task<IActionResult> GetSkills(string userId)
        {
            userId = GeneralPurpose.ConversionEncryptedId(userId);

            var decrypt = DecryptionId(userId);
            var listOfSkill = await userSkillRepo.GetUserSkillsByUserIdAsync(decrypt);
            
            var userSkills = listOfSkill.Select(skill => MapToUserSkillDto(skill)).ToList();

            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Skills fetched successfully.", userSkills));
        }

        [HttpDelete("Delete/{skillId}")]
        public async Task<IActionResult> DeleteUserSkillByUserId(string skillId)
        {
            if (!await userSkillRepo.SoftDeleteUserSkillAsync(skillId) ||
                !await userSkillRepo.SaveChangesAsync())
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));
            
            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Skill deleted successfully.", skillId));
        }
        #endregion

        #region UserTag
        [HttpPost("PostAddUserTag")]
        public async Task<IActionResult> PostAddUserTag(PostAddUserTag userTag)
        {
            var obj = new UserTag();

            obj.TagName = userTag.TagName;
            obj.UserId = userTag.UserId;
            obj.IsActive = 1;
            obj.CreatedAt = GeneralPurpose.DateTimeNow();

            if (!await userTagRepo.AddUserTag(obj))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Tag has been added to your account" });
        }

        [HttpPut("PostUpdateUserTag")]
        public async Task<IActionResult> PostUpdateUserTag(PostUpdateUserTag userTag)
        {
            var obj = await userTagRepo.GetUserTagById(StringCipher.DecryptId(userTag.UserTagEncId));

            obj.TagName = !string.IsNullOrEmpty(userTag.TagName) ? userTag.TagName : obj.TagName;
            obj.UpdatedAt = GeneralPurpose.DateTimeNow();

            if (!await userTagRepo.AddUserTag(obj))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Tag has been added to your account" });
        }

        [HttpGet("GetUserTagById")]
        public async Task<IActionResult> GetUserTagById(string userTagId)
        {
            var obj = await userTagRepo.GetUserTagById(StringCipher.DecryptId(userTagId));
            UserTagDto userTagDto = new UserTagDto()
            {
                Id = obj.Id,
                UserTagEncId = StringCipher.EncryptId(obj.Id),
                TagName = obj.TagName,
                UserId = obj.UserId
            };

            return Ok(new ResponseDto() { Data = userTagDto, Status = true, StatusCode = "200", Message = "Tag Fetch Successfully" });
        }

        [HttpGet("GetUserTagByUserId")]
        public async Task<IActionResult> GetUserTagByUserId(string userId)
        {
            var listOfTag = await userTagRepo.GetUserTagByUserId(StringCipher.DecryptId(userId));
            List<UserTagDto> dtos = new List<UserTagDto>();
            foreach (var obj in listOfTag)
            {
                UserTagDto userTagDto = new UserTagDto()
                {
                    Id = obj.Id,
                    UserTagEncId = StringCipher.EncryptId(obj.Id),
                    TagName = obj.TagName,
                    UserId = obj.UserId
                };
                dtos.Add(userTagDto);
            }

            return Ok(new ResponseDto() { Data = dtos, Status = true, StatusCode = "200", Message = "Tag Fetch Successfully" });
        }

        [HttpGet("GetUserTagList")]
        public async Task<IActionResult> GetUserTagList(string? TagName = "")
        {
            var listOfTag = await userTagRepo.GetUserTagList();

            if (!string.IsNullOrEmpty(TagName))
            {
                listOfTag = listOfTag.Where(x => x.TagName.ToLower().Contains(TagName.ToLower())).ToList();
            }

            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            if (sortColumn != "" && sortColumn != null)
            {
                if (sortColumn != "0")
                {
                    if (sortColumnDirection == "asc")
                    {
                        listOfTag = listOfTag.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                    else
                    {
                        listOfTag = listOfTag.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                }
            }
            int totalrows = listOfTag.Count();

            if (!string.IsNullOrEmpty(searchValue))
            {
                listOfTag = listOfTag.Where(x => x.TagName.Trim().ToLower().Contains(searchValue.Trim().ToLower())
                                    ).ToList();
            }
            int totalrowsafterfilterinig = listOfTag.Count();

            listOfTag = listOfTag.Skip(skip).Take(pageSize).ToList();
            List<UserTagDto> dtos = new List<UserTagDto>();
            foreach (var obj in listOfTag)
            {
                UserTagDto userTagDto = new UserTagDto()
                {
                    Id = obj.Id,
                    UserTagEncId = StringCipher.EncryptId(obj.Id),
                    TagName = obj.TagName,
                    UserId = obj.UserId
                };
                dtos.Add(userTagDto);
            }
            return Ok(new ResponseDto() { Data = new { data = dtos, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig }, Status = true, StatusCode = "200" });
        }
        #endregion

        #region UserAvailableSlot
        private async Task<bool> PostAddUserAvailableSlot(PostAddUserAvailableSlot userAvailableSlot)
        {
            var obj = new UserAvailableSlot();

            obj.DateTimeOfDay = Convert.ToDateTime(userAvailableSlot.DateTimeOfDay);
            obj.Slot1 = Convert.ToInt32(userAvailableSlot.Slot1);
            obj.Slot2 = Convert.ToInt32(userAvailableSlot.Slot2);
            obj.Slot3 = Convert.ToInt32(userAvailableSlot.Slot3);
            obj.Slot4 = Convert.ToInt32(userAvailableSlot.Slot4);
            obj.UserId = userAvailableSlot.UserId;
            obj.IsActive = 1;
            obj.CreatedAt = GeneralPurpose.DateTimeNow();

            if (!await userAvailableSlotRepo.AddUserAvailableSlot(obj))
            {
                return false;
            }
            return true;
        }

        private async Task<bool> DeleteUserAvailableSlots(int UserId)
        {
            var listOfAvailableSlot = await userAvailableSlotRepo.GetUserAvailableSlotByUserId(UserId);

            if (listOfAvailableSlot.Count() > 0)
            {
                foreach (var item in listOfAvailableSlot)
                {
                    await userAvailableSlotRepo.DeleteUserAvailableSlotWithoutSavingDatabase(item.Id);
                }
            }
            return true;
        }


        [HttpPost("PostAddUserAvailableSlots")]
        public async Task<bool> PostAddUserAvailableSlots(List<PostAddUserAvailableSlot> userAvailableSlots)
        {
            string todayDay = GeneralPurpose.DateTimeNow().DayOfWeek.ToString();

            foreach (var item in userAvailableSlots)
            {
                var obj = new UserAvailableSlot(); // Create a new instance for each item

                item.DateTimeOfDay = (item.DateTimeOfDay == todayDay)
                    ? GeneralPurpose.DateTimeNow().ToString()
                    : GeneralPurpose.GetNextOccurrenceOfDay(item.DateTimeOfDay).ToString();

                obj.DateTimeOfDay = Convert.ToDateTime(item.DateTimeOfDay);
                obj.Slot1 = Convert.ToInt32(item.Slot1);
                obj.Slot2 = Convert.ToInt32(item.Slot2);
                obj.Slot3 = Convert.ToInt32(item.Slot3);
                obj.Slot4 = Convert.ToInt32(item.Slot4);
                obj.UserId = item.UserId;
                obj.IsActive = 1;
                obj.CreatedAt = GeneralPurpose.DateTimeNow();

                if (!await userAvailableSlotRepo.AddUserAvailableSlot(obj))
                {
                    return false;
                }
            }
            return true;
        }

        [HttpPut("PostUpdateUserAvailableSlot")]
        public async Task<IActionResult> PostUpdateUserAvailableSlot(PostUpdateUserAvailableSlot userAvailableSlot)
        {
            var obj = await userAvailableSlotRepo.GetUserAvailableSlotById(StringCipher.DecryptId(userAvailableSlot.UserAvailableSlotEncId));
            obj.Slot1 = !string.IsNullOrEmpty(userAvailableSlot.Slot1) ? Convert.ToInt32(userAvailableSlot.Slot1) : obj.Slot1;
            obj.Slot2 = !string.IsNullOrEmpty(userAvailableSlot.Slot2) ? Convert.ToInt32(userAvailableSlot.Slot2) : obj.Slot2;
            obj.Slot3 = !string.IsNullOrEmpty(userAvailableSlot.Slot3) ? Convert.ToInt32(userAvailableSlot.Slot3) : obj.Slot3;
            obj.Slot4 = !string.IsNullOrEmpty(userAvailableSlot.Slot4) ? Convert.ToInt32(userAvailableSlot.Slot4) : obj.Slot4;
            obj.UpdatedAt = GeneralPurpose.DateTimeNow();
            if (!await userAvailableSlotRepo.UpdateUserAvailableSlot(obj))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = GlobalMessages.UpdateMessage });
        }
        
        private async Task<int> GetUserSlotByUserId(int userId)
        {
            var obj = await userAvailableSlotRepo.GetUserAvailableSlotByUserId(userId);
            return obj.Count();
        }

        [HttpGet("GetUserAvailableSlotById")]
        public async Task<IActionResult> GetUserAvailableSlotById(string userAvailableSlotId)
        {
            var obj = await userAvailableSlotRepo.GetUserAvailableSlotById(StringCipher.DecryptId(userAvailableSlotId));
            UserAvailableSlotDto userAvailableSlotDto = new UserAvailableSlotDto()
            {
                Id = obj.Id,
                UserAvailableSlotEncId = StringCipher.EncryptId(obj.Id),
                DateTimeOfDay = obj.DateTimeOfDay.ToString(),
                Slot1 = obj.Slot1.ToString(),
                Slot2 = obj.Slot2.ToString(),
                Slot3 = obj.Slot3.ToString(),
                Slot4 = obj.Slot4.ToString(),
                UserId = obj.UserId
            };
            return Ok(new ResponseDto() { Data = userAvailableSlotDto, Status = true, StatusCode = "200", Message = "AvailableSlot Fetch Successfully" });
        }
        
        [HttpGet("GetUserAvailableSlotByUserId")]
        public async Task<IActionResult> GetUserAvailableSlotByUserId(string userId)
        {
            int id = Convert.ToInt32(userId);
            var listOfAvailableSlot = await userAvailableSlotRepo.GetUserAvailableSlotByUserId(id);
            List<UserAvailableSlotDto> dtos = new List<UserAvailableSlotDto>();

            foreach (var obj in listOfAvailableSlot)
            {
                DateTime? dateTimeOfDay = obj.DateTimeOfDay;
                string dayName = dateTimeOfDay.HasValue ? dateTimeOfDay.Value.DayOfWeek.ToString() : string.Empty;

                UserAvailableSlotDto userAvailableSlotDto = new UserAvailableSlotDto()
                {
                    Id = obj.Id,
                    UserAvailableSlotEncId = StringCipher.EncryptId(obj.Id),
                    DateTimeOfDay = dateTimeOfDay.Value.ToString("d"),
                    DayName = dayName, // Extract day name
                    Slot1 = obj.Slot1.ToString(),
                    Slot2 = obj.Slot2.ToString(),
                    Slot3 = obj.Slot3.ToString(),
                    Slot4 = obj.Slot4.ToString(),
                    UserId = obj.UserId
                };
                dtos.Add(userAvailableSlotDto);
            }

            return Ok(new ResponseDto() { Data = dtos, Status = true, StatusCode = "200", Message = "AvailableSlot Fetch Successfully" });
        }

        [HttpGet("GetUserAvailableSlotList")]
        public async Task<IActionResult> GetUserAvailableSlotList(string? DateTimeOfDay = "", string? Slot1 = "", string? Slot4 = "")
        {
            var listOfAvailableSlot = await userAvailableSlotRepo.GetUserAvailableSlotList();


            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            if (sortColumn != "" && sortColumn != null)
            {
                if (sortColumn != "0")
                {
                    if (sortColumnDirection == "asc")
                    {
                        listOfAvailableSlot = listOfAvailableSlot.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                    else
                    {
                        listOfAvailableSlot = listOfAvailableSlot.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                }
            }
            int totalrows = listOfAvailableSlot.Count();

            if (!string.IsNullOrEmpty(searchValue))
            {
                listOfAvailableSlot = listOfAvailableSlot.ToList();
            }
            int totalrowsafterfilterinig = listOfAvailableSlot.Count();

            listOfAvailableSlot = listOfAvailableSlot.Skip(skip).Take(pageSize).ToList();
            List<UserAvailableSlotDto> dtos = new List<UserAvailableSlotDto>();
            foreach (var obj in listOfAvailableSlot)
            {
                UserAvailableSlotDto userAvailableSlotDto = new UserAvailableSlotDto()
                {
                    Id = obj.Id,
                    UserAvailableSlotEncId = StringCipher.EncryptId(obj.Id),
                    DateTimeOfDay = obj.DateTimeOfDay.ToString(),
                    Slot1 = obj.Slot1.ToString(),
                    Slot2 = obj.Slot2.ToString(),
                    Slot3 = obj.Slot3.ToString(),
                    Slot4 = obj.Slot4.ToString(),
                    UserId = obj.UserId
                };
                dtos.Add(userAvailableSlotDto);
            }
            return Ok(new ResponseDto() { Data = new { data = dtos, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig }, Status = true, StatusCode = "200" });
        }
        #endregion

        #region SearchLog
        [HttpPost("PostAddSearchLog")]
        public async Task<IActionResult> PostAddSearchLog(PostAddSearchLog SearchLog)
        {
            var obj = new SearchLog();
            obj.SearchKeyword = SearchLog.SearchKeyword.Trim();
            var getSearchLogs = await searchLogRepo.GetSearchLogListBySearchingWord(obj.SearchKeyword);
            if(getSearchLogs != null)
            {
                getSearchLogs.SearchKeywordCount = getSearchLogs.SearchKeywordCount + 1;
                getSearchLogs.UpdatedAt = GeneralPurpose.DateTimeNow();
                if (!await updateSearchLog(getSearchLogs))
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
                }
                return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Search updation is successfull" });
            }
            else
            {
                obj.SearchKeywordCount = 1;
                obj.IsActive = 1;
                obj.CreatedAt = GeneralPurpose.DateTimeNow();

                if (!await searchLogRepo.AddSearchLog(obj))
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
                }
                return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "AvailableSlot has been added to your account" });
            }
        }

        private async Task<bool> updateSearchLog(SearchLog searchLog)
        {
            if (!await searchLogRepo.UpdateSearchLog(searchLog))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region RequestServices
        [HttpDelete("DeleteRequestService")]
        public async Task<IActionResult> DeleteRequestService(string requestServiceId)
        {
            if (string.IsNullOrEmpty(requestServiceId))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.RecordNotFound });
            }

            if (!await requestServiceRepo.DeleteRequestService(Convert.ToInt32(requestServiceId)))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = GlobalMessages.DeletedMessage });
        }

        [HttpGet("GetRequestServiceById")]
        public async Task<IActionResult> GetRequestServiceById(string requestServiceId)
        {
            if (requestServiceId.Contains(" "))
            {
                requestServiceId.Replace(" ", "+");
            }
            var obj = await requestServiceRepo.GetRequestServiceById(StringCipher.DecryptId(requestServiceId));
            if (obj == null)
            {
                return Ok(new ResponseDto() { Status = true, StatusCode = "400", Message = GlobalMessages.RecordNotFound });
            }
            RequestServicesDto service = new RequestServicesDto()
            {
                Id = obj.Id,
                ServiceTitle = obj.ServiceTitle,
                PrefferedServiceTime = obj.PrefferedServiceTime,
                CategoriesOfProblems = obj.CategoriesOfProblems,
                ServiceDescription = obj.ServiceDescription,
                FromDateTime = obj.FromDateTime.Value.ToString("yyyy-MM-dd HH:mm tt"),
                ToDateTime = obj.ToDateTime.Value.ToString("yyyy-MM-dd HH:mm tt"),
                RequestServiceSkills = obj.RequestServiceSkills,
                ServiceLanguage = obj.ServiceLanguage,
                RequestedServiceUserId = obj.RequestedServiceUserId.ToString(),
                RequestServiceType = obj.RequestServiceType.ToString(),
            };

            return Ok(new ResponseDto() { Data = service, Status = true, StatusCode = "200" });
        }

        /*[HttpPost("GetRequestServicesDatatableByUserId")]
        public async Task<IActionResult> GetRequestServicesDatatableByUserId(string? Name = "", string? SkillName = "")
        {
            try
            {
                UserClaims? getUsetFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
                var ulist = await requestServiceRepo.GetRequestServiceByUserId((int)getUsetFromToken.Id);

                if (!string.IsNullOrEmpty(Name))
                {
                    ulist = ulist.Where(x => x.ServiceTitle.ToLower().Contains(Name.ToLower())).ToList();
                }
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                if (sortColumn != "" && sortColumn != null)
                {
                    if (sortColumn != "0")
                    {
                        if (sortColumnDirection == "asc")
                        {
                            ulist = ulist.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                        }
                        else
                        {
                            ulist = ulist.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                        }
                    }
                }
                int totalrows = ulist.Count();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    ulist = ulist.Where(x => x.ServiceTitle.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                        x.ServiceDescription != null && x.ServiceDescription.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                        x.ServiceLanguage != null && x.ServiceLanguage.Trim().ToLower().Contains(searchValue.Trim().ToLower())
                                        ).ToList();
                }
                int totalrowsafterfilterinig = ulist.Count();

                ulist = ulist.Skip(skip).Take(pageSize).ToList();
                List<RequestServicesDto> udto = new List<RequestServicesDto>();
                foreach (RequestService u in ulist)
                {
                    RequestServicesDto obj = new RequestServicesDto()
                    {
                        Id = u.Id,
                        EncId = StringCipher.EncryptId(u.Id),
                        PrefferedServiceTime = u.PrefferedServiceTime,
                        CategoriesOfProblems = u.CategoriesOfProblems,
                        ServiceDescription = u.ServiceDescription,
                        FromDateTime = u.FromDateTime.ToString(),
                        ToDateTime = u.ToDateTime.ToString(),
                        AppointmentTime = u.FromDateTime.ToString() + " - " + u.ToDateTime.ToString(),
                        ServiceLanguage = u.ServiceLanguage,
                        RequestServiceType = u.RequestServiceType.ToString(),
                        RequestServiceSkills = u.RequestServiceSkills,
                        CreatedAt = u.CreatedAt.ToString(),
                    };
                    udto.Add(obj);
                }
                return new ObjectResult(new { data = udto, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto() { Status = false, StatusCode = "406", Message = GlobalMessages.SystemFailureMessage });
            }
        }*/

        [HttpPost("GetRequestServicesDatatableByUserId")]
        public async Task<IActionResult> GetRequestServicesDatatableByUserId(string? Name = "", string? SkillName = "")
        {
            try
            {
                UserClaims? getUsetFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
                var ulist = await requestServiceRepo.GetRequestServiceByUserId((int)getUsetFromToken.Id);
                //string userRegionRequestedTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getUsetFromToken.Timezone);


                if (!string.IsNullOrEmpty(Name))
                {
                    ulist = ulist.Where(x => x.ServiceTitle.ToLower().Contains(Name.ToLower())).ToList();
                }
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                if (sortColumn != "" && sortColumn != null)
                {
                    if (sortColumn != "0")
                    {
                        if (sortColumnDirection == "asc")
                        {
                            ulist = ulist.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                        }
                        else
                        {
                            ulist = ulist.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                        }
                    }
                }
                int totalrows = ulist.Count();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    ulist = ulist.Where(x => x.ServiceTitle.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                        x.ServiceDescription != null && x.ServiceDescription.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                        x.ServiceLanguage != null && x.ServiceLanguage.Trim().ToLower().Contains(searchValue.Trim().ToLower())
                                        ).ToList();
                }
                int totalrowsafterfilterinig = ulist.Count();

                ulist = ulist.Skip(skip).Take(pageSize).ToList();
                List<RequestServicesDto> udto = new List<RequestServicesDto>();
                foreach (RequestService u in ulist)
                {
                    string userRegionRequestedTime = GeneralPurpose.regionChanged(Convert.ToDateTime(u.CreatedAt), getUsetFromToken.Timezone);
                    string userRegionRequestStartTime = "";
                    string userRegionRequestEndTime = "";
                    if (u.FromDateTime != null && u.ToDateTime != null)
                    {
                        userRegionRequestStartTime = GeneralPurpose.regionChanged(Convert.ToDateTime(u.FromDateTime), getUsetFromToken.Timezone);
                        userRegionRequestEndTime = GeneralPurpose.regionChanged(Convert.ToDateTime(u.ToDateTime), getUsetFromToken.Timezone);

                    }


                    RequestServicesDto obj = new RequestServicesDto()
                    {
                        Id = u.Id,
                        EncId = StringCipher.EncryptId(u.Id),
                        PrefferedServiceTime = u.PrefferedServiceTime,
                        CategoriesOfProblems = u.CategoriesOfProblems,
                        ServiceDescription = u.ServiceDescription,
                        FromDateTime = u.FromDateTime.ToString(),
                        ToDateTime = u.ToDateTime.ToString(),
                        //AppointmentTime = u.FromDateTime.ToString() + " - " + u.ToDateTime.ToString(), // Time without region
                        AppointmentTime = userRegionRequestStartTime + " - " + userRegionRequestEndTime,
                        ServiceLanguage = u.ServiceLanguage,
                        RequestServiceType = u.RequestServiceType.ToString(),
                        RequestServiceSkills = u.RequestServiceSkills,
                        //CreatedAt = u.CreatedAt.ToString(), // Requested Time without region
                        CreatedAt = userRegionRequestedTime,
                    };
                    udto.Add(obj);
                }
                return new ObjectResult(new { data = udto, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto() { Status = false, StatusCode = "406", Message = GlobalMessages.SystemFailureMessage });
            }
        }
        #endregion

        #region Orders
        [HttpPost("GetOrdersDatatableByUserId")]
        public async Task<IActionResult> GetOrdersDatatableByUserId()
        {
            UserClaims? getUsetFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
            var ulist = await orderRepo.GetOrderByUserId((int)getUsetFromToken.Id);

            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;

            if (sortColumn != "" && sortColumn != null)
            {
                if (sortColumn != "0")
                {
                    if (sortColumnDirection == "asc")
                    {
                        ulist = ulist.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                    else
                    {
                        ulist = ulist.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                }
            }
            int totalrows = ulist.Count();


            // pagination
            int totalrowsafterfilterinig = ulist.Count();

            ulist = ulist.Skip(skip).Take(pageSize).ToList();

            List<OrderDtoList> udto = new List<OrderDtoList>();

            foreach (Order item in ulist)
            {
                var order = new OrderDtoList
                {
                    Id = item.Id.ToString(),
                    EncId = StringCipher.EncryptId(item.Id),
                    OrderTitle = item.OrderTitle,
                    StartDateTime = item.StartDateTime != null ? item.StartDateTime.ToString() : "",
                    EndDateTime = item.EndDateTime != null ? item.EndDateTime.ToString() : "",
                    OrderPrice = item.OrderPrice.ToString(),
                    IsDelivered = item.IsDelivered.ToString(),
                };
                if (item.OrderReason != null && item.OrderReason.Count > 0)
                {
                    foreach (var reason in item.OrderReason)
                    {
                        order.OrderReasonId = reason.Id.ToString();
                        order.OrderReasonExplanation = reason.ReasonExplanation;
                        order.OrderReasonType = reason.ReasonType.ToString();
                        order.OrderReasonIsActive = reason.IsActive.ToString();
                    }
                }
                udto.Add(order);
            }

            return new ObjectResult(new { data = udto, draw = Request.Form["draw"].FirstOrDefault(), recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig });
        }

        [HttpGet("GetOrderById")]
        public async Task<IActionResult> GetOrderById(string? orderId = "")
        {
            UserClaims? getUserFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
            if (string.IsNullOrEmpty(orderId))
            {
                return null;
            }
            var getOrder = await orderRepo.GetOrderById(StringCipher.DecryptId(orderId));
            var getCustomer = await userRepo.GetUserById((int)getOrder.CustomerId);
            var getValet = await userRepo.GetUserById((int)getOrder.ValetId);

            List<OrderDtoList> udto = new List<OrderDtoList>();
            var order = new OrderDtoList
            {
                Id = getOrder.Id.ToString(),
                OrderTrackingId = Encrypt((getOrder.Id + 1000).ToString()),
                EncId = StringCipher.EncryptId(getOrder.Id),
                OrderTitle = getOrder.OrderTitle,
                OrderStatus = getOrder.OrderStatus.ToString(),
                StartDateTime = getOrder.StartDateTime != null ? getOrder.StartDateTime.ToString() : "",
                EndDateTime = getOrder.EndDateTime != null ? getOrder.EndDateTime.ToString() : "",
                OrderPrice = getOrder.OrderPrice.ToString(),
                IsDelivered = getOrder.IsDelivered.ToString(),
                CustomerId = getOrder.CustomerId.ToString(),
                PackageBuyFrom = getOrder.PackageBuyFrom,
                CapturedId = getOrder.CapturedId,
                ValetId = getOrder.ValetId.ToString(),
                CreatedAt = Convert.ToDateTime(GeneralPurpose.regionChanged(Convert.ToDateTime(getOrder.CreatedAt), getUserFromToken.Timezone)).ToString("MM-dd-yyyy"),
                Rating = await ratingRepo.GetOrderRatingByOrderId(getOrder.Id)
            };
            

            if (getValet.Status != null)
            {
                order.ValetStatus = getValet.Status.ToString();
            }
            if (getValet.Status != null)
            {
                order.CustomerStatus = getCustomer.Status.ToString();
            }
            var getOrderReason = await orderReasonRepo.GetOrderReasonByOrderId(getOrder.Id);
            if (getOrderReason != null)
            {
                order.OrderReasonId = getOrderReason.Id.ToString();
                order.OrderReasonExplanation = getOrderReason.ReasonExplanation;
                order.OrderReasonType = getOrderReason.ReasonType.ToString();
                order.OrderReasonIsActive = getOrderReason.IsActive.ToString();
            }
            if (getOrder.CustomerId != Convert.ToInt32(getUserFromToken.Id))
            {
                order.UserName = getCustomer.FirstName + " " + getCustomer.LastName;
            }
            else
            {
                order.UserName = getValet.FirstName + " " + getValet.LastName;
            }

            return Ok(new ResponseDto() { Data = order, Status = true, StatusCode = "200" });
        }

        [HttpGet("GetEarnings")]
        public async Task<IActionResult> GetEarnings()
        {
            try
            {
                
                UserClaims? getUserFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
                var GetUser = await userRepo.GetUserById((int)getUserFromToken.Id);
                var GetAllOrders = await orderRepo.GetOrderByUserId((int)getUserFromToken.Id);
                long balance_available = 0, balance_pending = 0;
                decimal availableBalance = 0 ;
                var payPalTotalEarning = await _payPalGateWayService.GetPayPalEarnings((int)getUserFromToken.Id);
                if (GetUser != null && GetUser.Role == 4 && GetUser.StripeId != null)
                {
                    var requestOptions = new RequestOptions();
                    requestOptions.StripeAccount = GetUser.StripeId;
                    var service = new BalanceService();
                    Balance balance = service.Get(requestOptions);
                    var pendingList = balance.Pending;
                    var balanceList = balance.Available;
                    foreach (var x in balanceList)
                    {
                        balance_available = balance_available + x.Amount;
                        availableBalance = balance_available;
                    }
                    foreach (var x in pendingList)
                    {
                        balance_pending = balance_pending + x.Amount;
                    }
                    balance_pending = balance_pending / 100;
                    balance_available = balance_available / 100;
                    availableBalance = availableBalance / 100;

                }

                var response = new EarningsApiResponse
                {
                    BalancePending = balance_pending.ToString(),
                    BalanceAvailable = availableBalance.ToString(),
                    UserId = GetUser.Id,
                    PayPalEarning = payPalTotalEarning,
                };

                return Ok(new ResponseDto() { Data = response, Status = true, StatusCode = "200" });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto() { Status = true, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        #endregion

        #region SearchValet

        [HttpGet("SearchValet")]
        public async Task<IActionResult> GetSearchedValetRecord (string keyword)
        {
            try
            {
                var searchValets = await _searchLogService.SearchValetsAndSkillsByKey(keyword);
                if (searchValets.Count() > 0)
                {
                  return Ok(new ResponseDto() { Data = searchValets, Status = true, StatusCode = "200", Message = "Valets Found Successfully" });
                }

                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Record Not Found" });
            }
            catch(Exception ex)
            {
                MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = "Exception Occured" });
            }
        }

        [HttpGet("GetHighSearchedKeys")]
        public async Task<IActionResult> GetHighRankedKeys()
        {
            try
            {
                var rankedKeyWords = await _searchLogService.GetHighSearchVolumeKeys();
                if (rankedKeyWords.Count() > 0)
                {
                    return Ok(new ResponseDto() { Data = rankedKeyWords, Status = true, StatusCode = "200" });
                }
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Record Not Found" });
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(projectVariables.BaseUrl + " ----------<br>" + ex.Message.ToString() + "---------------" + ex.StackTrace);
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = "Exception Occured" });
            }
        }
        #endregion

        #region Create Stripe Connect Account

        
        
        [HttpPost("ValidateStripeAccount")]
        public async Task<bool> ValidateStripeAccount(string val = "")
        {
            try
            {
                if (val != "" && val != null)
                {
                    var GetService = new AccountService();
                    var GetAccount = GetService.Get(val);
                    if (GetAccount.StripeResponse.StatusCode == HttpStatusCode.OK)
                    {
                        var stripeAccountStatus = await GetStripeAccountStatus(val);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage("Environment: " + projectVariables.BaseUrl +
                    "<br/> Message: " + ex.Message.ToString() + "<br/> Path: " + ex.StackTrace);
                return false;
            }
        }

        [HttpGet("GetStripeAccountStatus")]
        public async Task<IActionResult> GetStripeAccountStatus(string accountId)
        {
            var response = new ResponseDto();
            try
            {
                if (!string.IsNullOrEmpty(accountId))
                {
                    response = await StripeAccountStatus(accountId);
                    if (response.Status == true)
                    {
                        return Ok(response);
                    }
                }
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.RecordNotFound });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage("Environment: " + projectVariables.BaseUrl +
                "<br/> Message: " + ex.Message.ToString() + "<br/> Path: " + ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        private async Task<ResponseDto> StripeAccountStatus(string accountId)
        {
            var response = new ResponseDto();
            try
            {
                var accountService = new AccountService();
                var account = await accountService.GetAsync(accountId);

                if (account.StripeResponse.StatusCode == HttpStatusCode.OK)
                {
                    var cardPaymentsCapability = account.Capabilities.CardPayments;
                    if (cardPaymentsCapability != null)
                    {
                        if (cardPaymentsCapability == "active")
                        {
                            response.Data = "Completed";
                            response.Status = true;
                            response.Message = "Stripe Account Verified";
                        }
                        else if (cardPaymentsCapability == "inactive")
                        {
                            response.Data = "Restricted";
                            response.Status = true;
                            response.Message = "Stripe Account Not Verified";
                        }
                    }
                }
                else
                {
                    response.Status = false;
                    response.StatusCode = "500";
                    response.Message = GlobalMessages.SystemFailureMessage;
                }
                return response;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                await MailSender.SendErrorMessage("Environment: " + projectVariables.BaseUrl +
                "<br/> Message: " + ex.Message.ToString() + "<br/> Path: " + ex.StackTrace);
                return response;
            }
        }

        #endregion

        #region UserPackages 

        [HttpGet("GetUserSessionStatus")]
        public async Task<IActionResult> GetUserSessionStatus(string customerId)
        {
            int customerIdInt = int.Parse(customerId);
            int? remainingSessions = await _packageService.GetRemainingSessionCount(customerIdInt);

            if (remainingSessions > 0)
            {
                return Ok(new ResponseDto { Status = true, StatusCode = "200", Data = remainingSessions });
            }
            else
            {
                return Ok(new ResponseDto { Status = true, StatusCode = "200", Data = 0 });
            }
        }

        #endregion
        private string Encrypt(string input)
        {
            StringBuilder encrypted = new StringBuilder();
            int shift = 3;
            foreach (char c in input)
            {
                if (char.IsLetter(c))
                {
                    char encryptedChar = (char)(c + shift);

                    if ((char.IsLower(c) && encryptedChar > 'z') || (char.IsUpper(c) && encryptedChar > 'Z'))
                    {
                        encryptedChar = (char)(c - (26 - shift));
                    }

                    encrypted.Append(encryptedChar);
                }
                else
                {
                    encrypted.Append(c);
                }
            }

            return encrypted.ToString();
        }

        #region CalenderEvent 
        [HttpGet("GetOrderEventsByUserId")]
        public async Task<IActionResult> GetOrderEventsByUserId (string Id)
        {
            int userId = Convert.ToInt32(Id);
            // Determine whether the user is a Valet or a Customer.
            var userObj = await userRepo.GetUserById(userId);
            var orderEvents = await orderRepo.GetOrderEventRecord(userId, userObj.Role);
            if(orderEvents.Count > 0)
            {
                return Ok(new ResponseDto { Status = true, StatusCode = "200", Data = orderEvents });
            }
            return Ok(new ResponseDto { Status = false, StatusCode = "400", Message = "Events Not found"});
        }

        [HttpGet("GetOrderEventsOfValet")]
        public async Task<IActionResult> GetOrderEventsOfValet(string Id)
        {
            int ValetId = StringCipher.DecryptId(Id);
            var orderEvents = await orderRepo.GetOrderEventRecordForValet(ValetId);
            if (orderEvents.Count > 0)
            {
                return Ok(new ResponseDto { Status = true, StatusCode = "200", Data = orderEvents });
            }
            return Ok(new ResponseDto { Status = false, StatusCode = "400", Message = "Events Not found" });
        }

        [HttpGet("GetBookedAvailabilitySlot")]
        public async Task<IActionResult> GetBookedAvailabilitySlot (string Id)
        {
            UserClaims? getUsetFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
            int valetId = StringCipher.DecryptId(Id);
            var getValet = await userRepo.GetUserById(valetId);
            var availableSlots = await userRepo.GetValetAvailableSlots(valetId);
            if (availableSlots != null)
            {
                if (getUsetFromToken.Role != "Valet")
                {
                    foreach (var item in availableSlots)
                    {
                        item.StartDateTime = DateTimeHelper.GetUtcTimeFromZoned(item.StartDateTime, getValet?.Timezone);
                        item.EndDateTime = DateTimeHelper.GetUtcTimeFromZoned(item.EndDateTime, getValet?.Timezone);
                        item.StartDateTime =  DateTimeHelper.GetZonedDateTimeFromUtc(item.StartDateTime, getUsetFromToken?.Timezone);
                        item.EndDateTime = DateTimeHelper.GetZonedDateTimeFromUtc(item.EndDateTime, getUsetFromToken?.Timezone);
                    }
                }
                return Ok(new ResponseDto { Data = availableSlots, Status = true, StatusCode = "200"});
            }
            return Ok(new ResponseDto { Status = false, StatusCode = "400", Message = "Valet is not Available" });
        }

        #endregion

        #region UserRating

        [HttpGet("GetValetRatingRecord")]
        public async Task<IActionResult> GetValetRatingRecord(string ValetEncId)
        {
            var valetRating = await ratingRepo.GetValetRatingRecords(ValetEncId);
            if(valetRating.Rating.Count > 0)
            {
                return Ok(new ResponseDto() { Data = valetRating, Status = true, StatusCode = "200" });
            }
            return Ok(new ResponseDto() { Status = false, StatusCode = "500", Message = "Rating Record Not Found" });
        }        
        
        [HttpGet("GetValetRatingRecordByDecreaptedId")]
        public async Task<IActionResult> GetValetRatingRecordByDecreaptedId(int ValetEncId)
        {
            var valetRating = await ratingRepo.GetValetStarsRatingForPayment(StringCipher.EncryptId(ValetEncId));
            if(valetRating != null)
            {
                return Ok(new ResponseDto() { Data = valetRating, Status = true, StatusCode = "200" });
            }
            return Ok(new ResponseDto() { Status = false, StatusCode = "500", Message = "Rating Record Not Found" });
        }
        #endregion'

        #region Orders

        [HttpPost("GetAllCompletedOrders")]
        public async Task<IActionResult> GetAllCompletedOrders(string? ValetId)
        {
            int Id = Convert.ToInt32(ValetId);
            var getAllCompletedOrderRecord = await orderRepo.GetCompletedOrderRecord(Id);
            // Apply filter based on searches
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            if (sortColumn != "" && sortColumn != null)
            {
                if (!string.IsNullOrEmpty(sortColumn) && sortColumn != "0")
                {
                    if (sortColumnDirection == "asc")
                    {
                        getAllCompletedOrderRecord = getAllCompletedOrderRecord.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                    else
                    {
                        getAllCompletedOrderRecord = getAllCompletedOrderRecord.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                }
            }
            int totalrows = getAllCompletedOrderRecord.Count();

            if (!string.IsNullOrEmpty(searchValue))
            {
                getAllCompletedOrderRecord = getAllCompletedOrderRecord.Where(x =>
                                    (x.OrderTitle != null && x.OrderTitle.ToLower().Contains(searchValue)) ||
                                    (x.EarnedFromOrder != null && x.EarnedFromOrder.ToLower().Contains(searchValue)) ||
                                    (x.CompletedAt != null && x.CompletedAt.ToLower().Contains(searchValue)) ||
                                    (x.OrderPrice != null && x.OrderPrice.ToLower().Contains(searchValue)) ||
                                    (x.OrderPaidBy != null && x.OrderPaidBy.ToLower().Contains(searchValue))
                                ).ToList();
            }
            int totalrowsafterfilterinig = getAllCompletedOrderRecord.Count();

            getAllCompletedOrderRecord = getAllCompletedOrderRecord.Skip(skip).Take(pageSize).ToList();
            List<CompletedOrderRecord> completedOrders = new List<CompletedOrderRecord>();
            foreach (var order in getAllCompletedOrderRecord)
            {
                CompletedOrderRecord obj = new CompletedOrderRecord()
                {
                   EncOrderId = order.EncOrderId,
                   OrderPrice = order.OrderPrice,
                   OrderPaidBy = order.OrderPaidBy,
                   EarnedFromOrder = order.EarnedFromOrder,
                   OrderTitle = order.OrderTitle,
                   CompletedAt = order.CompletedAt,
                };
                completedOrders.Add(obj);
            }
            return new ObjectResult(new { data = completedOrders, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig });
        }
        #endregion

        #region Helpers

        #region Skills
        private UserSkillDto MapToUserSkillDto(UserSkill skill)
        {
            return new UserSkillDto
            {
                Id = skill.Id,
                UserSkillEncId = StringCipher.EncryptId(skill.Id),
                SkillName = skill.SkillName,
                UserId = skill.UserId
            };
        }
        #endregion

        #region Experience/Service

        private UserExperienceDto MapToUserExperienceDto(UserExperience obj)
        {
            return new UserExperienceDto
            {
                Id = obj.Id,
                UserExperienceEncId = StringCipher.EncryptId(obj.Id),
                Title = obj.Title,
                Description = obj.Description,
                ExperienceFrom = obj.ExperienceFrom != null ? obj.ExperienceFrom.ToString() : null,
                ExperienceTo = obj.ExperienceTo != null ? obj.ExperienceTo.ToString() : null,
                Organization = obj.Organization,
                Website = obj.Website,
                UserId = obj.UserId
            };
        }
        #endregion

        private int DecryptionId(string userId)
        {
            userId = GeneralPurpose.ConversionEncryptedId(userId);
            var decrypt = StringCipher.DecryptId(userId);
            return decrypt;
        }
        #endregion

    }
}
