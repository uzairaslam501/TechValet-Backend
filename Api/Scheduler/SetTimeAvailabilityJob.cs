using ITValet.HelpingClasses;
using Microsoft.Extensions.Options;
using ITValet.Models;
using ITValet.Services;
using Quartz;

namespace ITValet.Scheduler
{
    public class SetTimeAvailabilityJob : IJob
    {
        private readonly IUserRepo _userService;
        private readonly ReturnUrls _returnUrls;
        private readonly ProjectVariables _projectVariables;
        private readonly INotificationRepo _notificationService;
        private readonly ILogger<SetTimeAvailabilityJob> _logger;
        private readonly IUserAvailableSlotRepo _userAvailableSlotRepo;
        public SetTimeAvailabilityJob(ILogger<SetTimeAvailabilityJob> logger,
            IUserRepo userService, INotificationRepo notificationService, IUserAvailableSlotRepo userAvailableSlotRepo,
            IOptions<ProjectVariables> projectVariables, IOptions<ReturnUrls> returnUrls)
        {
            _logger = logger;
            _userService = userService;
            _notificationService = notificationService;
            _userAvailableSlotRepo = userAvailableSlotRepo;
            _projectVariables = projectVariables.Value;
            _returnUrls = returnUrls.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                // Check if today is Sunday before proceeding
                //if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
                //{
                //}
                var userRecords = await _userService.GetValetRecord();

                if (userRecords != null && userRecords.Any())
                {
                    foreach (var user in userRecords)
                    {
                        Notification notificationObj = new Notification
                        {
                            UserId = user.Id,
                            Title = "Availability Alert",
                            IsRead = 0,
                            IsActive = (int)EnumActiveStatus.Active,
                            CreatedAt = GeneralPurpose.DateTimeNow(),
                            Url = $"{_returnUrls.AccountUrl}",
                            Description = "Update your availability time for an upcoming order.",
                            NotificationType = (int)NotificationType.TimeAvailabilityNotification
                        };

                        // Insert NotificationRecord against Each User and also send the email
                        bool isNotification = await _notificationService.AddNotification(notificationObj);
                        bool isEmailSent = await MailSender.SendEmailForSetTimeAvailability(user.UserName!, user.Email!, $"{_projectVariables.ReactUrl}{notificationObj.Url}");
                        bool isCreated = await _userAvailableSlotRepo.CreateEntriesForCurrentMonth(user.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message.ToString());
            }
        }
    }
}
