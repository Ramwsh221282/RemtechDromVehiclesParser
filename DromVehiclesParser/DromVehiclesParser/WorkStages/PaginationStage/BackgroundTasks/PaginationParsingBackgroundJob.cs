using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using DromVehiclesParser.Parsing.PaginationParsing.Models;
using DromVehiclesParser.WorkStages.Common.Parsers.Database;
using DromVehiclesParser.WorkStages.Common.Parsers.Models;
using DromVehiclesParser.WorkStages.Common.Stages.Database;
using DromVehiclesParser.WorkStages.Common.Stages.Models;
using DromVehiclesParser.WorkStages.PaginationStage.Database;
using DromVehiclesParser.WorkStages.PaginationStage.Models;
using ParsingSDK.Parsing;
using Quartz;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.Quartz;

namespace DromVehiclesParser.WorkStages.PaginationStage.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class PaginationParsingBackgroundJob(PaginationParsingBackgroundJobDependencies dependencies) : ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = dependencies.Logger.ForContext<PaginationParsingBackgroundJob>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        await using NpgSqlSession session = new(dependencies.NpgSql);
        await session.UseTransaction(ct);

        ParserWorkStageStoringImplementation.ParserWorkStageQuery stageQuery = new(
            Name: ParserWorkStageConstants.PAGINATION, 
            WithLock: true);
        
        Maybe<ParserWorkStage> stage = await ParserWorkStage.FromDb(session, stageQuery, ct);
        if (!stage.HasValue) return;
        
        WorkingParserStoringImplementation.WorkingParserQuery linksQuery = new(
            ParserId: stage.Value.Id,
            PaginationUncalculated: true, 
            RetryCountThreshold: 5, 
            WithLock: true);
        
        WorkingParserLink[] links = (await IEnumerable<WorkingParserLink>.LinksFromDb(session, linksQuery, ct)).ToArray();
        if (links.Length == 0)
        {
            ParserWorkStage catalogueStage = stage.Value.CatalogueStage();
            await catalogueStage.Update(session, ct);
            await session.UnsafeCommit(ct);
            return;
        }

        List<WorkingParserLinkPagination> paginations = [];
        
        _logger.Information("Starting parsing pagination.");
        for (int i = 0; i < links.Length; i++)
        {
            WorkingParserLink link = links[i];
            _logger.Information("Parsing pagination for URL: {Url}", link.Url);
            try
            {
                DromPagination pagination = await DromPagination.Extract(link.Url, dependencies.BrowserFactory);
                foreach (DromCataloguePage page in pagination.Pages(link.Url))
                    paginations.Add(WorkingParserLinkPagination.Create(link, page));
                link = link.MarkPaginationCalculated();
                _logger.Information("Parsing pagination for URL: {Url}. Completed.", link.Url);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to calculate pagination for URL: {Url}", link.Url);
                link = link.IncreaseRetryCount();
            }
            finally
            {
                links[i] = link;
            }
        }

        await links.UpdateMany(session);
        await paginations.SaveMany(session);
        await session.UnsafeCommit(ct);
        _logger.Information("Saved pagination urls for catalogue parsing.");
    }
}