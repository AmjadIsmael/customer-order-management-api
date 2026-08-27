using Quartz;

namespace CustomerOrderManagement.API.Jobs;

public static class JobsExtensions
{
    public static IServiceCollection AddApplicationJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JobSettings>(configuration.GetSection(JobSettings.SectionName));

        var settings = configuration.GetSection(JobSettings.SectionName).Get<JobSettings>()
            ?? new JobSettings();

        services.AddQuartz(quartz =>
        {
            var jobKey = new JobKey(nameof(ExpireStalePendingOrdersJob));

            quartz.AddJob<ExpireStalePendingOrdersJob>(options => options.WithIdentity(jobKey));

            quartz.AddTrigger(options => options
                .ForJob(jobKey)
                .WithIdentity($"{nameof(ExpireStalePendingOrdersJob)}-trigger")
                .WithCronSchedule(settings.ExpireStalePendingOrders.CronExpression));
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }
}
