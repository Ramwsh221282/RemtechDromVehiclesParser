namespace DromVehiclesParser.WorkStages.Common.Stages.Models;

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
    }
}