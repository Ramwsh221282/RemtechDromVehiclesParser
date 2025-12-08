using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;
using DromVehiclesParser.WorkStages.CatalogueStage.Database;
using DromVehiclesParser.WorkStages.CatalogueStage.Models;
using DromVehiclesParser.WorkStages.Common.Stages.Database;
using DromVehiclesParser.WorkStages.Common.Stages.Models;
using ParsingSDK.Parsing;
using Quartz;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.Quartz;

namespace DromVehiclesParser.WorkStages.CatalogueStage.BackgroundTask;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class CatalogueProcessingBackgroundTask(CatalogueProcessingBackgroundTaskDependencies dependencies) : ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = dependencies.Logger.ForContext<CatalogueProcessingBackgroundTaskDependencies>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        await using NpgSqlSession session = new(dependencies.NpgSql);
        await session.UseTransaction(ct);
        
        ParserWorkStageStoringImplementation.ParserWorkStageQuery stageQuery = new(
            Name: ParserWorkStageConstants.CATALOGUE,
            WithLock: true
            );

        Maybe<ParserWorkStage> stage = await ParserWorkStage.FromDb(session, stageQuery, ct);
        if (!stage.HasValue) return;

        WorkingCataloguePageStorageImplementation.WorkingCataloguePageQuery pagesQuery = new(
            CatalogueItemsNotFetched: true,
            RetryCountThreshold: 5,
            WithLock: true
            );

        WorkingCataloguePage[] pages = (await IEnumerable<WorkingCataloguePage>.GetMany(session, pagesQuery, ct)).ToArray();
        if (pages.Length == 0)
        {
            ParserWorkStage concrete = stage.Value.ConcreteStage();
            await concrete.Update(session, ct);
            await session.UnsafeCommit(ct);
            return;
        }

        DromCataloguePage[] pagesToProcess = pages.Select(p => DromCataloguePage.FromWorkingPage(p)).ToArray();

        for (int i = 0; i < pages.Length; i++)
        {
            WorkingCataloguePage processingPage = pages[i];
            DromCataloguePage pageToProcess = pagesToProcess[i];
            try
            {
                IEnumerable<DromCatalogueItem> results = await pageToProcess.ExtractItems(dependencies.BrowserFactory);
                await results.SaveMany(session);
                processingPage = processingPage.MarkItemsFetched();
                _logger.Information("""
                                    Received catalogue items from URL: 
                                    URL: {Url}
                                    Items fetched: {ItemsFetched}
                                    """, processingPage.Url, processingPage.CatalogueItemsFetched);
            }
            catch(Exception ex)
            {
                processingPage = processingPage.IncreaseRetryAmount();
                _logger.Information(ex, "Failed to parse catalogue items for URL: {Url} Retry count: {Retry}", 
                    pageToProcess.Url, 
                    processingPage.RetryCount);
            }
            finally
            {
                pages[i] = processingPage;
            }
        }

        await pages.UpdateMany(session);
        await session.UnsafeCommit(ct);
    }
}