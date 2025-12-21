namespace DromVehiclesParser.Parsing.ParsingStages.Models;

public static class ParserWorkStageImplementation
{
    extension(ParserWorkStage stage)
    {
        public ParserWorkStage PaginationStage()
        {
            return stage with { StageName = ParserWorkStageConstants.PAGINATION };
        }

        public ParserWorkStage CatalogueStage()
        {
            return stage with { StageName = ParserWorkStageConstants.CATALOGUE };
        }

        public ParserWorkStage ConcreteStage()
        {
            return stage with { StageName = ParserWorkStageConstants.CONCRETE };
        }

        public ParserWorkStage FinalizationStage()
        {
            return stage with { StageName = ParserWorkStageConstants.FINALIZATION };
        }
        
        public ParserWorkStage SleepStage()
        {
            return stage with { StageName = ParserWorkStageConstants.SLEEP };
        }
    }
}