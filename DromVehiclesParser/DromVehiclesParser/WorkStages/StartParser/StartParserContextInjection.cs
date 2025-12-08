using DromVehiclesParser.WorkStages.StartParser.BackgroundTasks;

namespace DromVehiclesParser.WorkStages.StartParser;

public static class StartParserContextInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterStartParserContext()
        {
            services.AddHostedService<StartParserWorkStageListener>();
            services.AddSingleton<StartParserWorkStageListenerDependencies>();
        }
    }
}