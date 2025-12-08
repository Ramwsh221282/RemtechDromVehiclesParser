using DromVehiclesParser.WorkStages.ConcreteItemWorkStage.BackgroundTasks;
using RemTech.SharedKernel.Infrastructure.Quartz;

namespace DromVehiclesParser.WorkStages.ConcreteItemWorkStage;

public static class ConcreteItemInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterConcreteItemsContext()
        {
            services.AddSingleton<ICronScheduleJob, ConcreteItemParsingBackgroundTask>();
            services.AddSingleton<ConcreteItemParsingBackgroundTaskDependencies>();
        }
    }
}