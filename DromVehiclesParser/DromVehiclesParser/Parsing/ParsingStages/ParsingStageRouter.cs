using System.Diagnostics;
using DromVehiclesParser.Stages.Models;

namespace DromVehiclesParser.Parsing.ParsingStages;

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