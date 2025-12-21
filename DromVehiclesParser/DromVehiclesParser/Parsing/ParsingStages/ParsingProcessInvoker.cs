using DromVehiclesParser.Parsers.Database;
using DromVehiclesParser.Parsers.Models;
using DromVehiclesParser.Parsing.ParsingStages.Database;
using DromVehiclesParser.Parsing.ParsingStages.Models;
using DromVehiclesParser.Parsing.ParsingStages.StageProcessStrategies;
using ParsingSDK.Parsing;
using Quartz;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.Quartz;

namespace DromVehiclesParser.Parsing.ParsingStages;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class ParsingProcessInvoker(ParsingStageDependencies dependencies) : ICronScheduleJob
{
    private ParsingStageDependencies Dependencies { get; } = dependencies;
    
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        if (!await EnsureHasWorkingParsers(ct))
        {
            Dependencies.Logger.Information("No working parsers detected. Not invoking parser process.");
            return;
        }
        
        ParserWorkStage stage = await GetCurrentParsingStage(ct);
        ParsingStage process = ParsingStageRouter.ResolveByStageName(stage);
        
        Dependencies.Logger.Information("Invoking stage - {Name}.", stage.StageName);
        await process(Dependencies, ct);
        Dependencies.Logger.Information("Parsing stage - {Name} completed.", stage.StageName);
    }

    private async Task<ParserWorkStage> GetCurrentParsingStage(CancellationToken ct)
    {
        await using NpgSqlSession session = new(Dependencies.NpgSql);
        var query = new ParserWorkStageStoringImplementation.ParserWorkStageQuery();
        Maybe<ParserWorkStage> stage = await ParserWorkStage.FromDb(session, query, ct);
        return stage.Value;
    }

    private async Task<bool> EnsureHasWorkingParsers(CancellationToken ct)
    {
        await using NpgSqlSession session = new(Dependencies.NpgSql);
        return await WorkingParser.Exists(session, ct);
    }
}