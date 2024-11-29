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
using System.Reflection;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IUserRepo userRepo;
        private readonly INotificationService userPackageRepo;
        private readonly IUserAvailableSlotRepo userAvailableSlotRepo;
        private readonly IJwtUtils jwtUtils;
        private readonly IOrderRepo _orderService;
        private readonly IPayPalGateWayService _payPalGateWayService;
        private readonly ProjectVariables projectVariables;
        private readonly IHubContext<NotificationHubSocket> _notificationHubSocket;
        private readonly IUserExperienceRepo userExperienceRepo;
        private readonly IUserEducationRepo userEducationRepo;
        private readonly IUserSkillRepo userSkillRepo;

        public AdminController(IUserEducationRepo _userEducationRepo, IUserExperienceRepo _userExperienceRepo, IUserSkillRepo _userSkillRepo, IUserRepo _userRepo, IOrderRepo orderService, IPayPalGateWayService payPalGateWayService, IJwtUtils _jwtUtils, IOptions<ProjectVariables> options, IUserAvailableSlotRepo _userAvailableSlotRepo, INotificationService _userPackageRepo, IHubContext<NotificationHubSocket> notificationHubSocket)
        {
            userPackageRepo = _userPackageRepo;
            userRepo = _userRepo;
            jwtUtils = _jwtUtils;
            projectVariables = options.Value;
            userAvailableSlotRepo = _userAvailableSlotRepo;
            _notificationHubSocket = notificationHubSocket;
            _orderService = orderService;
            _payPalGateWayService = payPalGateWayService;
            userExperienceRepo = _userExperienceRepo;
            userSkillRepo = _userSkillRepo;
            userEducationRepo = _userEducationRepo;
        }

        [HttpGet("PostIndex")]
        public async Task<IActionResult> PostIndex()
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

                return Ok(new ResponseDto() { Data = response, Status = true, StatusCode = "200" });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto() { Status = true, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin, EnumRoles.Employee })]
        [HttpPost("GetUserList")]
        public async Task<IActionResult> GetUserList(int Role, string? pendingRec = "", string? Name = "", string? Email = "", string? Contact = "", string? Country = "",
            string? State = "", string? City = "", string? IsActive = "")
        {
            var ulist = new List<User>();

            if (!string.IsNullOrEmpty(pendingRec))
            {
                ulist = (List<User>)await userRepo.GetAccountOnHold(Role);
            }
            else
            {
                ulist = (List<User>)await userRepo.GetUserList(Role);
            }
            if (!string.IsNullOrEmpty(Name))
            {
                ulist = ulist.Where(x => x.FirstName.ToLower().Contains(Name.ToLower()) || x.LastName.ToLower().Contains(Name.ToLower()) ||
                x.UserName.ToLower().Contains(Name.ToLower())).ToList();
            }
            if (!string.IsNullOrEmpty(IsActive))
            {
                ulist = ulist.Where(x => x.IsActive == Convert.ToInt16(IsActive)).ToList();
            }
            if (!string.IsNullOrEmpty(Email))
            {
                ulist = ulist.Where(x => x.Email.ToLower().Contains(Email.ToLower())).ToList();
            }
            if (!string.IsNullOrEmpty(Contact))
            {
                ulist = ulist.Where(x => x.Contact.ToLower().Contains(Contact.ToLower())).ToList();
            }
            if (!string.IsNullOrEmpty(Country))
            {
                ulist = ulist.Where(x => x.Country.ToLower().Contains(Country.ToLower())).ToList();
            }
            if (!string.IsNullOrEmpty(State))
            {
                ulist = ulist.Where(x => x.State.ToLower().Contains(State.ToLower())).ToList();
            }
            if (!string.IsNullOrEmpty(City))
            {
                ulist = ulist.Where(x => x.City.ToLower().Contains(City.ToLower())).ToList();
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
                if (!string.IsNullOrEmpty(sortColumn) && sortColumn != "0")
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
                ulist = ulist.Where(x => x.Email.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.FirstName != null && x.FirstName.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.LastName != null && x.LastName.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.UserName != null && x.UserName.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.Contact != null && x.Contact.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.Country != null && x.Country.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.City != null && x.City.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.State != null && x.State.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.Gender != null && x.Gender.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                    x.PricePerHour != null && x.PricePerHour.ToString().Trim().ToLower().Contains(searchValue.Trim().ToLower())
                                    ).ToList();
            }
            int totalrowsafterfilterinig = ulist.Count();

            ulist = ulist.Skip(skip).Take(pageSize).ToList();
            List<UserListDto> udto = new List<UserListDto>();
            foreach (User u in ulist)
            {
                UserListDto obj = new UserListDto()
                {
                    Id = u.Id,
                    UserEncId = StringCipher.EncryptId(u.Id),
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    UserName = u.UserName,
                    Contact = u.Contact,
                    Email = u.Email,
                    // Password = StringCipher.Decrypt(u.Password),
                    Gender = u.Gender,
                    ProfilePicture = u.ProfilePicture,
                    Country = u.Country,
                    State = u.State,
                    City = u.City,
                    Timezone = u.Timezone,
                    Availability = u.Availability.ToString(),
                    Status = u.Status.ToString(),
                    BirthDate = u.BirthDate.ToString(),
                    Role = Enum.GetName(typeof(EnumRoles), u.Role),
                    IsActive = Enum.GetName(typeof(EnumActiveStatus), u.IsActive)
                };
                udto.Add(obj);
            }
            return new ObjectResult(new { data = udto, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig });
        }


        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpPost("GetPayPalOrdersRecord")]
        public async Task<IActionResult> GetPayPalOrdersRecord(string? UserName = "", string? ItValet = "")
        {
            var paypalOrdersRecord = await _payPalGateWayService.GetPayPalOrdersRecord();
            // Apply filter based on CustomerName
            if (!string.IsNullOrEmpty(UserName))
            {
                paypalOrdersRecord = paypalOrdersRecord.Where(x =>
                    x.CustomerName.ToLower().Contains(UserName.ToLower())
                ).ToList();
            }

            if (!string.IsNullOrEmpty(ItValet))
            {
                paypalOrdersRecord = paypalOrdersRecord.Where(x =>
                    x.ITValet.ToLower().Contains(ItValet.ToLower())
                ).ToList();
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
                if (!string.IsNullOrEmpty(sortColumn) && sortColumn != "0")
                {
                    if (sortColumnDirection == "asc")
                    {
                        paypalOrdersRecord = paypalOrdersRecord.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                    else
                    {
                        paypalOrdersRecord = paypalOrdersRecord.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                }
            }
            int totalrows = paypalOrdersRecord.Count();

            if (!string.IsNullOrEmpty(searchValue))
            {
                paypalOrdersRecord = paypalOrdersRecord.Where(x =>
                                    (x.CustomerName != null && x.CustomerName.ToLower().Contains(searchValue)) ||
                                    (x.ITValet != null && x.ITValet.ToLower().Contains(searchValue)) ||
                                    (x.OrderTitle != null && x.OrderTitle.ToLower().Contains(searchValue)) ||
                                    (x.OrderPrice != null && x.OrderPrice.ToLower().Contains(searchValue)) ||
                                    (x.OrderStatus != null && x.OrderStatus.ToLower().Contains(searchValue)) ||
                                    (x.PaymentStatus != null && x.PaymentStatus.ToLower().Contains(searchValue))
                                ).ToList();
            }
            int totalrowsafterfilterinig = paypalOrdersRecord.Count();

            paypalOrdersRecord = paypalOrdersRecord.Skip(skip).Take(pageSize).ToList();
            List<PayPalOrderDetailsForAdminDB> paypalRecordDto = new List<PayPalOrderDetailsForAdminDB>();
            foreach (var orderObj in paypalOrdersRecord)
            {
                PayPalOrderDetailsForAdminDB obj = new PayPalOrderDetailsForAdminDB()
                {
                   Id = orderObj.Id,
                   ITValet = orderObj.ITValet,
                   OrderTitle = orderObj.OrderTitle,
                   OrderEncId = orderObj.OrderEncId,
                   OrderPrice = orderObj.OrderPrice,
                   OrderStatus = orderObj.OrderStatus,
                   PaymentStatus = orderObj.PaymentStatus,
                   CustomerName = orderObj.CustomerName,
                   CaptureId = orderObj.CaptureId,
                   PaidByPackage = orderObj.PaidByPackage,
                };
                paypalRecordDto.Add(obj);
            }
            return new ObjectResult(new { data = paypalRecordDto, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig });
        }


        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpPost("GetPayPalTransactionRecord")]
        public async Task<IActionResult> GetPayPalTransactionRecord(string? UserName = "", string? ItValet = "")
        {
            var paypalTransactionRecord = await _payPalGateWayService.GetPayPalTransactionsRecord();
            // Apply filter based on searches
            if (!string.IsNullOrEmpty(UserName))
            {
                paypalTransactionRecord = paypalTransactionRecord.Where(x =>
                    x.CustomerName.ToLower().Contains(UserName.ToLower())
                ).ToList();
            }

            if (!string.IsNullOrEmpty(ItValet))
            {
                paypalTransactionRecord = paypalTransactionRecord.Where(x =>
                    x.ITValetName.ToLower().Contains(ItValet.ToLower())
                ).ToList();
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
                if (!string.IsNullOrEmpty(sortColumn) && sortColumn != "0")
                {
                    if (sortColumnDirection == "asc")
                    {
                        paypalTransactionRecord = paypalTransactionRecord.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                    else
                    {
                        paypalTransactionRecord = paypalTransactionRecord.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                }
            }
            int totalrows = paypalTransactionRecord.Count();

            if (!string.IsNullOrEmpty(searchValue))
            {
                paypalTransactionRecord = paypalTransactionRecord.Where(x =>
                                    (x.CustomerName != null && x.CustomerName.ToLower().Contains(searchValue)) ||
                                    (x.ITValetName != null && x.ITValetName.ToLower().Contains(searchValue)) ||
                                    (x.OrderTitle != null && x.OrderTitle.ToLower().Contains(searchValue)) ||
                                    (x.OrderPrice != null && x.OrderPrice.ToLower().Contains(searchValue)) ||
                                    (x.PlatformFee != null && x.PlatformFee.ToLower().Contains(searchValue)) ||
                                    (x.SentAmount != null && x.SentAmount.ToLower().Contains(searchValue)) ||
                                    (x.PayPalEmailAccount != null && x.PayPalEmailAccount.ToLower().Contains(searchValue)) ||
                                    (x.TransactionStatus != null && x.TransactionStatus.ToLower().Contains(searchValue)) ||
                                    (x.ExpectedDateToTransmitPayment != null && x.ExpectedDateToTransmitPayment.ToLower().Contains(searchValue))
                                ).ToList();
            }
            int totalrowsafterfilterinig = paypalTransactionRecord.Count();

            paypalTransactionRecord = paypalTransactionRecord.Skip(skip).Take(pageSize).ToList();
            List<PayPalTransactionDetailsForAdminDB> paypalRecordDto = new List<PayPalTransactionDetailsForAdminDB>();
            foreach (var transactionObj in paypalTransactionRecord)
            {
                PayPalTransactionDetailsForAdminDB obj = new PayPalTransactionDetailsForAdminDB()
                {
                    ITValetName = transactionObj.ITValetName,
                    OrderTitle = transactionObj.OrderTitle,
                    OrderPrice = transactionObj.OrderPrice,
                    TransactionStatus = transactionObj.TransactionStatus,
                    PlatformFee = transactionObj.PlatformFee,
                    CustomerName = transactionObj.CustomerName,
                    OrderEncId = transactionObj.OrderEncId,
                    PayOutItemId = transactionObj.PayOutItemId,
                    SentAmount = transactionObj.SentAmount,
                    PayPalEmailAccount = transactionObj.PayPalEmailAccount,
                    ExpectedDateToTransmitPayment = transactionObj.ExpectedDateToTransmitPayment,
                };
                paypalRecordDto.Add(obj);
            }
            return new ObjectResult(new { data = paypalRecordDto, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig });
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpPost("GetPayPalUnclaimedPaymentRecord")]
        public async Task<IActionResult> GetPayPalUnclaimedPaymentRecord(string? UserName = "", string? ItValet = "")
        {
            var unclaimedPaymentRecord = await _payPalGateWayService.GetPayPalUnclaimedRecord();
            // Apply filter based on searches
            if (!string.IsNullOrEmpty(UserName))
            {
                unclaimedPaymentRecord = unclaimedPaymentRecord.Where(x =>
                    x.CustomerName.ToLower().Contains(UserName.ToLower())
                ).ToList();
            }

            if (!string.IsNullOrEmpty(ItValet))
            {
                unclaimedPaymentRecord = unclaimedPaymentRecord.Where(x =>
                    x.ITValetName.ToLower().Contains(ItValet.ToLower())
                ).ToList();
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
                if (!string.IsNullOrEmpty(sortColumn) && sortColumn != "0")
                {
                    if (sortColumnDirection == "asc")
                    {
                        unclaimedPaymentRecord = unclaimedPaymentRecord.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                    else
                    {
                        unclaimedPaymentRecord = unclaimedPaymentRecord.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                }
            }
            int totalrows = unclaimedPaymentRecord.Count();

            if (!string.IsNullOrEmpty(searchValue))
            {
                unclaimedPaymentRecord = unclaimedPaymentRecord.Where(x =>
                                    (x.CustomerName != null && x.CustomerName.ToLower().Contains(searchValue)) ||
                                    (x.ITValetName != null && x.ITValetName.ToLower().Contains(searchValue)) ||
                                    (x.OrderTitle != null && x.OrderTitle.ToLower().Contains(searchValue)) ||
                                    (x.Reason != null && x.Reason.ToLower().Contains(searchValue)) ||
                                    (x.PayPalEmailAccount != null && x.PayPalEmailAccount.ToLower().Contains(searchValue)) ||
                                    (x.TransactionStatus != null && x.TransactionStatus.ToLower().Contains(searchValue)) 
                                ).ToList();
            }
            int totalrowsafterfilterinig = unclaimedPaymentRecord.Count();

            unclaimedPaymentRecord = unclaimedPaymentRecord.Skip(skip).Take(pageSize).ToList();
            List<PayPalUnclaimedTransactionDetailsForAdminDB> paypalRecordDto = new List<PayPalUnclaimedTransactionDetailsForAdminDB>();
            foreach (var unclaimedObj in unclaimedPaymentRecord)
            {
                PayPalUnclaimedTransactionDetailsForAdminDB obj = new PayPalUnclaimedTransactionDetailsForAdminDB()
                {
                    ITValetName = unclaimedObj.ITValetName,
                    OrderTitle = unclaimedObj.OrderTitle,
                    Reason = unclaimedObj.Reason,
                    TransactionStatus = unclaimedObj.TransactionStatus,
                    UnclaimedAmountStatus = unclaimedObj.UnclaimedAmountStatus,
                    CustomerName = unclaimedObj.CustomerName,
                    OrderEncId = unclaimedObj.OrderEncId,
                    PayPalEmailAccount = unclaimedObj.PayPalEmailAccount,
                };
                paypalRecordDto.Add(obj);
            }
            return new ObjectResult(new { data = paypalRecordDto, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig });
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpPost("GetStripeOrdersRecord")]
        public async Task<IActionResult> GetStripeOrdersRecord(string? UserName = "", string? ItValet = "")
        {
            var stripeOrdersRecord = await _orderService.GetStripeOrdersRecord();
            // Apply filter based on CustomerName
            if (!string.IsNullOrEmpty(UserName))
            {
                stripeOrdersRecord = stripeOrdersRecord.Where(x =>
                    x.CustomerName.ToLower().Contains(UserName.ToLower())
                ).ToList();
            }

            if (!string.IsNullOrEmpty(ItValet))
            {
                stripeOrdersRecord = stripeOrdersRecord.Where(x =>
                    x.ITValet.ToLower().Contains(ItValet.ToLower())
                ).ToList();
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
                if (!string.IsNullOrEmpty(sortColumn) && sortColumn != "0")
                {
                    if (sortColumnDirection == "asc")
                    {
                        stripeOrdersRecord = stripeOrdersRecord.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                    else
                    {
                        stripeOrdersRecord = stripeOrdersRecord.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                }
            }
            int totalrows = stripeOrdersRecord.Count();

            if (!string.IsNullOrEmpty(searchValue))
            {
                stripeOrdersRecord = stripeOrdersRecord.Where(x =>
                                    (x.CustomerName != null && x.CustomerName.ToLower().Contains(searchValue)) ||
                                    (x.ITValet != null && x.ITValet.ToLower().Contains(searchValue)) ||
                                    (x.OrderTitle != null && x.OrderTitle.ToLower().Contains(searchValue)) ||
                                    (x.OrderPrice != null && x.OrderPrice.ToLower().Contains(searchValue)) ||
                                    (x.OrderStatus != null && x.OrderStatus.ToLower().Contains(searchValue)) ||
                                    (x.PaymentStatus != null && x.PaymentStatus.ToLower().Contains(searchValue))
                                ).ToList();
            }
            int totalrowsafterfilterinig = stripeOrdersRecord.Count();

            stripeOrdersRecord = stripeOrdersRecord.Skip(skip).Take(pageSize).ToList();
            List<StripeOrderDetailForAdminDb> stripeRecordDto = new List<StripeOrderDetailForAdminDb>();
            foreach (var orderObj in stripeOrdersRecord)
            {
                StripeOrderDetailForAdminDb obj = new StripeOrderDetailForAdminDb()
                {
                    Id = orderObj.Id,
                    ITValet = orderObj.ITValet,
                    OrderTitle = orderObj.OrderTitle,
                    OrderEncId = orderObj.OrderEncId,
                    OrderPrice = orderObj.OrderPrice,
                    OrderStatus = orderObj.OrderStatus,
                    PaymentStatus = orderObj.PaymentStatus,
                    CustomerName = orderObj.CustomerName,
                    StripeStatus = orderObj.StripeStatus,
                    StripeId = orderObj.StripeId,
                    IsDelivered = orderObj.IsDelivered,
                    PaidByPackage = orderObj.PaidByPackage,
                };
                stripeRecordDto.Add(obj);
            }
            return new ObjectResult(new { data = stripeRecordDto, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig });
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin, EnumRoles.Employee })]
        [HttpPost("GetSubscriptionDatatable")]
        public async Task<IActionResult> GetSubscriptionDatatable(int subscriptionType = -1)
        {
            try
            {
                var userPackageListDto = await userPackageRepo.GetUserPackageLists();

                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;

                if (!string.IsNullOrEmpty(searchValue))
                {
                    // Implement the search functionality based on the view model's properties
                    userPackageListDto = userPackageListDto
                        .Where(x => x.PackageName != null && x.PackageName.Trim().ToLower().Contains(searchValue.Trim().ToLower())
                            || (x.RemainingSessions != null && x.RemainingSessions.ToString().Contains(searchValue.ToLower()))
                            || (x.PackageType != null && x.PackageType.ToString().Contains(searchValue.ToLower()))
                            || (x.Customer != null && x.Customer.ToLower().Contains(searchValue.ToLower()))
                        ).ToList();
                }

                int totalrows = userPackageListDto.Count();
                int totalrowsafterfiltering = userPackageListDto.Count();

                if (sortColumn != "" && sortColumn != null)
                {
                    PropertyInfo propertyInfo = typeof(UserPackageListDto).GetProperty(sortColumn);

                    if (propertyInfo != null)
                    {
                        if (sortColumnDirection == "asc")
                        {
                            userPackageListDto = userPackageListDto.OrderByDescending(x => propertyInfo.GetValue(x)).ToList();
                        }
                        else
                        {
                            userPackageListDto = userPackageListDto.OrderBy(x => propertyInfo.GetValue(x)).ToList();
                        }
                    }
                    else
                    {
                        // Handle the case where sortColumn doesn't exist in UserPackageListDto
                    }
                }

                userPackageListDto = userPackageListDto.Skip(skip).Take(pageSize).ToList();

                return new ObjectResult(new { data = userPackageListDto, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfiltering });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                return StatusCode(StatusCodes.Status500InternalServerError);
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
            {
                IsCompleteValetAccount = await GeneralPurpose.CheckValuesNotEmpty(user, userExperienceRepo, userSkillRepo, _payPalGateWayService, userEducationRepo);
            }

            UserListDto obj = new UserListDto()
            {
                Id = user.Id,
                UserEncId = StringCipher.EncryptId(user.Id),
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Contact = user.Contact,
                Email = user.Email,
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

        [HttpPut("DeleteUser")]
        public async Task<bool> DeleteUser(int UserId)
        {
            bool ChkUserDeleted = await userRepo.DeleteUser(UserId);
            await _notificationHubSocket.Clients.All.SendAsync("LogOutDeletedUser", UserId);
            return ChkUserDeleted;
        }

        [HttpPut("UpdateUserActiveness")]
        public async Task<IActionResult> UpdateUserActiveness(int Id)
        {
            try
            {
                User? obj = await userRepo.GetUserById(Id);

                if (obj == null)
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = "No record found." });
                }
                obj.IsActive = (int)EnumActiveStatus.EmailVerificationPending;

                if (!await userRepo.SaveChanges())
                {
                    return Ok(false);
                }
                if (obj.Role == 4 || obj.Role == 3)
                {
                    bool chkVerificationMailSent = await MailSender.SendEmailForITValetAdminVerified(obj.Email, obj.UserName, (int)obj.Role);
                    bool chkConfirmationMailSent = await MailSender.EmailAccountVerification(StringCipher.EncryptId(obj.Id), obj.UserName, obj.Email, (int)obj.Role, projectVariables.BaseUrl);
                }
                return Ok(true);
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(false);
            }
        }

        [HttpPut("SendActivationEmail")]
        public async Task<IActionResult> SendActivationEmail(int Id)
        {
            try
            {
                User? obj = await userRepo.GetUserById(Id);

                if (obj == null)
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = "No record found." });
                }

                bool chkConfirmationMailSent = await MailSender.EmailAccountVerification(StringCipher.EncryptId(obj.Id), obj.UserName, obj.Email, (int)obj.Role, projectVariables.BaseUrl);

                return Ok(true);
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(false);
            }
        }

        [HttpGet("GetUserByIdEncryptedId")]
        public async Task<ActionResult<UserListDto>> GetUserByIdEncryptedId(string userId)
        {
            int Id = StringCipher.DecryptId(userId);
            var user = await userRepo.GetUserById(Id);
            var userEducation = await userEducationRepo.UserEducationRecordById(Id);
            var userExperience = await userExperienceRepo.UserExperiencedRecordById(Id);

            if (user == null)
            {
                return NotFound(new ResponseDto() { Status = false, StatusCode = "404", Message = "No record found." });
            }

            UserListDto obj = new UserListDto()
            {
                Id = user.Id,
                UserEncId = StringCipher.EncryptId(user.Id),
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Contact = user.Contact,
                Email = user.Email,
                Gender = user.Gender,
                ProfilePicture = user.ProfilePicture != null ? projectVariables.BaseUrl + user.ProfilePicture : null,
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
                Description = user.Description,
                CurrentTime = GeneralPurpose.regionChanged(Convert.ToDateTime(GeneralPurpose.DateTimeNow()), user.Timezone),
                StripeId = user.StripeId,
                UserEducations = userEducation,
                UserExperienced = userExperience,
            };
            var date = GeneralPurpose.DateTimeNow().Date;
            var slot = await userAvailableSlotRepo.GetUserAvailableSlotByUserIdAndDateOrDay(user.Id, date.ToString());
            if (slot != null)
            {
                obj.AvailabilitySlots = slot.Slot1 + "," + slot.Slot2 + "," + slot.Slot3 + "," + slot.Slot4;
            }
            return Ok(new ResponseDto() { Data = obj, Status = true, StatusCode = "200", Message = "Record Fetch Successfully" });
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpPost("PostAddUser")]
        public async Task<IActionResult> PostAddUser(PostAddUserDto user)
        {
            var obj = new User();

            if (!await userRepo.ValidateEmail(user.Email))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Duplicate email, please try another." });
            }

            obj.FirstName = user.FirstName;
            obj.LastName = user.LastName;
            obj.UserName = user.UserName;
            obj.Contact = user.Contact;
            obj.Email = user.Email;
            obj.Password = StringCipher.Encrypt(user.Password);
            obj.BirthDate = Convert.ToDateTime(user.BirthDate);
            obj.Country = user.Country;
            obj.State = user.State;
            obj.City = user.City;
            obj.ZipCode = user.ZipCode;
            obj.Timezone = user.Timezone;
            obj.Availability = Convert.ToInt32(user.Availability);
            obj.Status = Convert.ToInt32(user.Status);
            obj.Gender = user.Gender;
            obj.Role = Convert.ToInt32(user.Role);
            obj.IsActive = 1;
            obj.PricePerHour = 25;
            obj.CreatedAt = GeneralPurpose.DateTimeNow();

            if (!await userRepo.AddUser(obj))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Database updation failed." });
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Account has been created" });
        }

        [HttpPut("PostUpdateUser")]
        public async Task<IActionResult> PostUpdateUser(PostUpdateUserDto user)
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
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = "No record found." });
            }
            if (!await userRepo.ValidateEmail(obj.Email, obj.Id))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Duplicate email, please try another." });
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
                return Ok(new ResponseDto() { Data = obj, Status = false, StatusCode = "400", Message = "Database updation failed." });
            }

            return Ok(obj);
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpDelete("UpdateUserAccountStatus")]
        public async Task<IActionResult> UpdateUserAccountStatus(string UserId, EnumActiveStatus statuses)
        {
            bool chkRole = false;
            int getUserId = StringCipher.DecryptId(UserId);

            chkRole = await userRepo.UpdateUserAccountStatus(getUserId, statuses);

            if (!chkRole)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "406", Message = "Database Updation Failed" });
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Record Updated Successfully" });
        }

        [CustomAuthorize]
        [HttpPut("UploadPicture")]
        public async Task<IActionResult> UploadPicture(string UserId, IFormFile file)
        {

            if (!string.IsNullOrEmpty(UserId))
            {
                var getUser = new User();
                getUser = await userRepo.GetUserById(Convert.ToInt32(UserId));
                getUser.ProfilePicture = await UploadFiles(file, "profiles");
                if (!await userRepo.UpdateUser(getUser))
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "406", Message = "Database Updation Failed" });
                }
                int IsCompleteValetAccount = 1;
                if (getUser.Role == 4)
                {
                    IsCompleteValetAccount = await GeneralPurpose.CheckValuesNotEmpty(getUser, userExperienceRepo, userSkillRepo, _payPalGateWayService, userEducationRepo);
                }
                UserListDto obj = new UserListDto()
                {
                    Id = getUser.Id,
                    UserEncId = StringCipher.EncryptId(getUser.Id),
                    FirstName = getUser.FirstName,
                    LastName = getUser.LastName,
                    UserName = getUser.UserName,
                    Contact = getUser.Contact,
                    Email = getUser.Email,
                    Password = StringCipher.Decrypt(getUser.Password),
                    Gender = getUser.Gender,
                    ProfilePicture = getUser.ProfilePicture != null ? projectVariables.BaseUrl + "" + getUser.ProfilePicture : null,
                    Country = getUser.Country,
                    State = getUser.State,
                    City = getUser.City,
                    Timezone = getUser.Timezone,
                    Availability = getUser.Availability.ToString(),
                    Status = getUser.Status.ToString(),
                    BirthDate = getUser.BirthDate.ToString(),
                    Role = Enum.GetName(typeof(EnumRoles), getUser.Role),
                    IsCompleteValetAccount = IsCompleteValetAccount.ToString(),
                    IsActive = Enum.GetName(typeof(EnumActiveStatus), getUser.IsActive)
                };
                return Ok(new ResponseDto() { Data = obj, Status = true, StatusCode = "200", Message = "Image Updated Successfully" });
            }
            return Ok(new ResponseDto() { Status = false, StatusCode = "406", Message = "Record Not Found" });
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

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpGet("GetOrderEventsByUserId")]
        public async Task<IActionResult> GetOrderEventsByUserId(string Id)
        {
            int userId = StringCipher.DecryptId(Id);
            var userObj = await userRepo.GetUserById(userId);
            var orderEventsRecord = await _orderService.GetOrderEventRecord(userId, userObj.Role);
            if (orderEventsRecord.Count() > 0)
            {
                return Ok(new ResponseDto { Status = true, StatusCode = "200", Data = orderEventsRecord });
            }
            return Ok(new ResponseDto { Status = false, StatusCode = "400", Message = "Record Not Found" });
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
        [HttpGet]
        [Route("GetTimeZones")]
        public async Task<IActionResult> GetTimeZones()
        {
            var getKeyPairValues = DateTimeHelper.TimeZoneFriendlyNames;
            return Ok(new ResponseDto { Status = true, StatusCode = "200", Data = getKeyPairValues });
        }
    }
}
