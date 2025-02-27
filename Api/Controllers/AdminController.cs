using AutoMapper;
using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.NotificationHub;
using ITValet.Services;
using ITValet.Utils.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        public readonly IMapper _mapper;
        private readonly IJwtUtils jwtUtils;
        private readonly IUserRepo userRepo;
        private readonly IOrderRepo _orderService;
        private readonly IUserSkillRepo userSkillRepo;
        private readonly ProjectVariables projectVariables;
        private readonly IUserPackageService userPackageRepo;
        private readonly IUserEducationRepo userEducationRepo;
        private readonly IUserExperienceRepo userExperienceRepo;
        private readonly IUserAvailableSlotRepo userAvailableSlotRepo;
        private readonly IPayPalGateWayService _payPalGateWayService;
        private readonly IHubContext<NotificationHubSocket> _notificationHubSocket;

        public AdminController(IMapper mapper, IUserEducationRepo _userEducationRepo, IUserExperienceRepo _userExperienceRepo, 
            IUserSkillRepo _userSkillRepo, IUserRepo _userRepo, IOrderRepo orderService, IPayPalGateWayService payPalGateWayService, 
            IJwtUtils _jwtUtils, IOptions<ProjectVariables> options, IUserAvailableSlotRepo _userAvailableSlotRepo, 
            IUserPackageService _userPackageRepo, IHubContext<NotificationHubSocket> notificationHubSocket)
        {
            _mapper = mapper;
            jwtUtils = _jwtUtils;
            userRepo = _userRepo;
            _orderService = orderService;
            userSkillRepo = _userSkillRepo;
            projectVariables = options.Value;
            userPackageRepo = _userPackageRepo;
            userEducationRepo = _userEducationRepo;
            userExperienceRepo = _userExperienceRepo;
            userAvailableSlotRepo = _userAvailableSlotRepo;
            _notificationHubSocket = notificationHubSocket;
            _payPalGateWayService = payPalGateWayService;
        }

        [HttpGet("get-admin-dashboard-detail")]
        public async Task<IActionResult> GetAdminDashboardDetail()
        {
            try
            {
                int Customer = await userRepo.GetUserCount(3, EnumActiveStatus.Active);
                int Valet = await userRepo.GetUserCount(4, EnumActiveStatus.Active);
                int CustomersUnderReviewCount = await userRepo.GetUserCount(3, EnumActiveStatus.AccountOnHold);
                int ValetUnderReviewCount = await userRepo.GetUserCount(4, EnumActiveStatus.AccountOnHold);
                int CustomersVerificationPending = await userRepo.GetUserCountPendingVerifications(3);
                int ValetVerificationPending = await userRepo.GetUserCountPendingVerifications(4);

                var response = new
                {
                    Customer = Customer.ToString(),
                    Valet = Valet.ToString(),
                    ValetUnderReview = ValetUnderReviewCount.ToString(),
                    CustomersUnderReview = CustomersUnderReviewCount.ToString(),
                    CustomersVerificationPending = CustomersVerificationPending.ToString(),
                    ValetVerificationPending = ValetVerificationPending.ToString(),
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "", response));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));
            }
        }

        [CustomAuthorize]
        [HttpGet("GetUserById")]
        public async Task<ActionResult<UserListDto>> GetUserById(int id)
        {
            var user = await userRepo.GetUserById(id);

            if (user == null)
            {
                return NotFound(new ResponseDto() { Status = false, StatusCode = "404", Message = "No record found." });
            }
            int IsCompleteValetAccount = 1;
            if (user.Role == 4)
                IsCompleteValetAccount = await GeneralPurpose.CheckValuesNotEmpty(user, userSkillRepo);

            UserListDto obj = new UserListDto()
            {
                Id = user.Id,
                UserEncId = StringCipher.EncryptId(user.Id),
                FirstName = user.FirstName!.Trim(),
                LastName = user.LastName!.Trim(),
                UserName = user.UserName!.Trim(),
                Contact = user.Contact,
                Email = user.Email!.Trim(),
                Password = StringCipher.Decrypt(user?.Password!),
                Gender = user?.Gender,
                ProfilePicture = user?.ProfilePicture != null ? projectVariables.BaseUrl + user.ProfilePicture : null,
                Country = user?.Country,
                State = user?.State,
                City = user?.City,
                ZipCode = user?.ZipCode,
                Timezone = user?.Timezone,
                Availability = user?.Availability.ToString(),
                Status = user.Status.ToString(),
                BirthDate = user.BirthDate?.ToString("yyyy-MM-dd"),
                Role = Enum.GetName(typeof(EnumRoles), user.Role),
                IsActive = Enum.GetName(typeof(EnumActiveStatus), user.IsActive),
                Language = user.Language,
                Description = user.Description,
                StripeId = user.StripeId,
                IsVerify_StripeAccount = user.IsVerify_StripeAccount,
                IsBankAccountAdded = user.IsBankAccountAdded,
                IsCompleteValetAccount = IsCompleteValetAccount.ToString(),
                PricePerHour = user.PricePerHour.ToString(),
                HST = user.HST,
                AverageRating = user.AverageRating.ToString(),
                StarsCount = user.StarsCount
            };
            var date = GeneralPurpose.DateTimeNow().Date;
            var slot = await userAvailableSlotRepo.GetUserAvailableSlotByUserIdAndDateOrDay(user.Id, date.ToString());
            if (slot != null)
            {
                obj.AvailabilitySlots = slot.Slot1 + "," + slot.Slot2 + "," + slot.Slot3 + "," + slot.Slot4;
            }
            return Ok(new ResponseDto() { Data = obj, Status = true, StatusCode = "200", Message = "Record Fetch Successfully" });
        }

        [HttpDelete("DeleteUser")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            int userId = StringCipher.DecryptionId(id);
            bool isDeleted = await userRepo.DeleteUser(userId);

            if (isDeleted)
            {
                await _notificationHubSocket.Clients.All.SendAsync("LogOutDeletedUser", userId);
                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.DeletedMessage));
            }
            else
                return Ok(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));
        }

        [HttpPut("VerifyUserAccount/{userId}")]
        public async Task<IActionResult> VerifyUserAccount(string userId, string? type = "")
        {
            try
            {
                var decrypt = StringCipher.DecryptionId(userId);
                var user = await userRepo.GetUserById(decrypt);

                if (user == null)
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

                if(user.Role == (int)EnumRoles.Customer )
                    user.IsActive = (int)EnumActiveStatus.Active;
                else if (type == "RemoveFromHold")
                    user.IsActive = (int)EnumActiveStatus.Active;
                else if (type == "AdminVerificationPending")
                    user.IsActive = (int)EnumActiveStatus.AccountCompletion;
                else if (type == "ValetAccountCompletion")
                    user.IsActive = (int)EnumActiveStatus.Active;

                if (await userRepo.SaveChanges())
                {
                    if (type == "RemoveFromHold")
                    {
                        await MailSender.SendEmailReactiveUserAccount(user.Email!, user.UserName!, projectVariables.ReactUrl);
                        return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Congratulations! The account has been activated successfully"));
                    }
                    else if (type == "AdminVerificationPending")
                    {
                        await MailSender.SendEmailForITValetAdminVerified(user.Email!, user.UserName!, Enum.GetName(typeof(EnumRoles), user?.Role!)!);
                        if (user?.Role == (int)EnumRoles.Valet)
                            await MailSender.SendEmailToValetForProfileCompletion(user.Email!, user.UserName!);

                        return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Account has been verified successfully"));
                    }
                    else if (type == "ValetAccountCompletion")
                    {
                        return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Account verification process completed!", user));
                    }
                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.UpdateMessage));
                }
                else
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpPut("AccountOnHold/{userId}")]
        public async Task<IActionResult> AccountOnHold(string userId)
        {
            try
            {
                var decrypt = StringCipher.DecryptionId(userId);
                var user = await userRepo.GetUserById(decrypt);

                if (user == null)
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

                user.IsActive = (int)EnumActiveStatus.AccountOnHold;
                //user.IsActive = (int)EnumActiveStatus.Active;

                if (await userRepo.SaveChanges())
                {
                    await MailSender.SendAccountBlockedNotification(user.Email!, user.UserName!);
                    await _notificationHubSocket.Clients.All.SendAsync("LogOutWhenAccountOnHold", user.Id);
                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "The Account has been blocked"));
                }
                else
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpPost("PostAddUser")]
        public async Task<IActionResult> PostAddUser(PostAddUserDto user)
        {
            // Validate email
            if (!await userRepo.ValidateEmail(user.Email!))
                return Conflict(GlobalMessages.DuplicateEmail);

            // Validate username
            if (!await userRepo.ValidateUsername(user.UserName!))
                return Conflict(GlobalMessages.DuplicateUsername);

            if (!GeneralPurpose.MatchPassword(user.Password!, user.ConfirmPassword!))
                return BadRequest("Password and Confirm Password must be same.");


            var obj = _mapper.Map<User>(user);

            obj = GeneralPurpose.SetRoles(user.Role!, obj);
            obj.Password = StringCipher.Encrypt(user.Password!);
            obj.IsActive = obj.Role == (int)EnumRoles.Valet ? (int)EnumActiveStatus.AccountCompletion : (int)EnumActiveStatus.Active;
            obj.CreatedAt = GeneralPurpose.DateTimeNow();

            // Add user
            if (!await userRepo.AddUser(obj))
                return BadRequest(GlobalMessages.SystemFailureMessage);

            // Send verification email
            if (obj.Role == (int)EnumRoles.Customer || obj.Role == (int)EnumRoles.Valet || obj.Role == (int)EnumRoles.Seo)
                await MailSender.SendEmailWhenAdminCreateAccount(obj, user.Role!, projectVariables.ReactUrl);

            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.SuccessMessage, obj));
        }

        [HttpPut("PostUpdateUser/{Id}")]
        public async Task<IActionResult> PostUpdateUser(string Id, PostUpdateUserDto user)
        {
            int getUserId;
            if (!string.IsNullOrEmpty(user.UserEncId))
            {
                getUserId = StringCipher.DecryptId(user.UserEncId);
            }
            else
            {
                getUserId = (int)user.Id;
            }
            User? obj = await userRepo.GetUserById(getUserId);

            if (obj == null)
            {
                return BadRequest(GlobalMessages.RecordNotFound);
            }
            if (!await userRepo.ValidateEmail(obj.Email, obj.Id))
            {
                return BadRequest(GlobalMessages.DuplicateEmail);
            }

            obj.FirstName = !string.IsNullOrEmpty(user.FirstName) ? user.FirstName!.Trim() : obj.FirstName;
            obj.LastName = !string.IsNullOrEmpty(user.LastName) ? user.LastName!.Trim() : obj.LastName;
            obj.UserName = !string.IsNullOrEmpty(user.UserName) ? user.UserName!.Trim() : obj.UserName;
            obj.Contact = !string.IsNullOrEmpty(user.Contact) ? user.Contact!.Trim() : obj.Contact;
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


            if (user.ProfilePicture != null)
            {
                string imgName = DateTime.Now.Ticks.ToString() + user.ProfilePicture;
                var rootDir = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\profiles", imgName);
                obj.ProfilePicture = obj.ProfilePicture != null ? projectVariables.BaseUrl + "" + imgName : null;
            }

            if (!await userRepo.UpdateUser(obj))
            {
                return Ok(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }

            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.UpdateMessage, obj));
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpGet("GetActiveUsersNameForSearching")]
        public async Task<IActionResult> GetActiveUsersNameForSearching()
        {
            var activeUserName = await userRepo.FetchAllUsersName();
            if (activeUserName.Count() > 0)
            {
                return Ok(new ResponseDto { Status = true, StatusCode = "200", Data = activeUserName });
            }
            return Ok(new ResponseDto { Status = false, StatusCode = "400", Message = "Record Not Found" });
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpGet("GetOrderEventsRecordByOrderStatus")]
        public async Task<IActionResult> GetOrderEventsRecordByOrderStatus(string UserId, bool InProgress, bool cancelled, bool completed)
        {
            int userId = StringCipher.DecryptId(UserId);
            var userObj = await userRepo.GetUserById(userId);
            var orderEventsRecord = await _orderService.GetOrderEventRecordByOrderStatus(userId, userObj.Role, InProgress, cancelled, completed);
            if (orderEventsRecord.Count() > 0)
            {
                return Ok(new ResponseDto { Status = true, StatusCode = "200", Data = orderEventsRecord });
            }
            return Ok(new ResponseDto { Status = false, StatusCode = "400", Message = "Record Not Found" });
        }

        #region Paypal
        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpGet("GetPayPalOrdersRecord")]
        public async Task<IActionResult> GetPayPalOrdersRecord(int start, int length, string? sortColumnName, string? sortDirection,
            string? searchValue)
        {
            try
            {
                var paypalOrdersRecord = await _payPalGateWayService.GetPayPalOrdersRecord();

                // Initialize BaseService
                var baseService = new DatatableHelper<PayPalOrderDetailsForAdminDB>();

                // Apply sorting
                paypalOrdersRecord = baseService.ApplySorting(paypalOrdersRecord, sortColumnName, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    paypalOrdersRecord = baseService.ApplyFiltering(paypalOrdersRecord, o =>
                        o.CustomerName != null && o.CustomerName?.ToLower().Contains(searchValue.ToLower()) == true ||
                        o.ITValet != null && o.ITValet?.ToLower().Contains(searchValue.ToLower()) == true ||
                        o.OrderTitle != null && o.OrderTitle?.ToLower().Contains(searchValue.ToLower()) == true ||
                        o.OrderPrice != null && o.OrderPrice?.ToLower().Contains(searchValue.ToLower()) == true ||
                        o.OrderStatus != null && o.OrderStatus?.ToLower().Contains(searchValue.ToLower()) == true ||
                        o.PaymentStatus != null && o.PaymentStatus?.ToLower().Contains(searchValue.ToLower()) == true);
                }

                // Record counts
                int totalRows = paypalOrdersRecord.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    paypalOrdersRecord = baseService.ApplyPagination(paypalOrdersRecord, start, length);
                }

                // Map data to DTOs
                var payPalOrderDetailDtos = MappingHelper.MapPaypalOrderDetailToDtos(paypalOrdersRecord);

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Record Found", new
                {
                    draw = (start / length) + 1,
                    data = payPalOrderDetailDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                }));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpGet("GetPayPalTransactionRecord")]
        public async Task<IActionResult> GetPayPalTransactionRecord(int start, int length, string? sortColumnName, string? sortDirection,
            string? searchValue)
        {
            try
            {
                var paypalTransactionRecord = await _payPalGateWayService.GetPayPalTransactionsRecord();

                // Initialize BaseService
                var baseService = new DatatableHelper<PayPalTransactionDetailsForAdminDB>();

                // Apply sorting
                paypalTransactionRecord = baseService.ApplySorting(paypalTransactionRecord, sortColumnName, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    paypalTransactionRecord = baseService.ApplyFiltering(paypalTransactionRecord, x =>
                        (x.CustomerName != null && x.CustomerName.ToLower().Contains(searchValue)) ||
                        (x.ITValetName != null && x.ITValetName.ToLower().Contains(searchValue)) ||
                        (x.OrderTitle != null && x.OrderTitle.ToLower().Contains(searchValue)) ||
                        (x.OrderPrice != null && x.OrderPrice.ToLower().Contains(searchValue)) ||
                        (x.PlatformFee != null && x.PlatformFee.ToLower().Contains(searchValue)) ||
                        (x.SentAmount != null && x.SentAmount.ToLower().Contains(searchValue)) ||
                        (x.PayPalEmailAccount != null && x.PayPalEmailAccount.ToLower().Contains(searchValue)) ||
                        (x.TransactionStatus != null && x.TransactionStatus.ToLower().Contains(searchValue)) ||
                        (x.ExpectedDateToTransmitPayment != null && x.ExpectedDateToTransmitPayment.ToLower().Contains(searchValue)));
                }

                // Record counts
                int totalRows = paypalTransactionRecord.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    paypalTransactionRecord = baseService.ApplyPagination(paypalTransactionRecord, start, length);
                }

                // Map data to DTOs
                var transactionDetailDtos = MappingHelper.MapPaypalTransactionDetailToDtos(paypalTransactionRecord);

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Record Found", new
                {
                    draw = (start / length) + 1,
                    data = transactionDetailDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                }));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpGet("GetPayPalUnclaimedPaymentRecord")]
        public async Task<IActionResult> GetPayPalUnclaimedPaymentRecord(int start, int length, string? sortColumnName, string? sortDirection,
            string? searchValue)
        {
            try
            {
                var unclaimedPaymentRecord = await _payPalGateWayService.GetPayPalUnclaimedRecord();

                // Initialize BaseService
                var baseService = new DatatableHelper<PayPalUnclaimedTransactionDetailsForAdminDB>();

                // Apply sorting
                unclaimedPaymentRecord = baseService.ApplySorting(unclaimedPaymentRecord, sortColumnName, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    unclaimedPaymentRecord = baseService.ApplyFiltering(unclaimedPaymentRecord, x =>
                        (x.CustomerName != null && x.CustomerName.ToLower().Contains(searchValue)) ||
                        (x.ITValetName != null && x.ITValetName.ToLower().Contains(searchValue)) ||
                        (x.OrderTitle != null && x.OrderTitle.ToLower().Contains(searchValue)) ||
                        (x.Reason != null && x.Reason.ToLower().Contains(searchValue)) ||
                        (x.PayPalEmailAccount != null && x.PayPalEmailAccount.ToLower().Contains(searchValue)) ||
                        (x.TransactionStatus != null && x.TransactionStatus.ToLower().Contains(searchValue)));
                }

                // Record counts
                int totalRows = unclaimedPaymentRecord.Count();
                int totalRowsAfterFiltering = totalRows;


                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    unclaimedPaymentRecord = baseService.ApplyPagination(unclaimedPaymentRecord, start, length);
                }

                // Map data to DTOs
                var unclaimedPaymentDtos = MappingHelper.MapPaypalUnclaimedTransactionToDtos(unclaimedPaymentRecord);

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Record Found", new
                {
                    draw = (start / length) + 1,
                    data = unclaimedPaymentDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                }));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }
        #endregion

        [HttpGet]
        [Route("GetTimeZones")]
        public IActionResult GetTimeZones()
        {
            var getKeyPairValues = DateTimeHelper.TimeZoneFriendlyNames;
            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Found", getKeyPairValues));
        }
    }
}
