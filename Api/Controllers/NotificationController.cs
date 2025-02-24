using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.Models;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : Controller
    {

        private readonly INotificationRepo notificationRepo;
        private readonly IUserRepo userRepo;
        private readonly ProjectVariables _projectVariables;

        public NotificationController(INotificationRepo notificationRepo, IUserRepo userRepo, IOptions<ProjectVariables> options)
        {
            this.notificationRepo = notificationRepo;
            this.userRepo = userRepo;
            _projectVariables = options.Value;
        }

        [HttpPost("PostAddNotification")]
        public async Task<IActionResult> PostAddNotification(CreateNotificationDto notificationDto)
        {
            Notification notification = new Notification()
            {
                UserId = Convert.ToInt32(notificationDto.UserId),
                Title = notificationDto.Title,
                Description = notificationDto.Description,
                Url = notificationDto.Url,
                IsRead = 0,
                IsActive = 1,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
            if (!await notificationRepo.AddNotification(notification))
            {
                return Ok(new  { Status = false, StatusCode = "500", Message = "Record insertion failed." });

            }
            return Ok(new {Status = true, StatusCode = "200", Message = "Record inserted successfully."});

        }

        [HttpGet("GetNotificationsCount")]
        public async Task<IActionResult> GetNotificationsCount(string UserId, string? Title = "", int? IsRead = -1)
        {
            int notificationCount = 0;
            notificationCount = await notificationRepo.GetUnreadNotificationCountByUserId(Convert.ToInt32(UserId), Title);
            return Ok(new { Status = true, StatusCode = "200", Data = notificationCount });
        }

        [HttpGet("GetNotifications/{userId}")]
        public async Task<IActionResult> GetNotifications(string userId, int isRead = -1,
            int NotificationType = -1, int take = -1)
        {
            var decrypt = StringCipher.DecryptionId(userId);
            var notificationList = await notificationRepo.GetNotificationListByUserId(decrypt);
            var loggedInUser = await userRepo.GetUserById(decrypt);

            if (isRead == 1)
            {
                notificationList = notificationList.Where(x => x.IsRead == 1).ToList();
            }
            if (isRead == 0)
            {
                notificationList = notificationList.Where(x => x.IsRead == 0).ToList();
            }
            if (NotificationType != -1)
            {
                if (NotificationType == 3)
                {
                    notificationList = notificationList.Where(x => x.NotificationType == 3).ToList();
                }
                else
                {
                    notificationList = notificationList.Where(x => x.NotificationType == 2).ToList();
                }
            }

            List<ViewNotificationDto> viewNotificationDtoList = new List<ViewNotificationDto>();

            foreach (Notification notification in notificationList)
            {
                ViewNotificationDto viewNotificationDto = new ViewNotificationDto()
                {
                    NotificationId = notification.Id.ToString(),
                    UserId = notification.UserId.ToString(),
                    Title = notification.Title,
                    Description = notification.Description,
                    Url = $"{_projectVariables.ReactUrl}{notification.Url}",
                    IsRead = (int)notification.IsRead,
                    NotificationType = notification.NotificationType,
                };
                viewNotificationDto.CreatedAt = Convert.ToDateTime(GeneralPurpose.regionChanged(Convert.ToDateTime(notification.CreatedAt), loggedInUser.Timezone)).ToString("yyyy-MMM-dd hh:mm tt");

                viewNotificationDtoList.Add(viewNotificationDto);
            }
            if(take != -1)
                viewNotificationDtoList = viewNotificationDtoList.Take(take).ToList();
            else
                viewNotificationDtoList = viewNotificationDtoList.ToList();

            return Ok(new { Status = true, StatusCode = "200", Data = viewNotificationDtoList });
        }

        [HttpGet("MarkNotification")]
        public async Task<IActionResult> MarkNotification(string NotificationId)
        {
            bool chkNotification = await notificationRepo.MarkNotification(Convert.ToInt32(NotificationId));
            return Ok(chkNotification);
        }

        [HttpPatch("MarkNotifications/{NotificationId}")]
        public async Task<IActionResult> MarkNotifications(string NotificationId)
        {
            bool chkNotification = await notificationRepo.MarkNotification(Convert.ToInt32(NotificationId));
            return Ok(new { Status = true, StatusCode = "200", Data = chkNotification });
        }

        [HttpGet("MarkAllAsRead/{userId}")]
        public async Task<IActionResult> MarkAllNotifications(string userId)
        {
            var response = await notificationRepo.MarkAllNotification(userId);
            if(response.Status == false)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete]
        [Route("DeleteNotification")]
        public async Task<IActionResult> DeleteNotification(string NotificationId)
        {
            bool chkNotification = await notificationRepo.DeleteNotification(Convert.ToInt32(NotificationId));
            if (!chkNotification)
            {
                return Ok(new  { Status = false, StatusCode = "500", Message = "Record deletion failed."});
            }
            return Ok(new { Status = true, StatusCode = "200", Message = "Record deleted successfully." });
        }

        [HttpDelete("DeleteAll/{userId}")]
        public async Task<IActionResult> DeleteAllNotifications(string userId)
        {
            var response = await notificationRepo.DeleteAllNotification(userId);
            if (response.Status == false)
            {
                return BadRequest(response);
            }
            return Ok(response);

        }

        [HttpDelete]
        [Route("DeleteAllNotificationsByType")]
        public async Task<IActionResult> DeleteAllNotificationsByType(string UserId, int NotificationType = -1)
        {
            bool chkNotification = await notificationRepo.DeleteAllNotificationByType(Convert.ToInt32(UserId), NotificationType);
            if (!chkNotification)
            {
                return Ok(new { Status = false, StatusCode = "500", Message = "Record deletion failed." });
            }
            return Ok(new { Status = true, StatusCode = "200", Message = "Record deleted successfully." });
        }

        [HttpGet]
        [Route("ReadAllNotificationsByType")]
        public async Task<IActionResult> ReadAllNotificationsByType(string UserId, int NotificationType = -1)
        {
            bool chkNotification = await notificationRepo.MarkAllNotificationByType(Convert.ToInt32(UserId), NotificationType);
            if (!chkNotification)
            {
                return Ok(new { Status = false, StatusCode = "500", Message = "Record deletion failed." });

            }
            return Ok(new { Status = true, StatusCode = "200", Message = "All Notifications marked Read successfully." });
        }
    }
}
