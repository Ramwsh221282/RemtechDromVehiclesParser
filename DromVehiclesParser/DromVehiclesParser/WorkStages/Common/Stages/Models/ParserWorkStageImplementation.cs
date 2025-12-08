namespace DromVehiclesParser.WorkStages.Common.Stages.Models;

public static class ParserWorkStageImplementation
{
    extension(ParserWorkStage stage)
    {
        public ParserWorkStage PaginationStage()
        {
            return stage with { StageName = ParserWorkStageConstants.PAGINATION };
        }
    }
}