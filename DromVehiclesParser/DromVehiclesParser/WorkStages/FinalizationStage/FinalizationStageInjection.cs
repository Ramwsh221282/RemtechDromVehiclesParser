using DromVehiclesParser.WorkStages.FinalizationStage.BackgroundTasks;
using RemTech.SharedKernel.Infrastructure.Quartz;

namespace DromVehiclesParser.WorkStages.FinalizationStage;

public static class FinalizationStageInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterFinalizationStageContext()
        {
            services.AddSingleton<FinalizationStageTaskDependencies>();
            services.AddSingleton<ICronScheduleJob, FinalizationStageTask>();
        }
    }
}