using Quartz;
using RemTech.SharedKernel.Infrastructure.Quartz;

namespace DromVehiclesParser.DependencyInjection;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class DummyCronScheduleJob : ICronScheduleJob
{
    public Task Execute(IJobExecutionContext context)
    {
        return Task.CompletedTask;
    }
}