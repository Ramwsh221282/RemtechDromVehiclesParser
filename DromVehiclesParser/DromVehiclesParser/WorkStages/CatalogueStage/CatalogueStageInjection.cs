using DromVehiclesParser.WorkStages.CatalogueStage.BackgroundTask;
using RemTech.SharedKernel.Infrastructure.Quartz;

namespace DromVehiclesParser.WorkStages.CatalogueStage;

public static class CatalogueStageInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterCatalogueStageContext()
        {
            services.AddSingleton<CatalogueProcessingBackgroundTaskDependencies>();
            services.AddSingleton<ICronScheduleJob, CatalogueProcessingBackgroundTask>();
        }
    }
}