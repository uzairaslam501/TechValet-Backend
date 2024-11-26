using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.Models;
using ITValet.NotificationHub;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : Controller
    {

        private readonly INotificationRepo notificationRepo;
        private readonly IUserRepo userRepo;

        public NotificationController(INotificationRepo notificationRepo, IUserRepo userRepo)
        {
            this.notificationRepo = notificationRepo;
            this.userRepo = userRepo;
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

        [HttpGet("GetNotifications")]
        public async Task<IActionResult> GetNotifications(string UserId, int isRead = -1, int NotificationType = -1)
        {

            var notificationList = await notificationRepo.GetNotificationListByUserId(Convert.ToInt32(UserId));
            var loggedInUser = await userRepo.GetUserById(Convert.ToInt32(UserId));

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
                    Url = notification.Url,
                    IsRead = (int)notification.IsRead,
                    NotificationType = notification.NotificationType,
                };
                viewNotificationDto.CreatedAt = GeneralPurpose.regionChanged(Convert.ToDateTime(notification.CreatedAt), loggedInUser.Timezone);

                viewNotificationDtoList.Add(viewNotificationDto);
            }
            //viewNotificationDtoList = viewNotificationDtoList.Take(10).ToList(); 
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

        [HttpGet("MarkAllNotifications")]
        public async Task<IActionResult> MarkAllNotifications(string UserId)
        {
            bool chkNotification = await notificationRepo.MarkAllNotification(Convert.ToInt32(UserId));
            return Ok(chkNotification);
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

        [HttpDelete]
        [Route("DeleteAllNotifications")]
        public async Task<IActionResult> DeleteAllNotifications(string UserId)
        {
            bool chkNotification = await notificationRepo.DeleteAllNotification(Convert.ToInt32(UserId));
            if (!chkNotification)
            {
                return Ok(new { Status = false, StatusCode = "500", Message = "Record inserted failed." });

            }
            return Ok(new { Status = true, StatusCode = "200", Message = "Record inserted successfully." });

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
