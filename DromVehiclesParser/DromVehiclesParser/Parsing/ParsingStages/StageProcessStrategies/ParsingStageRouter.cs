using System.Diagnostics;
using DromVehiclesParser.Parsing.ParsingStages.Models;

namespace DromVehiclesParser.Parsing.ParsingStages.StageProcessStrategies;

public static class ParsingStageRouter
{
    public static ParsingStage ResolveByStageName(ParserWorkStage stage)
    {
        string name = stage.StageName;
        return name switch
        {
            ParserWorkStageConstants.PAGINATION => ParsingStage.Pagination,
            ParserWorkStageConstants.CATALOGUE => ParsingStage.CatalogueAdvertisementsExtraction,
            ParserWorkStageConstants.CONCRETE => ParsingStage.ExtractAdvertisementsFromItsPage,
            ParserWorkStageConstants.FINALIZATION => ParsingStage.Finalization,
            ParserWorkStageConstants.SLEEP => ParsingStage.Sleep,
            _ => throw new UnreachableException($"Unknown stage: {name}.")
        };
    }
}