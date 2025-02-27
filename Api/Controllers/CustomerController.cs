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
        private readonly IJwtUtils _jwtUtils;
        private readonly IOrderRepo _orderRepo;
        private readonly ProjectVariables _projectVariables;
        private readonly IPayPalGateWayService _paypalService;
        private readonly IRequestServiceRepo _requestServiceRepo;
        private readonly IUserPackageService _userPackageService;

        public CustomerController(IJwtUtils jwtUtils, IOrderRepo orderRepo, IOptions<ProjectVariables> options,
            IPayPalGateWayService paypalService, IRequestServiceRepo requestServiceRepo, IUserPackageService userPackageService)
        {
            _jwtUtils = jwtUtils;
            _orderRepo = orderRepo;
            _paypalService = paypalService;
            _projectVariables = options.Value;
            _requestServiceRepo = requestServiceRepo;
            _userPackageService = userPackageService;
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
                GeneralPurpose.CreateLogger(_projectVariables, ex);
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
                if (!await _requestServiceRepo.DeleteRequestService(decryptId))
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.DeletedMessage));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));
            }
        }

        #region CustomerPackage

        [HttpGet("GetUserSessionStatus/{userId}")]
        public async Task<IActionResult> GetUserSessionStatus(string userId)
        {
            try
            {
                var decryptId = StringCipher.DecryptionId(userId);
                var remainingSessionsResult = await _userPackageService.GetRemainingSessionCount(decryptId);

                if (remainingSessionsResult?.Data == null || remainingSessionsResult?.Status != true)
                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "No active session found.", null));

                var userPackage = remainingSessionsResult!.Data as UserPackage;
                if (userPackage == null)
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", "Invalid package data."));
                if(userPackage.PaidBy == "STRIPE")
                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Active sessions found.", userPackage.RemainingSessions));

                var packageRecordsResult = await _paypalService.GetPackageByUserId(decryptId);
                if (packageRecordsResult?.Status != true)
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

                var package = packageRecordsResult.Data as PayPalPackagesCheckOut;
                if (package == null || package.PaymentStatus != "completed")
                {
                    userPackage.IsActive = 0;
                    userPackage.DeletedAt = GeneralPurpose.DateTimeNow();

                    if (!await _userPackageService.SaveChangesAsync())
                        return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));

                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Payment incomplete. Session deactivated.", 0));
                }

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Active sessions found.", userPackage));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

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
                GeneralPurpose.CreateLogger(_projectVariables, ex);
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
            try
            {
                var getuserPackage = await _userPackageService.GetUserPackageByUserId(userId);
                if (getuserPackage!.Status == false)
                    return BadRequest(getuserPackage);
                return Ok(getuserPackage);
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpGet("GetOrderById/{id}")]
        public async Task<IActionResult> GetOrderById(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

                var decryptId = StringCipher.DecryptionId(id);
                var getOrder = await _orderRepo.GetOrderById(decryptId);

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
                GeneralPurpose.CreateLogger(_projectVariables, ex);
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

                var getResult = await _requestServiceRepo.AddRequestServiceReturnId(postAddRequestService);
                if (getResult != -1)
                {
                    return getResult;
                }
                return -1;
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return -1;
            }
        }
        #endregion
    }
}
