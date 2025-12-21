namespace DromVehiclesParser.Parsing.ParsingStages.StageProcessStrategies;

public static class SleepingParserStageImplementation
{
    extension(ParsingStage)
    {
        public static ParsingStage Sleep => (deps, _) =>
        {
            Serilog.ILogger logger = deps.Logger.ForContext<ParsingStage>();
            logger.Information("sleeping.");
            return Task.CompletedTask;
        };
    }
}