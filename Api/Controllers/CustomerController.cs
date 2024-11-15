using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    [CustomAuthorize]
    public class CustomerController : ControllerBase
    {
        private readonly IUserRepo userRepo;
        private readonly IRequestServiceRepo requestServiceRepo;
        private readonly IJwtUtils jwtUtils;
        private readonly ProjectVariables projectVariables;
        private readonly INotificationService _userPackageService;
        private readonly IOrderRepo orderRepo;

        public CustomerController(IUserRepo _userRepo, IJwtUtils _jwtUtils, IOptions<ProjectVariables> options,
            IRequestServiceRepo _requestServiceRepo, INotificationService userPackageService, IOrderRepo _orderRepo)
        {
            userRepo = _userRepo;
            jwtUtils = _jwtUtils;
            projectVariables = options.Value;
            requestServiceRepo = _requestServiceRepo;
            _userPackageService = userPackageService;
            orderRepo = _orderRepo;
        }

        [HttpPost("PostAddRequestService")]
        public async Task<IActionResult> PostAddRequestService(PostAddRequestServices postAddRequestService)
        {
            try
            {
                var obj = new RequestService();
                var getServiceId = await AddRequestService(postAddRequestService);

                if (postAddRequestService?.RequestServiceSkills!.Length > 0)
                {
                    List<string> requestSkills = postAddRequestService.RequestServiceSkills!.Split(",").ToList();
                }
                return Ok(new ResponseDto() { Data = StringCipher.EncryptId(getServiceId), Status = true, StatusCode = "200", Message = GlobalMessages.SuccessMessage });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message.ToString());
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        private async Task<int> AddRequestService(PostAddRequestServices postAddRequestServices)
        {
            try
            {
                var postAddRequestService = new RequestService();
                postAddRequestService.ServiceTitle = postAddRequestServices.ServiceTitle;
                postAddRequestService.PrefferedServiceTime = postAddRequestServices.PrefferedServiceTime;
                postAddRequestService.CategoriesOfProblems = postAddRequestServices.CategoriesOfProblems;
                postAddRequestService.ServiceDescription = postAddRequestServices.ServiceDescription;
                postAddRequestService.FromDateTime = Convert.ToDateTime(postAddRequestServices.FromDateTime);
                postAddRequestService.ToDateTime = Convert.ToDateTime(postAddRequestServices.ToDateTime);
                postAddRequestService.RequestServiceSkills = postAddRequestServices.RequestServiceSkills;
                postAddRequestService.ServiceLanguage = postAddRequestServices.ServiceLanguage;
                postAddRequestService.RequestedServiceUserId = Convert.ToInt32(postAddRequestServices.RequestedServiceUserId);
                postAddRequestService.RequestServiceType = Convert.ToInt32(postAddRequestServices.RequestServiceType);
                postAddRequestService.IsActive = 1;
                postAddRequestService.CreatedAt = GeneralPurpose.DateTimeNow();

                var getResult = await requestServiceRepo.AddRequestServiceReturnId(postAddRequestService);
                if (getResult != -1)
                {
                    return getResult;
                }
                return -1;
            }
            catch(Exception ex)
            {
                await MailSender.SendErrorMessage(projectVariables.BaseUrl +" ----------<br>"+ ex.Message.ToString()+"---------------"+ex.StackTrace);
                return -1;
            }
        }

        [HttpPut("PostUpdateRequestService")]
        public async Task<IActionResult> PostUpdateRequestService(PostUpdateRequestService postUpdateRequestService)
        {
            try
            {
                var getDecryptedId = StringCipher.DecryptId(postUpdateRequestService.RequestServiceEncId!);


                RequestService? obj = await requestServiceRepo.GetRequestServiceById(getDecryptedId);

                if (obj == null)
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = GlobalMessages.RecordNotFound });
                }
                if (!await requestServiceRepo.ValidateServiceTitle(postUpdateRequestService.ServiceTitle!, (int)obj.RequestedServiceUserId!))
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.DuplicateServiceTitle });
                }

                obj.ServiceTitle = !string.IsNullOrEmpty(postUpdateRequestService.ServiceTitle) ? postUpdateRequestService.ServiceTitle : obj.ServiceTitle;
                obj.PrefferedServiceTime = !string.IsNullOrEmpty(postUpdateRequestService.PrefferedServiceTime) ? postUpdateRequestService.PrefferedServiceTime : obj.PrefferedServiceTime;
                obj.CategoriesOfProblems = !string.IsNullOrEmpty(postUpdateRequestService.CategoriesOfProblems) ? postUpdateRequestService.CategoriesOfProblems : obj.CategoriesOfProblems;
                obj.ServiceDescription = !string.IsNullOrEmpty(postUpdateRequestService.ServiceDescription) ? postUpdateRequestService.ServiceDescription : obj.ServiceDescription;
                obj.FromDateTime = !string.IsNullOrEmpty(postUpdateRequestService.FromDateTime) ? Convert.ToDateTime(postUpdateRequestService.FromDateTime) : obj.FromDateTime;
                obj.ToDateTime = !string.IsNullOrEmpty(postUpdateRequestService.ToDateTime) ? Convert.ToDateTime(postUpdateRequestService.ToDateTime) : obj.ToDateTime;
                obj.RequestServiceSkills = !string.IsNullOrEmpty(postUpdateRequestService.RequestServiceSkills) ? postUpdateRequestService.RequestServiceSkills : obj.RequestServiceSkills;
                obj.ServiceLanguage = !string.IsNullOrEmpty(postUpdateRequestService.ServiceLanguage) ? postUpdateRequestService.ServiceLanguage : obj.ServiceLanguage;
                obj.RequestServiceType = !string.IsNullOrEmpty(postUpdateRequestService.RequestServiceType) ? Convert.ToInt32(postUpdateRequestService.RequestServiceType) : obj.RequestServiceType;

                if (!await requestServiceRepo.UpdateRequestService(obj))
                {
                    return Ok(new ResponseDto() { Data = obj, Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
                }

                return Ok(new ResponseDto() { Data = obj, Status = true, StatusCode = "200", Message = GlobalMessages.UpdateMessage });
            }
            catch(Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message);
                return Ok(new ResponseDto() {Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        #region CustomerPackage

        [HttpGet("GetUserPackageByUserId")]
        public async Task<ActionResult> GetUserPackageByUserId(int? UserId)
        {
            var getuserPackage = await _userPackageService.GetUserPackageByUserId(UserId);

            if (getuserPackage == null)
            {
                return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = GlobalMessages.InsufficientRemainingSession });
            }

            UserPackageDto obj = new UserPackageDto()
            {
                Id = getuserPackage.Id,
                PackageName = getuserPackage.PackageName,
                PackageType = getuserPackage.PackageType,
                RemainingSessions = getuserPackage.RemainingSessions,
                StartDateTime = getuserPackage.StartDateTime,
                EndDateTime = getuserPackage.EndDateTime,
                TotalSessions = getuserPackage.TotalSessions,
                CustomerId = getuserPackage.CustomerId,
                PackagePaidBy = getuserPackage.PaidBy
            };

            return Ok(new ResponseDto() { Data = obj, Status = true, StatusCode = "200", Message = "Record Fetch Successfully" });
        }

        [HttpPost("GetUserPackageDatatable")]
        public async Task<IActionResult> GetUserPackageDatatable(int? UserId)
        {
            try
            {
                var userPackagelist = new List<UserPackage>();

                userPackagelist = (List<UserPackage>)await _userPackageService.GetUserPackageListByUserId(UserId);


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
                            userPackagelist = userPackagelist.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                        }
                        else
                        {
                            userPackagelist = userPackagelist.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                        }
                    }
                }
                int totalrows = userPackagelist.Count();

                if (!string.IsNullOrEmpty(searchValue)) // incase we use search filter ahead
                {
                    userPackagelist = userPackagelist.Where(x => x.PackageName != null && x.PackageName.Trim().ToLower().Contains(searchValue.Trim().ToLower())
                                        || (x.TotalSessions != null && x.TotalSessions.ToString().Contains(searchValue.ToLower()))
                                        || (x.RemainingSessions != null && x.RemainingSessions.ToString().Contains(searchValue.ToLower()))
                                        || (x.PackageType != null && x.PackageType.ToString().Contains(searchValue.ToLower()))
                                        ).ToList();
                }
                int totalrowsafterfilterinig = userPackagelist.Count();

                userPackagelist = userPackagelist.Skip(skip).Take(pageSize).ToList();
                List<UserPackageListDto> udto = new List<UserPackageListDto>();
                foreach (UserPackage userPackage in userPackagelist)
                {
                    UserPackageListDto obj = new UserPackageListDto()
                    {
                        Id=userPackage.Id,
                        PackageName = userPackage.PackageName,
                        PackageType = userPackage.PackageType,
                        TotalSessions = userPackage.TotalSessions,
                        RemainingSessions = userPackage.RemainingSessions,
                        StartDateTime = userPackage.StartDateTime,
                        EndDateTime = userPackage.EndDateTime,
                        CustomerId=userPackage.CustomerId,
                    };
                    if (userPackage.CustomerId != null)
                    {
                        var getCustomerName = await userRepo.GetUserById((int)userPackage.CustomerId);
                        obj.Customer = getCustomerName.FirstName + " " + getCustomerName.LastName;
                    }
                    udto.Add(obj);
                }
                return new ObjectResult(new { data = udto, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig });

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost("GetOrdersDatatableByPackageId")]
        public async Task<IActionResult> GetOrdersDatatableByPackageId(int? packageId)
        {
            UserClaims? getUsetFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
            var ulist = await orderRepo.GetOrderByPackageId(packageId);

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
                    }
                }
                udto.Add(order);
            }

            return new ObjectResult(new { data = udto, draw = Request.Form["draw"].FirstOrDefault(), recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig });
        }

        #endregion
    }
}
