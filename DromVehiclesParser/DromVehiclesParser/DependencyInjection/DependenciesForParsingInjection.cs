using ParsingSDK;
using ParsingSDK.TextProcessing;

namespace DromVehiclesParser.DependencyInjection;

public static class DependenciesForParsingInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterDependenciesForParsing()
        {
            services.RegisterTextTransformerBuilder();
            services.RegisterParserDependencies(options =>
            {
                options.DevelopmentMode = true;
                options.Headless = false;
            });
        }
    }
}