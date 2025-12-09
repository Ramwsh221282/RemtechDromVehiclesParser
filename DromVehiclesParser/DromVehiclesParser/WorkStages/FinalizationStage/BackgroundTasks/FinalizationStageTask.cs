using DromVehiclesParser.ItemsPublishing.Database;
using DromVehiclesParser.ItemsPublishing.Models;
using DromVehiclesParser.Shared.NpgSql;
using DromVehiclesParser.WorkStages.Common.Stages.Database;
using DromVehiclesParser.WorkStages.Common.Stages.Models;
using ParsingSDK.Parsing;
using ParsingSDK.Publishing.TextPublishing;
using ParsingSDK.TextProcessing;
using Quartz;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.Quartz;

namespace DromVehiclesParser.WorkStages.FinalizationStage.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class FinalizationStageTask(FinalizationStageTaskDependencies dependencies) : ICronScheduleJob
{
    private readonly TextFileSaveOptions _options = new(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results")
        );
    private readonly Serilog.ILogger _logger = dependencies.Logger.ForContext<FinalizationStageTask>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        await using NpgSqlSession session = new(dependencies.NpgSql);
        await session.UseTransaction();

        ParserWorkStageStoringImplementation.ParserWorkStageQuery stageQuery =
            new(Name: ParserWorkStageConstants.FINALIZATION, WithLock: true);

        Maybe<ParserWorkStage> stage = await ParserWorkStage.FromDb(session, stageQuery);
        if (!stage.HasValue) return;

        PendingItemsStoringImplementation.PendingItemsQuery itemsQuery = new(Limit: 30, WithLock: true);
        DromPendingItem[] items = (await IEnumerable<DromPendingItem>.GetMany(session, itemsQuery, ct)).ToArray();
        if (items.Length == 0)
        {
            await session.ClearAllTables(ct);
            await session.UnsafeCommit(ct);
            _logger.Information("Finalization stage finished. No items to publish left.");
            return;
        }
        
        Directory.CreateDirectory(_options.FilePath);

        ITextTransformer transformer = dependencies.TextTransformerBuilder
            .UsePunctuationCleaner()
            .UseNewLinesCleaner()
            .UseEmojiCleaner()
            .UseSpacesCleaner()
            .Build();
        
        for (int i = 0; i < items.Length; i++)
        {
            TextFileSaveOptions saveOptions = new TextFileSaveOptions(
                Path.Combine(_options.FilePath, $"{Guid.NewGuid()}.txt")
                );
            
            string content = $"""
                              {transformer.TransformText(string.Join(" ", items[i].DescriptionList))}
                              """;
            
            await new TextFilePublisher().Publish(content, saveOptions, ct);
            _logger.Information("Published item with content: {Content}", content);
        }

        await items.DeleteMany(session);
        await session.UnsafeCommit(ct);
        _logger.Information("Processed {Count} messages", items.Length);
    }
}