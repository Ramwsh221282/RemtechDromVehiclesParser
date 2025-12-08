using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Shared;

public static class SharedDependenciesInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterSharedDependencies()
        {
            services.RegisterDbUpgrader();
        }

        private void RegisterDbUpgrader()
        {
            services.AddTransient<IDbUpgrader, DromVehicleDbUpgrader>();
        }
    }
}