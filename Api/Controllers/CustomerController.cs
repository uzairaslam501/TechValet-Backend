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
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));
            }
        }


        [HttpDelete("DeleteRequest/{serviceId}")]
        public async Task<IActionResult> DeleteRequestService(string serviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(serviceId))
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

                var decryptId = StringCipher.DecryptionId(serviceId);
                if (!await requestServiceRepo.DeleteRequestService(decryptId))
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.DeletedMessage));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));
            }
        }        

        #region CustomerPackage

        [HttpGet("GetUserPackageByUserId")]
        public async Task<IActionResult> GetUserPackageByUserId(int start, int length, string? sortColumnName = "", string? sortDirection = "",
            string? searchValue = "", string? userId = "")
        {
            try
            {
                var decryptUserId = StringCipher.DecryptionId(userId!);
                var userPackages = await _userPackageService.GetUserPackageListByUserId(decryptUserId);
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

                var dtoList = new List<UserPackageListViewModel>();
                foreach (var userPackage in userPackageList)
                {
                    var userPackageDtos = MappingHelper.MapUserPackageToDtos(userPackage);
                    dtoList.Add(userPackageDtos);
                };

                var response = new
                {
                    draw = (start / length) + 1,
                    data = dtoList,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, response));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpGet("GetPackageById/{id}")]
        public async Task<ActionResult> GetPackageById(string id)
        {
            var getuserPackage = await _userPackageService.GetUserPackageById(Convert.ToInt32(id));

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

            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "", obj));
        }

        [HttpGet("GetPackageByUserId/{userId}")]
        public async Task<ActionResult> GetPackageByUserId(string userId)
        {
            var getuserPackage = await _userPackageService.GetUserPackageByUserId(userId);
            if (getuserPackage!.Status == false)
                return BadRequest(getuserPackage);
            return Ok(getuserPackage);
        }

        [HttpGet("GetOrderById/{id}")]
        public async Task<IActionResult> GetOrderById(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

                var decryptId = Convert.ToInt32(id);
                var getOrder = await orderRepo.GetOrderById(decryptId);

                if(getOrder == null)
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

                List<OrderDtoList> udto = new List<OrderDtoList>();
                var order = new OrderDtoList
                {
                    Id = getOrder.Id.ToString(),
                    EncId = StringCipher.EncryptId(getOrder.Id),
                    OrderTitle = getOrder.OrderTitle,
                    StartDateTime = getOrder.StartDateTime != null ? getOrder.StartDateTime.ToString() : "",
                    EndDateTime = getOrder.EndDateTime != null ? getOrder.EndDateTime.ToString() : "",
                    OrderPrice = getOrder.OrderPrice.ToString(),
                    PackageBuyFrom = getOrder.PackageBuyFrom,
                    CapturedId = getOrder.CapturedId
                };

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, getOrder));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));
            }
        }

        #endregion

        #region Helpers
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
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return -1;
            }
        }
        #endregion
    }
}
