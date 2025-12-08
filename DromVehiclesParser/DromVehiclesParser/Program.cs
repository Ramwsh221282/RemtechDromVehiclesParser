using DromVehiclesParser.ParserRegistration;
using DromVehiclesParser.Shared;
using DromVehiclesParser.WorkStages.StartParser;
using ParsingSDK;
using RemTech.SharedKernel.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterSharedDependencies();
builder.Services.RegisterParserRegistrationContext();
builder.Services.RegisterParserDependencies();
builder.Services.RegisterSharedInfrastructure();
builder.Services.RegisterStartParserContext();

WebApplication app = builder.Build();

app.Run();

namespace DromVehiclesParser
{
    public partial class Program
    {
        
    }
}