using Microsoft.Extensions.Options;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Shared;

public sealed class DromVehicleDbUpgrader : AbstractDatabaseUpgrader
{
    public DromVehicleDbUpgrader(IOptions<NpgSqlOptions> options) : base(options)
    {
        OfAssembly(typeof(DromVehicleDbUpgrader).Assembly);
    }
}