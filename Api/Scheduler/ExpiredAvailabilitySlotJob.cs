using ITValet.HelpingClasses;
using ITValet.Services;
using Quartz;

namespace ITValet.Scheduler
{
    public class ExpiredAvailabilitySlotJob : IJob
    {
        private readonly IUserAvailableSlotRepo _slotService;
        private readonly ILogger<ExpiredAvailabilitySlotJob> _logger;
        public ExpiredAvailabilitySlotJob(IUserAvailableSlotRepo slotService, ILogger<ExpiredAvailabilitySlotJob> logger)
        {
            _slotService = slotService;
            _logger = logger;

        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                bool isPreviousSlotExpired = await _slotService.DeactivateExpiredSlots();
                if (!isPreviousSlotExpired)
                {
                    _logger.LogInformation("No expired slots found to deactivate.");
                }
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage($"An error occurred while executing the Slot Expired job: {ex.Message}");
            }
        }

    }
}
