using DromVehiclesParser.Parsing.ParsingStages;
using DromVehiclesParser.Parsing.ParsingStages.StageProcessStrategies;
using DromVehiclesParser.ResultsExporing.TextFileExporting;
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
            services.AddSingleton<IExporter<TextFile>, TextFileExporter>();
            services.AddSingleton<ParsingStageDependencies>();
        }
    }
}