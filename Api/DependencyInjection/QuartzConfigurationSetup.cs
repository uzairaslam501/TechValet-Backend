using ITValet.Scheduler;
using Microsoft.Extensions.Options;
using Quartz;

namespace ITValet.DependencyInjection
{
    public class QuartzConfigurationSetup : IConfigureOptions<QuartzOptions>
    {
        public void Configure(QuartzOptions options)
        {
            var jobKey = JobKey.Create(nameof(FundTransferJob));
            var timeAvailabilityjobKey = JobKey.Create(nameof(SetTimeAvailabilityJob));
            var expiredAvailabilityJobKey = JobKey.Create(nameof(ExpiredAvailabilitySlotJob));
            var zoomMeetingJobKey = JobKey.Create(nameof(ZoomMeetingJob));

            options
              .AddJob<FundTransferJob>(jobBuilder => jobBuilder.WithIdentity(jobKey))
              .AddTrigger(trigger =>
                    trigger
                        .ForJob(jobKey)
                        .WithSimpleSchedule(
                            schedule => schedule.WithIntervalInSeconds(100).RepeatForever()));


            options
                .AddJob<SetTimeAvailabilityJob>(jobBuilder => jobBuilder.WithIdentity(timeAvailabilityjobKey))
                .AddTrigger(timeJobTrigger =>
                    timeJobTrigger
                        .ForJob(timeAvailabilityjobKey)
                        .WithCronSchedule("0 0 0 1 * ? *"));
            //.WithDailyTimeIntervalSchedule(scheduleBuilder =>
            //    scheduleBuilder
            //        .OnDaysOfTheWeek(DayOfWeek.Sunday)
            //        .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(0, 0)))); // Every Sunday

            options
                .AddJob<ExpiredAvailabilitySlotJob>(jobBuilder => jobBuilder.WithIdentity(expiredAvailabilityJobKey))
                .AddTrigger(expiredJobTrigger =>
                    expiredJobTrigger
                        .ForJob(expiredAvailabilityJobKey)
                        .WithSimpleSchedule(schedule =>
                            schedule.WithIntervalInHours(24 * 7) // One week interval
                        .RepeatForever()));

            options
                .AddJob<ZoomMeetingJob>(jobBuilder => jobBuilder.WithIdentity(zoomMeetingJobKey))
                .AddTrigger(zoomMeetingTrigger =>
                    zoomMeetingTrigger
                        .ForJob(zoomMeetingJobKey)
                        .WithSimpleSchedule(schedule =>
                            schedule.WithIntervalInMinutes(10) // 10-minute interval
                            .RepeatForever()));

        }

    }
}
