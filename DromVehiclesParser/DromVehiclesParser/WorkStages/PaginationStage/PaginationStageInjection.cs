using DromVehiclesParser.WorkStages.PaginationStage.BackgroundTasks;
using RemTech.SharedKernel.Infrastructure.Quartz;

namespace DromVehiclesParser.WorkStages.PaginationStage;

public static class PaginationStageInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterPaginationStageContext()
        {
            services.AddSingleton<PaginationParsingBackgroundJobDependencies>();
            services.AddSingleton<ICronScheduleJob, PaginationParsingBackgroundJob>();
        }
    }
}