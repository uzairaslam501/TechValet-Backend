using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.Services;
using ITValet.Utils.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    [CustomAuthorize]
    [LogApiRequestResponseFilter]
    public class DatatableController : ControllerBase
    {
        private readonly IRequestServiceRepo requestServiceRepo;
        private readonly IOrderRepo orderRepo;
        private readonly IJwtUtils jwtUtils;
		private readonly INotificationService _userPackageService;
		private readonly IUserRepo _userRepo;
        private readonly IContactUsRepo _contactUsRepo;
        private readonly IPayPalGateWayService _payPalGateWayService;
        private readonly IUserEducationRepo _userEducationRepo;
        private readonly IUserExperienceRepo _userExperienceRepo;
        private readonly IUserSocialProfileRepo _userSocialProfileRepo;
        private readonly IUserSkillRepo _userSkillRepo;
        private readonly IUserTagRepo _userTagRepo;
        private readonly ProjectVariables _projectVariables;
        public DatatableController(IJwtUtils _jwtUtils, IRequestServiceRepo _requestServiceRepo,
            IOrderRepo _orderRepo, INotificationService userPackageService, IUserRepo userRepo,
            IContactUsRepo contactUsRepo, IPayPalGateWayService payPalGateWayService,
            IUserEducationRepo userEducationRepo, IUserExperienceRepo userExperienceRepo,
            IUserSocialProfileRepo userSocialProfileRepo, IUserSkillRepo userSkillRepo, 
            IUserTagRepo userTagRepo, IOptions<ProjectVariables> options)
        {
            jwtUtils = _jwtUtils;
            requestServiceRepo = _requestServiceRepo;
            orderRepo = _orderRepo;
            _userPackageService = userPackageService;
            _userRepo = userRepo;
            _contactUsRepo = contactUsRepo;
            _payPalGateWayService = payPalGateWayService;
            _userEducationRepo = userEducationRepo;
            _userExperienceRepo = userExperienceRepo;
            _userSocialProfileRepo = userSocialProfileRepo;
            _userSkillRepo = userSkillRepo;
            _userTagRepo = userTagRepo;
            _projectVariables = options.Value;
        }
        
        [HttpGet("GetRequestServicesDatatableByUserIdAsync")]
        public async Task<IActionResult> GetRequestServicesDatatableByUserIdAsync(int start, int length , string? sortColumnName ,string? sortDirection , string? searchValue , string? name)
        {
            try
            {
                UserClaims? getUserFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
                var requestServices = await requestServiceRepo.GetRequestServiceByUserId((int)getUserFromToken!.Id!);

                var baseService = new DatatableHelper<RequestService>();

                // Apply sorting
                requestServices = baseService.ApplySorting(requestServices, sortColumnName, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    requestServices = baseService.ApplyFiltering(requestServices, rs =>
                        rs.ServiceTitle?.ToLower().Contains(searchValue.ToLower()) == true ||
                        (rs.ServiceDescription != null && rs.ServiceDescription.ToLower().Contains(searchValue.ToLower())) ||
                        (rs.ServiceLanguage != null && rs.ServiceLanguage.ToLower().Contains(searchValue.ToLower()))
                    ).ToList();
                }

                if (!string.IsNullOrEmpty(name))
                    requestServices = requestServices.Where(rs => rs.ServiceTitle?.ToLower().Contains(name.ToLower()) == true).ToList();
                

                int totalRows = requestServices.Count();
                int totalRowsAfterFiltering = totalRows;

                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    requestServices = baseService.ApplyPagination(requestServices, start, length);
                }

                // Map data to DTOs
                var requestServicesDtos = MappingHelper.MapRequestServiceToDtos(requestServices, getUserFromToken);

                var response = new 
                {
                    draw = (start / length) + 1,
                    data = requestServicesDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));


            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpGet("orders-by-userId")]
        public async Task<IActionResult> GetOrderDatatableByUserId(int start, int length, string? sortColumnName, string? sortDirection, string? searchValue)
        {
            try
            {
                var userClaims = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
                var orders = await orderRepo.GetOrderByUserId((int)userClaims.Id);

                // Initialize BaseService
                var baseService = new DatatableHelper<Models.Order>();

                // Apply sorting
                orders = baseService.ApplySorting(orders, sortColumnName, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    orders = baseService.ApplyFiltering(orders, o =>
                        o.OrderTitle?.ToLower().Contains(searchValue.ToLower()) == true ||
                        o.OrderReason != null && o.OrderReason.Any(r => r.ReasonExplanation.ToLower().Contains(searchValue.ToLower())));
                }

                // Record counts
                int totalRows = orders.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    orders = baseService.ApplyPagination(orders, start, length);
                }

                // Map data to DTOs
                var orderDtos = MappingHelper.MapOrdersToDtos(orders);

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Record Found", new
                {
                    draw = (start / length) + 1,
                    data = orderDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                }));
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpGet("GetUserPackageDatatableAsync")]
		public async Task<IActionResult> GetUserPackageDatatableAsync(int start, int length, string? sortColumnName, string? sortDirection, string? searchValue, int? UserId)
		{
			try
			{
                UserClaims? getUserFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
                var userPackages = await _userPackageService.GetUserPackageListByUserId((int)getUserFromToken!.Id!);
                var userPackageList = userPackages.ToList();

                // Initialize BaseService
                var baseService = new DatatableHelper<UserPackage>();

                // Apply sorting
                userPackageList = baseService.ApplySorting(userPackageList, sortColumnName, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToLower().Trim();
                    userPackageList = baseService.ApplyFiltering(userPackageList, p =>
                        (p.PackageName != null && p.PackageName.ToLower().Contains(searchValue)) ||
                        (p.TotalSessions != null && p.TotalSessions.ToString().Contains(searchValue)) ||
                        (p.RemainingSessions != null && p.RemainingSessions.ToString().Contains(searchValue)) ||
                        (p.PackageType != null && p.PackageType.ToString().Contains(searchValue))).ToList();
                }

				int totalRows = userPackageList.Count();
				int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    userPackageList = baseService.ApplyPagination(userPackageList, start, length);
                }

                // Assume _userRepo is injected via DI
                var userPackageDtos = await MappingHelper.MapUserPackageToDtos(userPackageList, _userRepo);

                var response = new
                {
                    draw = (start / length) + 1,
                    data = userPackageDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));
            }
			catch (Exception ex)
			{
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
		}


		[HttpGet("GetOrdersDatatableByPackageId")]
		public async Task<IActionResult> GetOrdersDatatableByPackageId(int start, int length, string? sortColumnName, string? sortDirection, string? searchValue, int? packageId)
		{
			try
			{
				var orderList = await orderRepo.GetOrderByPackageId(packageId);
				var orderListMaterialized = orderList.ToList();

                // Initialize BaseService
                var baseService = new DatatableHelper<Models.Order>();

                // Apply sorting
                orderListMaterialized = baseService.ApplySorting(orderListMaterialized, sortColumnName, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    string search = searchValue.ToLower().Trim();
                    orderListMaterialized = baseService.ApplyFiltering(orderListMaterialized, o =>
                        (o.OrderTitle != null && o.OrderTitle.ToLower().Contains(search)) ||
                        (o.OrderPrice != null && o.OrderPrice.ToString().Contains(search)) ||
                        (o.IsDelivered != null && o.IsDelivered.ToString().Contains(search))
                    ).ToList();
                }

                // Record counts
                int totalRows = orderListMaterialized.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    orderListMaterialized = baseService.ApplyPagination(orderListMaterialized, start, length);
                }

                // Map data to DTOs
                var orderDtos = MappingHelper.MapOrders_ByPackageId_ToDtos(orderListMaterialized);


                var response = new
                {
                    draw = (start / length) + 1,
                    data = orderDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpGet("GetContactListAsync")]
        public async Task<IActionResult> GetContactListAsync(int start, int length, string? sortColumnName, string? sortDirection, string? searchValue, string? Name = "", string? Email = "", string? subject = "")
        {
            try
            {
                var contactList = await _contactUsRepo.GetContactList();
                var contactListMaterialized = contactList.ToList();

                // Initialize BaseService
                var baseService = new DatatableHelper<Contact>();

                // Apply sorting
                contactListMaterialized = baseService.ApplySorting(contactListMaterialized, sortColumnName, sortDirection);


                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToLower().Trim();
                    contactListMaterialized = baseService.ApplyFiltering(contactListMaterialized, c =>
                        (c.Name != null && c.Name.ToLower().Contains(searchValue)) ||
                        (c.Email != null && c.Email.ToLower().Contains(searchValue)) ||
                        (c.Subject != null && c.Subject.ToLower().Contains(searchValue))
                    ).ToList();
                }

                // Record counts
                int totalRows = contactListMaterialized.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    contactListMaterialized = baseService.ApplyPagination(contactListMaterialized, start, length);
                }

                var contactDtos = contactListMaterialized.Select(contact => new ContactDto
                {
                    Id = contact.Id,
                    UserContactEncId = StringCipher.EncryptId(contact.Id),
                    Name = contact.Name?.ToString(),
                    Email = contact.Email?.ToString(),
                    Subject = contact.Subject?.ToString(),
                    Message = contact.Message?.ToString()
                }).ToList();


                var response = new
                {
                    draw = (start / length) + 1,
                    data = contactDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));

            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin, EnumRoles.Employee })]
        [HttpGet("GetUserListAsync")]
        public async Task<IActionResult> GetUserListAsync(int Role, int start, int length, string? pendingRec = "", string? Name = "", 
            string? Email = "", string? Contact = "", string? Country = "", string? State = "", string? City = "", string? IsActive = "",  
            string? sortColumn = "", string? sortDirection = "asc", string? searchValue = "")
        {
            try
            {
                var ulist = new List<User>();

                if (!string.IsNullOrEmpty(pendingRec))
                {
                    ulist = (List<User>)await _userRepo.GetAccountOnHold(Role);
                }
                else
                {
                    ulist = (List<User>)await _userRepo.GetUserList(Role);
                }

                if(!string.IsNullOrEmpty(Name) || !string.IsNullOrEmpty(Email) || !string.IsNullOrEmpty(Contact) || !string.IsNullOrEmpty(Country) || 
                    !string.IsNullOrEmpty(State) || !string.IsNullOrEmpty(City) || !string.IsNullOrEmpty(IsActive))
                {
                    ulist = MappingHelper.FilterUsersList(ulist, Name, Email, Contact, Country, State, City , IsActive);
                }
                            
                // Initialize BaseService
                var baseService = new DatatableHelper<User>();

                // Apply sorting
                ulist = baseService.ApplySorting(ulist, sortColumn, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    string search = searchValue.ToLower().Trim();
                    ulist = baseService.ApplyFiltering(ulist, x =>
                        x.Email?.ToLower().Contains(search) == true ||
                        x.FirstName != null && x.FirstName.ToLower().Contains(search) ||
                        x.LastName != null && x.LastName.ToLower().Contains(search) ||
                        x.UserName != null && x.UserName.ToLower().Contains(search) ||
                        x.Contact != null && x.Contact.ToLower().Contains(search) ||
                        x.Country != null && x.Country.ToLower().Contains(search) ||
                        x.State != null && x.State.ToLower().Contains(search) ||
                        x.City != null && x.City.ToLower().Contains(search) ||
                        x.Gender != null && x.Gender.ToLower().Contains(search) ||
                        (x.PricePerHour != null && x.PricePerHour.ToString().ToLower().Contains(search))
                    ).ToList();
                }

                // Record counts
                int totalRows = ulist.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    ulist = baseService.ApplyPagination(ulist, start, length);
                }

                // Map data to DTOs
                var usersDtos = MappingHelper.MapUsersToDtos(ulist);

                var response = new
                {
                    draw = (start / length) + 1,
                    data = usersDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpGet("GetPayPalOrdersRecordAsync")]
        public async Task<IActionResult> GetPayPalOrdersRecordAsync(int start, int length, string? UserName = "", string? ItValet = "", 
            string? searchValue = "", string? sortColumnName = "",string? sortDirection = "")
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
                    string search = searchValue.ToLower().Trim();
                    paypalOrdersRecord = baseService.ApplyFiltering(paypalOrdersRecord, x =>
                        (x.CustomerName != null && x.CustomerName.ToLower().Contains(search)) ||
                        (x.ITValet != null && x.ITValet.ToLower().Contains(search)) ||
                        (x.OrderTitle != null && x.OrderTitle.ToLower().Contains(search)) ||
                        (x.OrderPrice != null && x.OrderPrice.ToLower().Contains(search)) ||
                        (x.OrderStatus != null && x.OrderStatus.ToLower().Contains(search)) ||
                        (x.PaymentStatus != null && x.PaymentStatus.ToLower().Contains(search))
                    ).ToList();
                }

                if (!string.IsNullOrEmpty(UserName))
                {
                    paypalOrdersRecord = paypalOrdersRecord.Where(x =>
                        x.CustomerName != null && x.CustomerName.ToLower().Contains(UserName.ToLower())
                    ).ToList();
                }

                if (!string.IsNullOrEmpty(ItValet))
                {
                    paypalOrdersRecord = paypalOrdersRecord.Where(x =>
                        x.ITValet != null && x.ITValet.ToLower().Contains(ItValet.ToLower())
                    ).ToList();
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
                var paypalRecordDto = MappingHelper.MapPaypalOrdersToDtos(paypalOrdersRecord);

                var response = new
                {
                    draw = (start / length) + 1,
                    data = paypalRecordDto,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));

            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpGet("GetPayPalUnclaimedPaymentRecordAsync")]
        public async Task<IActionResult> GetPayPalUnclaimedPaymentRecordAsync(
            int start,
            int length,
            string? userName = "",
            string? itValet = "",
            string? sortColumn = "",
            string sortColumnDirection = "",
            string? searchValue = "")
        {

            try
            {
                var unclaimedPaymentRecord = await _payPalGateWayService.GetPayPalUnclaimedRecord();

                // Initialize BaseService
                var baseService = new DatatableHelper<PayPalUnclaimedTransactionDetailsForAdminDB>();

                if (!string.IsNullOrEmpty(userName))
                {
                    unclaimedPaymentRecord = unclaimedPaymentRecord.Where(x =>
                        x.CustomerName.ToLower().Contains(userName.ToLower())
                    ).ToList();
                }

                if (!string.IsNullOrEmpty(itValet))
                {
                    unclaimedPaymentRecord = unclaimedPaymentRecord.Where(x =>
                        x.ITValetName.ToLower().Contains(itValet.ToLower())
                    ).ToList();
                }


                // Apply sorting
                unclaimedPaymentRecord = baseService.ApplySorting(unclaimedPaymentRecord, sortColumn, sortColumnDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    unclaimedPaymentRecord = baseService.ApplyFiltering(unclaimedPaymentRecord, x =>
                        (x.CustomerName != null && x.CustomerName.ToLower().Contains(searchValue)) ||
                        (x.ITValetName != null && x.ITValetName.ToLower().Contains(searchValue)) ||
                        (x.OrderTitle != null && x.OrderTitle.ToLower().Contains(searchValue)) ||
                        (x.Reason != null && x.Reason.ToLower().Contains(searchValue)) ||
                        (x.PayPalEmailAccount != null && x.PayPalEmailAccount.ToLower().Contains(searchValue)) ||
                        (x.TransactionStatus != null && x.TransactionStatus.ToLower().Contains(searchValue))
                    ).ToList();
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
                var paypalRecordDto = MappingHelper.MapPaypalUnclaimedPaymentRecordToDtos(unclaimedPaymentRecord);
                
                var response = new
                {
                    draw = (start / length) + 1,
                    data = paypalRecordDto,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpGet("GetPayPalTransactionRecordAsync")]
        public async Task<IActionResult> GetPayPalTransactionRecordAsync(
            int start,
            int length,
            string? userName = "",
            string? itValet = "",
            string? sortColumn = "",
            string sortColumnDirection = "",
            string? searchValue = "")
        {
            try
            {
                var paypalTransactionRecord = await _payPalGateWayService.GetPayPalTransactionsRecord();

                if (!string.IsNullOrEmpty(userName))
                {
                    paypalTransactionRecord = paypalTransactionRecord.Where(x =>
                        x.CustomerName.ToLower().Contains(userName.ToLower())
                    ).ToList();
                }

                if (!string.IsNullOrEmpty(itValet))
                {
                    paypalTransactionRecord = paypalTransactionRecord.Where(x =>
                        x.ITValetName.ToLower().Contains(itValet.ToLower())
                    ).ToList();
                }

                // Initialize BaseService
                var baseService = new DatatableHelper<PayPalTransactionDetailsForAdminDB>();

                // Apply sorting
                paypalTransactionRecord = baseService.ApplySorting(paypalTransactionRecord, sortColumn, sortColumnDirection);

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
                        (x.ExpectedDateToTransmitPayment != null && x.ExpectedDateToTransmitPayment.ToLower().Contains(searchValue))
                    ).ToList();
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
                var paypalRecordDto = MappingHelper.MapPaypalTransactionRecordsToDtos(paypalTransactionRecord);

                var response = new
                {
                    draw = (start / length) + 1,
                    data = paypalRecordDto,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));

            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpGet("GetStripeOrdersRecord")]
        public async Task<IActionResult> GetStripeOrdersRecord(
             int start,
             int length,
             string? userName = "",
             string? itValet = "",
             string? sortColumn = "",
             string sortColumnDirection = "",
             string? searchValue = "")
        {
            try
            {
                var stripeOrdersRecord = await orderRepo.GetStripeOrdersRecord();
                if (!string.IsNullOrEmpty(userName))
                {
                    stripeOrdersRecord = stripeOrdersRecord.Where(x =>
                        x.CustomerName.ToLower().Contains(userName.ToLower())
                    ).ToList();
                }

                if (!string.IsNullOrEmpty(itValet))
                {
                    stripeOrdersRecord = stripeOrdersRecord.Where(x =>
                        x.ITValet.ToLower().Contains(itValet.ToLower())
                    ).ToList();
                }

                // Initialize BaseService
                var baseService = new DatatableHelper<StripeOrderDetailForAdminDb>();

                // Apply sorting
                stripeOrdersRecord = baseService.ApplySorting(stripeOrdersRecord, sortColumn, sortColumnDirection);
                
                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    stripeOrdersRecord = baseService.ApplyFiltering(stripeOrdersRecord, x =>
                        (x.CustomerName != null && x.CustomerName.ToLower().Contains(searchValue)) ||
                        (x.ITValet != null && x.ITValet.ToLower().Contains(searchValue)) ||
                        (x.OrderTitle != null && x.OrderTitle.ToLower().Contains(searchValue)) ||
                        (x.OrderPrice != null && x.OrderPrice.ToLower().Contains(searchValue)) ||
                        (x.OrderStatus != null && x.OrderStatus.ToLower().Contains(searchValue)) ||
                        (x.PaymentStatus != null && x.PaymentStatus.ToLower().Contains(searchValue))
                    ).ToList();
                }

                // Record counts
                int totalRows = stripeOrdersRecord.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    stripeOrdersRecord = baseService.ApplyPagination(stripeOrdersRecord, start, length);
                }

                // Map data to DTOs
                var stripeRecordDto = MappingHelper.MapStripeOrderRecordsToDtos(stripeOrdersRecord);

                var response = new
                {
                    draw = (start / length) + 1,
                    data = stripeRecordDto,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));

            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }


        [CustomAuthorize(new EnumRoles[] {EnumRoles.Admin , EnumRoles.Employee})]
        [HttpGet("GetSubscriptionDataTableAsync")]
        public async Task<IActionResult> GetSubscriptionDataTableAsync(
            int start,
            int length,
            int subscriptionType = -1 , 
            string? sortColumn = "" ,
            string? sortDirection ="" , 
            string? searchValue="")
        {
            try
            {
                var userPackageListDto = await _userPackageService.GetUserPackageLists();

                // Initialize BaseService
                var baseService = new DatatableHelper<UserPackageListDto>();

                // Apply sorting
                userPackageListDto = baseService.ApplySorting(userPackageListDto, sortColumn, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    userPackageListDto = baseService.ApplyFiltering(userPackageListDto, x =>
                        (x.PackageName != null && x.PackageName.Trim().ToLower().Contains(searchValue.Trim().ToLower())) ||
                            (x.RemainingSessions != null && x.RemainingSessions.ToString().Contains(searchValue.ToLower())) ||
                            (x.PackageType != null && x.PackageType.ToString().Contains(searchValue.ToLower())) ||
                            (x.Customer != null && x.Customer.ToLower().Contains(searchValue.ToLower()))
                        ).ToList();
                }

                // Record counts
                int totalRows = userPackageListDto.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    userPackageListDto = baseService.ApplyPagination(userPackageListDto, start, length);
                }

                var response = new
                {
                    draw = (start / length) + 1,
                    data = userPackageListDto,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }

        }

        [HttpGet("GetUserEducationListAsync")]
        public async Task<IActionResult> GetUserEducationListAsync(
            int start,
            int length,
            string? sortColumn = "",
            string? sortDirection ="",
            string? searchValue="",
            string? DegreeName = "", 
            string? instituteName = "", 
            string? User = "")
        {
            try
            {
                var listOfEducation = await _userEducationRepo.GetUserEducationList();
                if (string.IsNullOrEmpty(DegreeName))
                {
                    listOfEducation = listOfEducation.Where(x => x.DegreeName == DegreeName.ToLower()).ToList();
                }
                if (string.IsNullOrEmpty(instituteName))
                {
                    listOfEducation = listOfEducation.Where(x => x.InstituteName == instituteName.ToLower()).ToList();
                }
                if (string.IsNullOrEmpty(User))
                {
                    listOfEducation = listOfEducation.Where(x => x.UserId == StringCipher.DecryptId(User)).ToList();
                }

                // Initialize BaseService
                var baseService = new DatatableHelper<UserEducation>();

                // Apply sorting
                listOfEducation = baseService.ApplySorting(listOfEducation, sortColumn, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    listOfEducation = baseService.ApplyFiltering(listOfEducation, x =>
                        x.DegreeName.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                        x.InstituteName != null && x.InstituteName.Trim().ToLower().Contains(searchValue.Trim().ToLower())
                        ).ToList();
                }

                // Record counts
                int totalRows = listOfEducation.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    listOfEducation = baseService.ApplyPagination(listOfEducation, start, length);
                }

                // Map data to DTOs
                var educationDto = MappingHelper.MapUserEducationRecordsToDtos(listOfEducation);

                var response = new
                {
                    draw = (start / length) + 1,
                    data = educationDto,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpGet("GetUserExperienceListAsync")]
        public async Task<IActionResult> GetUserExperienceListAsync(
            int start,
            int length,
            string? sortColumn = "",
            string? sortDirection = "",
            string? searchValue = "", 
            string? Title = "",
            string? Description = "",
            string? Organization = "")
        {
            try
            {
                var listOfExperience = await _userExperienceRepo.GetUserExperienceList();

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

                // Initialize BaseService
                var baseService = new DatatableHelper<UserExperience>();

                // Apply sorting
                listOfExperience = baseService.ApplySorting(listOfExperience, sortColumn, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    listOfExperience = baseService.ApplyFiltering(listOfExperience, x =>
                        x.Title.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                        x.Description != null && x.Description.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                        x.Organization != null && x.Organization.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                        x.Website != null && x.Website.Trim().ToLower().Contains(searchValue.Trim().ToLower())
                        ).ToList();
                }

                // Record counts
                int totalRows = listOfExperience.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    listOfExperience = baseService.ApplyPagination(listOfExperience, start, length);
                }

                // Map data to DTOs
                var experienceDto = MappingHelper.MapUserExperienceRecordsToDtos(listOfExperience);

                var response = new
                {
                    draw = (start / length) + 1,
                    data = experienceDto,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpGet("GetUserSocialProfileListAsync")]
        public async Task<IActionResult> GetUserSocialProfileListAsync(
            int start,
            int length,
            string? sortColumn = "",
            string? sortDirection = "",
            string? searchValue = "", 
            string? Title = "",
            string? Link = "", 
            string? Organization = "")
        {
            try
            {
                var listOfSocialProfile = await _userSocialProfileRepo.GetUserSocialProfileList();

                if (!string.IsNullOrEmpty(Title))
                {
                    listOfSocialProfile = listOfSocialProfile.Where(x => x.Title.ToLower().Contains(Title.ToLower())).ToList();
                }
                if (!string.IsNullOrEmpty(Link))
                {
                    listOfSocialProfile = listOfSocialProfile.Where(x => x.Link.ToLower().Contains(Link.ToLower())).ToList();
                }

                // Initialize BaseService
                var baseService = new DatatableHelper<UserSocialProfile>();

                // Apply sorting
                listOfSocialProfile = baseService.ApplySorting(listOfSocialProfile, sortColumn, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    listOfSocialProfile = baseService.ApplyFiltering(listOfSocialProfile, x =>
                        x.Title.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                        x.Link != null && x.Link.Trim().ToLower().Contains(searchValue.Trim().ToLower())
                        ).ToList();
                }

                // Record counts
                int totalRows = listOfSocialProfile.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    listOfSocialProfile = baseService.ApplyPagination(listOfSocialProfile, start, length);
                }

                // Map data to DTOs
                var socialProfileDto = MappingHelper.MapUserSocialProfileRecordsToDtos(listOfSocialProfile);
                
                var response = new
                {
                    draw = (start / length) + 1,
                    data = socialProfileDto,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpGet("GetUserSkillListAsync")]
        public async Task<IActionResult> GetUserSkillListAsync(
            int start,
            int length,
            string? sortColumn = "",
            string? sortDirection = "",
            string? searchValue = "", 
            string? SkillName = "")
        {
            try
            {
                var listOfSkill = await _userSkillRepo.GetAllActiveUserSkillsAsync();

                if (!string.IsNullOrEmpty(SkillName))
                {
                    listOfSkill = listOfSkill.Where(x => x.SkillName.ToLower().Contains(SkillName.ToLower())).ToList();
                }

                // Initialize BaseService
                var baseService = new DatatableHelper<UserSkill>();

                // Apply sorting
                listOfSkill = baseService.ApplySorting(listOfSkill, sortColumn, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    listOfSkill = baseService.ApplyFiltering(listOfSkill, x =>
                        x.SkillName.Trim().ToLower().Contains(searchValue.Trim().ToLower())).ToList();
                }


                // Record counts
                int totalRows = listOfSkill.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    listOfSkill = baseService.ApplyPagination(listOfSkill, start, length);
                }

                // Map data to DTOs
                var userSkillDto = MappingHelper.MapUserSkillRecordsToDtos(listOfSkill);

                var response = new
                {
                    draw = (start / length) + 1,
                    data = userSkillDto,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpGet("GetUserTagListAsync")]
        public async Task<IActionResult> GetUserTagListAsync(
            int start,
            int length,
            string? sortColumn = "",
            string? sortDirection = "",
            string? searchValue = "",
            string? TagName = "")
        {
            try
            {
                var listOfTag = await _userTagRepo.GetUserTagList();

                if (!string.IsNullOrEmpty(TagName))
                {
                    listOfTag = listOfTag.Where(x => x.TagName.ToLower().Contains(TagName.ToLower())).ToList();
                }

                // Initialize BaseService
                var baseService = new DatatableHelper<UserTag>();

                // Apply sorting
                listOfTag = baseService.ApplySorting(listOfTag, sortColumn, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    listOfTag = baseService.ApplyFiltering(listOfTag, x =>
                        x.TagName.Trim().ToLower().Contains(searchValue.Trim().ToLower())).ToList();
                }

                // Record counts
                int totalRows = listOfTag.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    listOfTag = baseService.ApplyPagination(listOfTag, start, length);
                }

                // Map data to DTOs
                var orderDtos = MappingHelper.MapUserTagRecordsToDtos(listOfTag);

                var response = new
                {
                    draw = (start / length) + 1,
                    data = orderDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }



        #region Helpers
        private int DecryptionId(string userId)
        {
            var validEncrypted = GeneralPurpose.ConversionEncryptedId(userId);
            return StringCipher.DecryptId(validEncrypted);
        }

        private async void CreateLogger(Exception ex)
        {
            await MailSender.SendErrorMessage($"URL: {_projectVariables.BaseUrl}<br/> Exception Message:  {ex.Message} <br/> Stack Trace: {ex.StackTrace}");
        }
        #endregion Helpers
    }

}
