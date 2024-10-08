using ITValet.HelpingClasses;
using ITValet.Models;
using ITValet.Services;
using Quartz;

namespace ITValet.Scheduler
{
    public class SetTimeAvailabilityJob : IJob
    {
        private readonly ILogger<SetTimeAvailabilityJob> _logger;
        private readonly IUserRepo _userService;
        private readonly INotificationRepo _notificationService;
        private readonly IUserAvailableSlotRepo _userAvailableSlotRepo;
        public SetTimeAvailabilityJob(ILogger<SetTimeAvailabilityJob> logger,
            IUserRepo userService, INotificationRepo notificationService, IUserAvailableSlotRepo userAvailableSlotRepo)
        {
            _logger = logger;
            _userService = userService;
            _notificationService = notificationService;
            _userAvailableSlotRepo = userAvailableSlotRepo;
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
                                Url = ProjectVariables.AccountUrl,
                                CreatedAt = GeneralPurpose.DateTimeNow(),
                                Description = "Update your availability time for an upcoming order.",
                                NotificationType = (int)NotificationType.TimeAvailabilityNotification
                            };

                            // Insert NotificationRecord against Each User and also send the email
                            bool isNotification = await _notificationService.AddNotification(notificationObj);
                            bool isEmailSent = await MailSender.SendEmailForSetTimeAvailability(user.UserName, user.Email);
                            bool ss = await _userAvailableSlotRepo.CreateEntriesForCurrentMonth(user.Id);
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
