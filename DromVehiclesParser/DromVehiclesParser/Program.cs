using DromVehiclesParser.DependencyInjection;
using RemTech.SharedKernel.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterDependenciesForParsing();
builder.Services.RegisterInfrastructureDependencies();

WebApplication app = builder.Build();

app.Services.ApplyDatabaseMigrations();

app.Run();

namespace DromVehiclesParser
{
    public partial class Program
    {
        
    }
}