using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

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
        public DatatableController(IJwtUtils _jwtUtils, IRequestServiceRepo _requestServiceRepo,
            IOrderRepo _orderRepo, INotificationService userPackageService, IUserRepo userRepo,
            IContactUsRepo contactUsRepo, IPayPalGateWayService payPalGateWayService,
            IUserEducationRepo userEducationRepo, IUserExperienceRepo userExperienceRepo,
            IUserSocialProfileRepo userSocialProfileRepo, IUserSkillRepo userSkillRepo, IUserTagRepo userTagRepo)
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
        }
        
        [HttpGet("GetRequestServicesDatatableByUserIdAsync")]
        public async Task<IActionResult> GetRequestServicesDatatableByUserIdAsync(int start, int length , string? sortColumnName ,string? sortDirection , string? searchValue , string? name)
        {
            try
            {
                UserClaims? getUserFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());

                var requestServices = await requestServiceRepo.GetRequestServiceByUserId((int)getUserFromToken.Id);

                if (!string.IsNullOrEmpty(name))
                {
                    requestServices = requestServices.Where(rs => rs.ServiceTitle.ToLower().Contains(name.ToLower())).ToList();
                }

                if (!string.IsNullOrEmpty(sortColumnName) && sortColumnName != "0")
                {
                    if (sortDirection == "asc")
                    {
                        requestServices = requestServices.OrderBy(rs => rs.GetType().GetProperty(sortColumnName)?.GetValue(rs)).ToList();
                    }
                    else
                    {
                        requestServices = requestServices.OrderByDescending(rs => rs.GetType().GetProperty(sortColumnName)?.GetValue(rs)).ToList();
                    }
                }
                else
                {
                    requestServices = requestServices.OrderByDescending(rs => rs.CreatedAt).ToList();
                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    requestServices = requestServices.Where(rs =>
                        rs.ServiceTitle.ToLower().Contains(searchValue.ToLower()) ||
                        (rs.ServiceDescription != null && rs.ServiceDescription.ToLower().Contains(searchValue.ToLower())) ||
                        (rs.ServiceLanguage != null && rs.ServiceLanguage.ToLower().Contains(searchValue.ToLower()))
                    ).ToList();
                }

                int totalRows = requestServices.Count();

               
                if(length != -1)
                {
                    requestServices = requestServices
                    .Skip(start * length)
                    .Take(length)
                    .ToList();
                }

                int totalRowsAfterFiltering = requestServices.Count();
                List<RequestServicesDto> requestServicesDtos = requestServices.Select(rs =>
                {
                    string userRegionRequestedTime = GeneralPurpose.regionChanged(Convert.ToDateTime(rs.CreatedAt), getUserFromToken.Timezone);
                    string userRegionRequestStartTime = rs.FromDateTime.HasValue
                        ? GeneralPurpose.regionChanged(Convert.ToDateTime(rs.FromDateTime), getUserFromToken.Timezone)
                        : "";
                    string userRegionRequestEndTime = rs.ToDateTime.HasValue
                        ? GeneralPurpose.regionChanged(Convert.ToDateTime(rs.ToDateTime), getUserFromToken.Timezone)
                        : "";

                    return new RequestServicesDto
                    {
                        Id = rs.Id,
                        EncId = StringCipher.EncryptId(rs.Id),
                        PrefferedServiceTime = rs.PrefferedServiceTime,
                        CategoriesOfProblems = rs.CategoriesOfProblems,
                        ServiceDescription = rs.ServiceDescription,
                        FromDateTime = rs.FromDateTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                        ToDateTime = rs.ToDateTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                        AppointmentTime = $"{userRegionRequestStartTime} - {userRegionRequestEndTime}",
                        ServiceLanguage = rs.ServiceLanguage,
                        RequestServiceType = rs.RequestServiceType.ToString(),
                        RequestServiceSkills = rs.RequestServiceSkills,
                        CreatedAt = userRegionRequestedTime
                    };
                }).ToList();

                return new ObjectResult(new
                {
                    draw = (start / length) + 1,
                    data = requestServicesDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto { Status = false, StatusCode = "406", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpGet("GetOrdersDatatableByUserId")]
        public async Task<IActionResult> GetOrdersDatatableByUserId(int start, int length, string? sortColumnName, string? sortDirection, string? searchValue)
        {
            try
            {
                UserClaims? getUserFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
                var orders = await orderRepo.GetOrderByUserId((int)getUserFromToken.Id);

                if (!string.IsNullOrEmpty(sortColumnName) && sortColumnName != "0")
                {
                    if (sortDirection == "asc")
                    {
                        orders = orders.OrderBy(o => o.GetType().GetProperty(sortColumnName)?.GetValue(o)).ToList();
                    }
                    else
                    {
                        orders = orders.OrderByDescending(o => o.GetType().GetProperty(sortColumnName)?.GetValue(o)).ToList();
                    }
                }
                else
                {
                    orders = orders.OrderByDescending(o => o.CreatedAt).ToList();
                }

                
                if (!string.IsNullOrEmpty(searchValue))
                {
                    orders = orders.Where(o =>
                        o.OrderTitle?.ToLower().Contains(searchValue.ToLower()) == true ||
                        o.OrderReason != null && o.OrderReason.Any(r => r.ReasonExplanation.ToLower().Contains(searchValue.ToLower()))
                    ).ToList();
                }

                int totalRows = orders.Count();

                
                int totalRowsAfterFiltering = orders.Count();
                if (totalRowsAfterFiltering>0 && start< totalRowsAfterFiltering)
                {
                    orders = orders.Skip(start * length).Take(length).ToList();
                }
                var orderDtos = new List<OrderDtoList>();
                foreach (var order in orders)
                {
                    var orderDto = new OrderDtoList
                    {
                        Id = order.Id.ToString(),
                        EncId = StringCipher.EncryptId(order.Id),
                        OrderTitle = order.OrderTitle,
                        StartDateTime = order.StartDateTime != null ? order.StartDateTime.ToString() : "",
                        EndDateTime = order.EndDateTime != null ? order.EndDateTime.ToString() : "",
                        OrderPrice = order.OrderPrice.ToString(),
                        IsDelivered = order.IsDelivered.ToString()
                    };

                    if (order.OrderReason != null && order.OrderReason.Any())
                    {
                        var reason = order.OrderReason.FirstOrDefault();
                        if (reason != null)
                        {
                            orderDto.OrderReasonId = reason.Id.ToString();
                            orderDto.OrderReasonExplanation = reason.ReasonExplanation;
                            orderDto.OrderReasonType = reason.ReasonType.ToString();
                            orderDto.OrderReasonIsActive = reason.IsActive.ToString();
                        }
                    }

                    orderDtos.Add(orderDto);
                }

                return Ok(new
                {
                    draw = (start / length) + 1,
                    data = orderDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
            }
        }

		[HttpGet("GetUserPackageDatatableAsync")]
		public async Task<IActionResult> GetUserPackageDatatableAsync(int start, int length, string? sortColumnName, string? sortDirection, string? searchValue, int? UserId)
		{
			try
			{
                UserClaims? getUserFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
                var userPackages = await _userPackageService.GetUserPackageListByUserId((int)getUserFromToken.Id);
				var userPackageList = userPackages.ToList(); 

				if (!string.IsNullOrEmpty(sortColumnName) && sortColumnName != "0")
				{
					if (sortDirection == "asc")
					{
						userPackageList = userPackageList.OrderBy(p => p.GetType().GetProperty(sortColumnName)?.GetValue(p)).ToList();
					}
					else
					{
						userPackageList = userPackageList.OrderByDescending(p => p.GetType().GetProperty(sortColumnName)?.GetValue(p)).ToList();
					}
				}
				else
				{
					userPackageList = userPackageList.OrderByDescending(p => p.StartDateTime).ToList();
				}

				// Apply searching
				if (!string.IsNullOrEmpty(searchValue))
				{
					string search = searchValue.ToLower().Trim();
					userPackageList = userPackageList.Where(p =>
						(p.PackageName != null && p.PackageName.ToLower().Contains(search)) ||
						(p.TotalSessions != null && p.TotalSessions.ToString().Contains(search)) ||
						(p.RemainingSessions != null && p.RemainingSessions.ToString().Contains(search)) ||
						(p.PackageType != null && p.PackageType.ToString().Contains(search.ToLower()))
					).ToList();
				}

				int totalRows = userPackageList.Count();

				
				int totalRowsAfterFiltering = userPackageList.Count();
                if (totalRowsAfterFiltering > 0 && start< totalRowsAfterFiltering)
                {
                    userPackageList = userPackageList.Skip(start * length).Take(length).ToList();
                }
				var userPackageDtos = new List<UserPackageListDto>();
				foreach (var userPackage in userPackageList)
				{
					var userPackageDto = new UserPackageListDto
					{
						Id = userPackage.Id,
						PackageName = userPackage.PackageName,
						PackageType = userPackage.PackageType,
						TotalSessions = userPackage.TotalSessions,
						RemainingSessions = userPackage.RemainingSessions,
						StartDateTime = userPackage.StartDateTime,
						EndDateTime = userPackage.EndDateTime,
						CustomerId = userPackage.CustomerId,
					};

					if (userPackage.CustomerId != null)
					{
						var customer = await _userRepo.GetUserById((int)userPackage.CustomerId);
						if (customer != null)
						{
							userPackageDto.Customer = $"{customer.FirstName} {customer.LastName}";
						}
					}

					userPackageDtos.Add(userPackageDto);
				}

				return Ok(new
				{
                    draw = (start / length) + 1,
                    data = userPackageDtos,
					recordsTotal = totalRows,
					recordsFiltered = totalRowsAfterFiltering
				});
			}
			catch (Exception ex)
			{
                await MailSender.SendErrorMessage(ex.Message);
				return Ok(new ResponseDto
				{
					Status = false,
					StatusCode = "500",
					Message = "Internal server error"
				});
			}
		}

		[HttpGet("GetOrdersDatatableByPackageId")]
		public async Task<IActionResult> GetOrdersDatatableByPackageId(int start, int length, string? sortColumnName, string? sortDirection, string? searchValue, int? packageId)
		{
			try
			{
				var orderList = await orderRepo.GetOrderByPackageId(packageId);
				var orderListMaterialized = orderList.ToList();  

				if (!string.IsNullOrEmpty(sortColumnName) && sortColumnName != "0")
				{
					if (sortDirection == "asc")
					{
						orderListMaterialized = orderListMaterialized.OrderBy(o => o.GetType().GetProperty(sortColumnName)?.GetValue(o)).ToList();
					}
					else
					{
						orderListMaterialized = orderListMaterialized.OrderByDescending(o => o.GetType().GetProperty(sortColumnName)?.GetValue(o)).ToList();
					}
				}
				else
				{
					orderListMaterialized = orderListMaterialized.OrderByDescending(o => o.StartDateTime).ToList();
				}

				// Apply search filter
				if (!string.IsNullOrEmpty(searchValue))
				{
					string search = searchValue.ToLower().Trim();
					orderListMaterialized = orderListMaterialized.Where(o =>
						(o.OrderTitle != null && o.OrderTitle.ToLower().Contains(search)) ||
						(o.OrderPrice != null && o.OrderPrice.ToString().Contains(search)) ||
						(o.IsDelivered != null && o.IsDelivered.ToString().Contains(search))
					).ToList();
				}

				int totalRows = orderListMaterialized.Count();
				
				
				int totalRowsAfterFiltering = orderListMaterialized.Count();
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    orderListMaterialized = orderListMaterialized.Skip(start * length).Take(length).ToList();
                }
                var orderDtos = new List<OrderDtoList>();
				foreach (var order in orderListMaterialized)
				{
					var orderDto = new OrderDtoList
					{
						Id = order.Id.ToString(),
						EncId = StringCipher.EncryptId(order.Id),
						OrderTitle = order.OrderTitle,
						StartDateTime = order.StartDateTime?.ToString() ?? "",
						EndDateTime = order.EndDateTime?.ToString() ?? "",
						OrderPrice = order.OrderPrice.ToString(),
						IsDelivered = order.IsDelivered.ToString(),
					};

					if (order.OrderReason != null && order.OrderReason.Count > 0)
					{
						foreach (var reason in order.OrderReason)
						{
							orderDto.OrderReasonId = reason.Id.ToString();
							orderDto.OrderReasonExplanation = reason.ReasonExplanation;
							orderDto.OrderReasonType = reason.ReasonType.ToString();
                            orderDto.OrderReasonIsActive = reason.IsActive.ToString();
                        }
					}

					orderDtos.Add(orderDto);
				}

				return Ok(new
				{
                    draw = (start / length) + 1,
                    data = orderDtos,
					recordsTotal = totalRows,
					recordsFiltered = totalRowsAfterFiltering
				});
			}
			catch (Exception ex)
			{
				await MailSender.SendErrorMessage(ex.Message);
				return Ok(new ResponseDto
				{
					Status = false,
					StatusCode = "500",
					Message = "Internal server error"
				});
			}
		}

        [HttpGet("GetContactListAsync")]
        public async Task<IActionResult> GetContactListAsync(int start, int length, string? sortColumnName, string? sortDirection, string? searchValue, string? Name = "", string? Email = "", string? subject = "")
        {
            try
            {
                var contactList = await _contactUsRepo.GetContactList();
                var contactListMaterialized = contactList.ToList();

                if (!string.IsNullOrEmpty(sortColumnName) && sortColumnName != "0")
                {
                    if (sortDirection == "asc")
                    {
                        contactListMaterialized = contactListMaterialized.OrderBy(c => c.GetType().GetProperty(sortColumnName)?.GetValue(c)).ToList();
                    }
                    else
                    {
                        contactListMaterialized = contactListMaterialized.OrderByDescending(c => c.GetType().GetProperty(sortColumnName)?.GetValue(c)).ToList();
                    }
                }
                else
                {
                    contactListMaterialized = contactListMaterialized.OrderByDescending(c => c.Name).ToList();
                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    string search = searchValue.ToLower().Trim();
                    contactListMaterialized = contactListMaterialized.Where(c =>
                        (c.Name != null && c.Name.ToLower().Contains(search)) ||
                        (c.Email != null && c.Email.ToLower().Contains(search)) ||
                        (c.Subject != null && c.Subject.ToLower().Contains(search))
                    ).ToList();
                }

                int totalRows = contactListMaterialized.Count();

                
                int totalRowsAfterFiltering = contactListMaterialized.Count();
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    contactListMaterialized = contactListMaterialized.Skip(start * length).Take(length).ToList();
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

                return Ok(new
                {
                    draw = (start / length) + 1,
                    data = contactDtos,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin, EnumRoles.Employee })]
        [HttpGet("GetUserListAsync")]
        public async Task<IActionResult> GetUserListAsync(int Role, int start, int length, string? pendingRec = "", string? Name = "", string? Email = "", string? Contact = "", string? Country = "",
    string? State = "", string? City = "", string? IsActive = "",  string? sortColumn = "", string? sortDirection = "asc", string? searchValue = "")
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

                if (!string.IsNullOrEmpty(Name))
                {
                    ulist = ulist.Where(x => x.FirstName.ToLower().Contains(Name.ToLower()) || x.LastName.ToLower().Contains(Name.ToLower()) || x.UserName.ToLower().Contains(Name.ToLower())).ToList();
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

                if (!string.IsNullOrEmpty(sortColumn))
                {
                    var isAscending = sortDirection == "asc";
                    ulist = isAscending
                        ? ulist.OrderBy(x => x.GetType().GetProperty(sortColumn)?.GetValue(x)).ToList()
                        : ulist.OrderByDescending(x => x.GetType().GetProperty(sortColumn)?.GetValue(x)).ToList();
                }

                int totalRows = ulist.Count();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    string search = searchValue.ToLower().Trim();
                    ulist = ulist.Where(x =>
                        x.Email.ToLower().Contains(search) ||
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

                int totalRowsAfterFiltering = ulist.Count();
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    ulist = ulist.Skip(start * length).Take(length).ToList();
                }
                
                var udto = ulist.Select(u => new UserListDto
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
                }).ToList();

                return Ok(new
                {
                    data = udto,
                    draw = (start / length) + 1,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
            }
        }

        [CustomAuthorize(new EnumRoles[] { EnumRoles.Admin })]
        [HttpGet("GetPayPalOrdersRecordAsync")]
        public async Task<IActionResult> GetPayPalOrdersRecordAsync(int start, int length, string? UserName = "", string? ItValet = "", string? searchValue = "", string? sortColumnName = "",string? sortDirection = "")
    {
            try
            {
                var paypalOrdersRecord = await _payPalGateWayService.GetPayPalOrdersRecord();

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

                if (!string.IsNullOrEmpty(searchValue))
                {
                    string search = searchValue.ToLower().Trim();
                    paypalOrdersRecord = paypalOrdersRecord.Where(x =>
                        (x.CustomerName != null && x.CustomerName.ToLower().Contains(search)) ||
                        (x.ITValet != null && x.ITValet.ToLower().Contains(search)) ||
                        (x.OrderTitle != null && x.OrderTitle.ToLower().Contains(search)) ||
                        (x.OrderPrice != null && x.OrderPrice.ToLower().Contains(search)) ||
                        (x.OrderStatus != null && x.OrderStatus.ToLower().Contains(search)) ||
                        (x.PaymentStatus != null && x.PaymentStatus.ToLower().Contains(search))
                    ).ToList();
                }

                int totalRows = paypalOrdersRecord.Count();
                if (!string.IsNullOrEmpty(sortColumnName))
                {
                    var propertyInfo = typeof(PayPalOrderDetailsForAdminDB).GetProperty(sortColumnName);
                    if (propertyInfo != null)
                    {
                        paypalOrdersRecord = sortDirection == "asc"
                            ? paypalOrdersRecord.OrderBy(x => propertyInfo.GetValue(x, null)).ToList()
                            : paypalOrdersRecord.OrderByDescending(x => propertyInfo.GetValue(x, null)).ToList();
                    }
                }

                int totalRowsAfterFiltering = paypalOrdersRecord.Count();
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    paypalOrdersRecord = paypalOrdersRecord.Skip(start * length).Take(length).ToList();
                }
                var paypalRecordDto = paypalOrdersRecord.Select(orderObj => new PayPalOrderDetailsForAdminDB
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
                }).ToList();

                return Ok(new
                {
                    data = paypalRecordDto,
                    draw = (start / length) + 1,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
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

                if (!string.IsNullOrEmpty(sortColumn))
                {
                    if (sortColumnDirection == "asc")
                    {
                        unclaimedPaymentRecord = unclaimedPaymentRecord
                            .OrderBy(x => x.GetType().GetProperty(sortColumn)?.GetValue(x))
                            .ToList();
                    }
                    else
                    {
                        unclaimedPaymentRecord = unclaimedPaymentRecord
                            .OrderByDescending(x => x.GetType().GetProperty(sortColumn)?.GetValue(x))
                            .ToList();
                    }
                }

                int totalRows = unclaimedPaymentRecord.Count();

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
                int totalRowsAfterFiltering = unclaimedPaymentRecord.Count();
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    unclaimedPaymentRecord = unclaimedPaymentRecord.Skip(start * length).Take(length).ToList();
                }
                var paypalRecordDto = unclaimedPaymentRecord.Select(unclaimedObj => new PayPalUnclaimedTransactionDetailsForAdminDB
                {
                    ITValetName = unclaimedObj.ITValetName,
                    OrderTitle = unclaimedObj.OrderTitle,
                    Reason = unclaimedObj.Reason,
                    TransactionStatus = unclaimedObj.TransactionStatus,
                    UnclaimedAmountStatus = unclaimedObj.UnclaimedAmountStatus,
                    CustomerName = unclaimedObj.CustomerName,
                    OrderEncId = unclaimedObj.OrderEncId,
                    PayPalEmailAccount = unclaimedObj.PayPalEmailAccount,
                }).ToList();

                return new ObjectResult(new
                {
                    data = paypalRecordDto,
                    draw = (start / length) + 1,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
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

                if (!string.IsNullOrEmpty(sortColumn))
                {
                    if (sortColumnDirection == "asc")
                    {
                        paypalTransactionRecord = paypalTransactionRecord
                            .OrderBy(x => x.GetType().GetProperty(sortColumn)?.GetValue(x))
                            .ToList();
                    }
                    else
                    {
                        paypalTransactionRecord = paypalTransactionRecord
                            .OrderByDescending(x => x.GetType().GetProperty(sortColumn)?.GetValue(x))
                            .ToList();
                    }
                }
                int totalRows = paypalTransactionRecord.Count();
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

                int totalRowsAfterFiltering = paypalTransactionRecord.Count();
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    paypalTransactionRecord = paypalTransactionRecord.Skip(start * length).Take(length).ToList();
                }
                var paypalRecordDto = paypalTransactionRecord.Select(transactionObj => new PayPalTransactionDetailsForAdminDB
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
                }).ToList();

                return new ObjectResult(new
                {
                    data = paypalRecordDto,
                    draw = (start / length) + 1,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
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

                if (!string.IsNullOrEmpty(sortColumn))
                {
                    if (sortColumnDirection == "asc")
                    {
                        stripeOrdersRecord = stripeOrdersRecord
                            .OrderBy(x => x.GetType().GetProperty(sortColumn)?.GetValue(x))
                            .ToList();
                    }
                    else
                    {
                        stripeOrdersRecord = stripeOrdersRecord
                            .OrderByDescending(x => x.GetType().GetProperty(sortColumn)?.GetValue(x))
                            .ToList();
                    }
                }

                int totalRows = stripeOrdersRecord.Count();
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
                int totalRowsAfterFiltering = stripeOrdersRecord.Count();
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    stripeOrdersRecord = stripeOrdersRecord.Skip(start * length).Take(length).ToList();
                }
                var stripeRecordDto = stripeOrdersRecord.Select(orderObj => new StripeOrderDetailForAdminDb
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
                }).ToList();
                return new ObjectResult(new
                {
                    data = stripeRecordDto,
                    draw = (start / length) + 1,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
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

                if (!string.IsNullOrEmpty(searchValue))
                {
                    userPackageListDto = userPackageListDto
                        .Where(x =>
                            (x.PackageName != null && x.PackageName.Trim().ToLower().Contains(searchValue.Trim().ToLower())) ||
                            (x.RemainingSessions != null && x.RemainingSessions.ToString().Contains(searchValue.ToLower())) ||
                            (x.PackageType != null && x.PackageType.ToString().Contains(searchValue.ToLower())) ||
                            (x.Customer != null && x.Customer.ToLower().Contains(searchValue.ToLower()))
                        ).ToList();
                }

                int totalRows = userPackageListDto.Count();

                if (!string.IsNullOrEmpty(sortColumn))
                {
                    PropertyInfo propertyInfo = typeof(UserPackageListDto).GetProperty(sortColumn);
                    if (propertyInfo != null)
                    {
                        if (sortDirection == "asc")
                        {
                            userPackageListDto = userPackageListDto.OrderBy(x => propertyInfo.GetValue(x)).ToList();
                        }
                        else
                        {
                            userPackageListDto = userPackageListDto.OrderByDescending(x => propertyInfo.GetValue(x)).ToList();
                        }
                    }
                }

                int totalRowsAfterFiltering = userPackageListDto.Count();
                if (totalRowsAfterFiltering> 0 && start< totalRowsAfterFiltering)
                {
                    userPackageListDto = userPackageListDto.Skip(start * length).Take(length).ToList();
                }
                return new ObjectResult(new
                {
                    data = userPackageListDto,
                    draw = (start/length) + 1,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
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
                if (sortColumn != "" && sortColumn != null)
                {
                    if (sortColumn != "0")
                    {
                        if (sortDirection == "asc")
                        {
                            listOfEducation = listOfEducation.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                        }
                        else
                        {
                            listOfEducation = listOfEducation.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                        }
                    }
                }
                int totalrows = listOfEducation.Count();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    listOfEducation = listOfEducation.Where(x => x.DegreeName.Trim().ToLower().Contains(searchValue.Trim().ToLower()) ||
                                        x.InstituteName != null && x.InstituteName.Trim().ToLower().Contains(searchValue.Trim().ToLower())
                                        ).ToList();
                }
                int totalrowsafterfilterinig = listOfEducation.Count();
                if (totalrowsafterfilterinig > 0 && start < totalrowsafterfilterinig)
                {
                    listOfEducation = listOfEducation.Skip(start * length).Take(length).ToList();
                }
                var educationDto = listOfEducation.Select(educationList => new UserEducationDto
                {
                    Id = educationList.Id,
                    UserEducationEncId = StringCipher.EncryptId(educationList.Id),
                    DegreeName = educationList.DegreeName,
                    InstituteName = educationList.DegreeName,
                    StartDate = educationList.StartDate.ToString(),
                    EndDate = educationList.EndDate.ToString(),
                    UserId = educationList.UserId
                }).ToList();
                return new ObjectResult(new
                {
                    data = educationDto,
                    draw = (start / length) + 1,
                    recordsTotal = totalrows,
                    recordsFiltered = totalrowsafterfilterinig
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
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
                if (sortColumn != "" && sortColumn != null)
                {
                    if (sortColumn != "0")
                    {
                        if (sortDirection == "asc")
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
                if (totalrowsafterfilterinig > 0 && start < totalrowsafterfilterinig)
                {
                    listOfExperience = listOfExperience.Skip(start * length).Take(length).ToList();
                }
                var experienceDto = listOfExperience.Select(experienceList => new UserExperienceDto
                {
                    Id = experienceList.Id,
                    UserExperienceEncId = StringCipher.EncryptId(experienceList.Id),
                    Title = experienceList.Title,
                    Description = experienceList.Description,
                    ExperienceFrom = experienceList.ExperienceFrom.ToString(),
                    ExperienceTo = experienceList.ExperienceTo.ToString(),
                    Organization = experienceList.Organization,
                    Website = experienceList.Website,
                    UserId = experienceList.UserId
                }).ToList();

                return new ObjectResult(new
                {
                    data = experienceDto,
                    draw = (start / length) + 1,
                    recordsTotal = totalrows,
                    recordsFiltered = totalrowsafterfilterinig
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
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

                if (sortColumn != "" && sortColumn != null)
                {
                    if (sortColumn != "0")
                    {
                        if (sortDirection == "asc")
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
                if (totalrowsafterfilterinig > 0 && start < totalrowsafterfilterinig)
                {
                    listOfSocialProfile = listOfSocialProfile.Skip(start * length).Take(length).ToList();
                }
                var socialProfileDto = listOfSocialProfile.Select(socialProfileList => new UserSocialProfileDto
                {
                    Id = socialProfileList.Id,
                    UserSocialProfileEncId = StringCipher.EncryptId(socialProfileList.Id),
                    Title = socialProfileList.Title,
                    Link = socialProfileList.Link,
                    UserId = socialProfileList.UserId
                }).ToList();

                return new ObjectResult(new
                {
                    data = socialProfileDto,
                    draw = (start / length) + 1,
                    recordsTotal = totalrows,
                    recordsFiltered = totalrowsafterfilterinig
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
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

                if (sortColumn != "" && sortColumn != null)
                {
                    if (sortColumn != "0")
                    {
                        if (sortDirection == "asc")
                        {
                            listOfSkill = listOfSkill.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                        }
                        else
                        {
                            listOfSkill = listOfSkill.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                        }
                    }
                }
                int totalrows = listOfSkill.Count();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    listOfSkill = listOfSkill.Where(x => x.SkillName.Trim().ToLower().Contains(searchValue.Trim().ToLower())
                                        ).ToList();
                }
                int totalrowsafterfilterinig = listOfSkill.Count();
                if (totalrowsafterfilterinig > 0 && start < totalrowsafterfilterinig)
                {
                    listOfSkill = listOfSkill.Skip(start * length).Take(length).ToList();
                }

                var userSkillDto = listOfSkill.Select(skillList => new UserSkillDto
                {
                    Id = skillList.Id,
                    UserSkillEncId = StringCipher.EncryptId(skillList.Id),
                    SkillName = skillList.SkillName,
                    UserId = skillList.UserId
                }).ToList();

                return new ObjectResult(new
                {
                    data = userSkillDto,
                    draw = (start / length) + 1,
                    recordsTotal = totalrows,
                    recordsFiltered = totalrowsafterfilterinig
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
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
                if (sortColumn != "" && sortColumn != null)
                {
                    if (sortColumn != "0")
                    {
                        if (sortDirection == "asc")
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
                if (totalrowsafterfilterinig > 0 && start < totalrowsafterfilterinig)
                {
                    listOfTag = listOfTag.Skip(start * length).Take(length).ToList();
                }
                var tagListDto = listOfTag.Select(userTagList => new UserTagDto
                {
                    Id = userTagList.Id,
                    UserTagEncId = StringCipher.EncryptId(userTagList.Id),
                    TagName = userTagList.TagName,
                    UserId = userTagList.UserId
                }).ToList();

                return new ObjectResult(new
                {
                    data = tagListDto,
                    draw = (start / length) + 1,
                    recordsTotal = totalrows,
                    recordsFiltered = totalrowsafterfilterinig
                });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
            }
        }

    }
}
