using DromVehiclesParser.Parsing.ParsingStages;
using DromVehiclesParser.Shared;
using RemTech.SharedKernel.Infrastructure;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.Quartz;

namespace DromVehiclesParser.DependencyInjection;

public static class InfrastructureInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterInfrastructureDependencies()
        {
            services.AddTransient<ICronScheduleJob, DummyCronScheduleJob>();
            services.AddSingleton<ICronScheduleJob, ParsingProcessInvoker>();
            services.AddTransient<IDbUpgrader, DromVehicleDbUpgrader>();
            services.RegisterSharedInfrastructure();
            services.AddQuartzServices();
        }
    }
}