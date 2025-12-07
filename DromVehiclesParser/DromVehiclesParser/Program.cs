using DromVehiclesParser.ParserRegistration;
using DromVehiclesParser.Shared;
using ParsingSDK;
using RemTech.SharedKernel.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterSharedDependencies();
builder.Services.RegisterParserRegistrationContext();
builder.Services.RegisterParserDependencies();
builder.Services.RegisterSharedInfrastructure();

WebApplication app = builder.Build();


app.Run();

namespace DromVehiclesParser
{
    public partial class Program
    {
        
    }
}