using ITValet.Scheduler;
using Quartz;

namespace ITValet.DependencyInjection
{
    public static class DependencyInjection
    {
        public static void AddQuartzDependencyInjection(this IServiceCollection service)
        {
            service.AddQuartz(option =>
            {
                option.UseMicrosoftDependencyInjectionJobFactory();
            });

            service.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });
            service.AddTransient<SetTimeAvailabilityJob>();
            service.AddTransient<ExpiredAvailabilitySlotJob>();
            service.AddTransient<ZoomMeetingJob>();
            service.ConfigureOptions<QuartzConfigurationSetup>();
        }
    }
}
