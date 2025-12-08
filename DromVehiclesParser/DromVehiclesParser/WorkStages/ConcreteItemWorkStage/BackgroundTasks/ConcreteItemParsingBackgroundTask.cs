using DromVehiclesParser.ItemsPublishing.Database;
using DromVehiclesParser.ItemsPublishing.Models;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;
using DromVehiclesParser.WorkStages.CatalogueStage.Database;
using DromVehiclesParser.WorkStages.Common.Stages.Database;
using DromVehiclesParser.WorkStages.Common.Stages.Models;
using ParsingSDK.Parsing;
using Quartz;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.Quartz;

namespace DromVehiclesParser.WorkStages.ConcreteItemWorkStage.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class ConcreteItemParsingBackgroundTask(ConcreteItemParsingBackgroundTaskDependencies dependencies) : ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = dependencies.Logger.ForContext<ConcreteItemParsingBackgroundTask>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        await using NpgSqlSession session = new(dependencies.Npgsq);
        await session.UseTransaction(ct);

        ParserWorkStageStoringImplementation.ParserWorkStageQuery stageQuery = new(Name: ParserWorkStageConstants.CONCRETE);
        Maybe<ParserWorkStage> stage = await ParserWorkStage.FromDb(session, stageQuery, ct);
        if (!stage.HasValue) return;
        
        CatalogueItemsStoringImplementation.DromCatalogueItemQuery itemsQuery = new(
            UnprocessedOnly: true, 
            RetryCountThreshold: 5, 
            WithLock: true, 
            Limit: 20);
        
        DromCatalogueItem[] items = (await IEnumerable<DromCatalogueItem>.GetManyFromDb(session, itemsQuery, ct)).ToArray();
        if (items.Length == 0)
        {
            ParserWorkStage finalization = stage.Value.FinalizationStage();
            await finalization.Update(session, ct);
            await session.UnsafeCommit(ct);
            _logger.Information("Switched to stage: {Stage}", finalization.StageName);
            return;
        }

        List<DromPendingItem> results = [];
        
        for (int i = 0; i < items.Length; i++)
        {
            DromCatalogueItem item = items[i];
            
            try
            {
                DromPendingItem pendingItem = await item.CreatePendingItem(dependencies.BrowserFactory);
                results.Add(pendingItem);
                item = item.MarkProcessed();
                _logger.Information("Processed item: {Url}", item.Url);
            }
            catch
            {
                item = item.IncreaseRetryAmount();
            }
            finally
            {
                items[i] = item;
            }
        }

        await items.UpdateMany(session);
        await results.SaveMany(session);
        await session.UnsafeCommit(ct);
    }
}