using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface INotificationRepo
    {
        Task<Notification?> GetNotificationById(int id);
        Task<IEnumerable<Notification>> GetNotificationList();
        Task<IEnumerable<Notification>> GetNotificationListByUserId(int userId);
        Task<bool> AddNotification(Notification notification);
        Task<bool> UpdateNotification(Notification notification);
        Task<bool> DeleteNotification(int id);
        Task<bool> MarkNotification(int NotificationId);
        Task<bool> MarkAllNotification(int UserId);
        Task<bool> DeleteAllNotification(int userId);
        Task<bool> DeleteAllNotificationByType(int UserId, int NotificationType);
        Task<bool> MarkAllNotificationByType(int UserId, int NotificationType);
        Task<int> GetUnreadNotificationCountByUserId(int userId, string? title = "");
    }

    public class NotificationRepo : INotificationRepo
    {
        private readonly AppDbContext _context;
        
        public NotificationRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }
        
        public async Task<bool> AddNotification(Notification notification)
        {
            try
            {
                _context.Notification.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteNotification(int id)
        {
            try
            {
                Notification? Notification = await GetNotificationById(id);

                if (Notification != null)
                {
                    Notification.IsActive = 0;
                    Notification.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateNotification(Notification);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<Notification?> GetNotificationById(int id)
        {
            return await _context.Notification.FindAsync(id);
        }

        public async Task<IEnumerable<Notification>> GetNotificationList()
        {
            return await _context.Notification.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetNotificationListByUserId(int userId)
        {
            return await _context.Notification.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.UserId == userId).OrderByDescending(x=>x.Id).ToListAsync();
        }        
        
        public async Task<int> GetUnreadNotificationCountByUserId(int userId, string? title = "")
        {
            if (!string.IsNullOrEmpty(title))
            {
                return await _context.Notification
                    .Where(x => x.IsActive == (int)EnumActiveStatus.Active &&
                    x.UserId == userId &&
                    x.IsRead == 0 &&
                    x.Title.ToLower().Contains(title.ToLower())).CountAsync();
            }
            else
            {
                return await _context.Notification
                    .Where(x => x.IsActive == (int)EnumActiveStatus.Active &&
                    x.UserId == userId &&
                    x.IsRead == 0 && !x.Title.ToLower().Contains("Message".ToLower())).CountAsync();
            }
        }

        public async Task<bool> UpdateNotification(Notification notification)
        {
            try
            {
                _context.Entry(notification).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        public async Task<bool> MarkNotification(int NotificationId)
        {
            try
            {
                var notification = await _context.Notification.FirstOrDefaultAsync(x => x.IsActive == 1 && x.Id == NotificationId);
                notification.IsRead = 1;
                return await UpdateNotification(notification);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> MarkAllNotification(int UserId)
        {
            var notifications = await _context.Notification.Where(x => x.UserId == UserId && x.IsRead == 0).ToListAsync();
            foreach (var i in notifications)
            {
                i.IsRead = 1;
                await UpdateNotification(i);
            }

            return true;
        }

        public async Task<bool> DeleteAllNotification(int userId)
        {
            var notifications = _context.Notification.Where(x => x.IsActive == 1 && x.UserId == userId).ToList();

            foreach (var i in notifications)
            {
                await DeleteNotification(i.Id);
            }

            return true;
        }
        
        public async Task<bool> DeleteAllNotificationByType(int UserId, int NotificationType)
        {
            var notifications = new List<Notification>();
            if (NotificationType == -1)
            {
                notifications = await _context.Notification.Where(x => x.IsActive == 1 && x.UserId == UserId).ToListAsync();
            }
            else
            {
                notifications = await _context.Notification.Where(x => x.IsActive == 1 && x.UserId == UserId && x.NotificationType == NotificationType).ToListAsync();
            }

            try
            {
                if (notifications.Count() != 0)
                {
                    if (notifications.Count() <= 400)
                    {
                        _context.Notification.RemoveRange(notifications);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _context.Notification.RemoveRange(notifications.Take(400));
                        await _context.SaveChangesAsync();
                        await DeleteAllNotificationByType(UserId, NotificationType);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        public async Task<bool> MarkAllNotificationByType(int UserId, int NotificationType)
        {
            var notifications = new List<Notification>();

            if (NotificationType == -1)
            {
                notifications = await _context.Notification.Where(x => x.UserId == UserId && x.IsRead == 0 && x.IsActive == 1).ToListAsync();
            }
            else
            {
                notifications = await _context.Notification.Where(x => x.UserId == UserId && x.IsRead == 0 && x.IsActive == 1 && x.NotificationType == NotificationType).ToListAsync();
            }

            foreach (var item in notifications)
            {
                item.IsRead = 1;
                //await UpdateNotificationAsync(i);
            }
            _context.UpdateRange(notifications);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
