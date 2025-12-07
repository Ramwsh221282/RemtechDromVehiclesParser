using ParsingSDK;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterParserDependencies();

WebApplication app = builder.Build();


app.Run();

namespace DromVehiclesParser
{
    public partial class Program
    {
        
    }
}