using DromVehiclesParser.Commands.ExtractPagedUrls;
using DromVehiclesParser.Parsers.Database;
using DromVehiclesParser.Parsers.Models;
using DromVehiclesParser.Parsing.CatalogueParsing.Extensions;
using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using DromVehiclesParser.Parsing.ParsingStages.Database;
using DromVehiclesParser.Parsing.ParsingStages.Models;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Parsing.ParsingStages.StageProcessStrategies;

public static class PagedUrlsExtractingStage
{
    extension(ParsingStage)
    {
        public static ParsingStage Pagination => async (deps, ct) =>
        {
            deps.Deconstruct(out BrowserFactory browsers, out NpgSqlConnectionFactory npgSql, out Serilog.ILogger dLogger, out _);
            Serilog.ILogger logger = dLogger.ForContext<ParsingStage>();
            await using NpgSqlSession session = new(npgSql);

            Maybe<ParserWorkStage> stage = await GetPaginationStage(session, ct);
            if (StageIsNotPaginationStage(stage)) return;
            
            WorkingParserLink[] links = await GetParserLinksForPaginationUrlsExtraction(session, ct);
            if (CanSwitchNextStage(links))
            {
                await SwitchNextStage(stage, session, logger, ct);
                await FinishTransaction(session, logger, ct);
            }
            
            await CollectPagedUrlsFromLinks(links, session, browsers, logger);
            await FinishTransaction(session, logger, ct);
        };
    }

    private static async Task CollectPagedUrlsFromLinks(
        WorkingParserLink[] links, 
        NpgSqlSession session, 
        BrowserFactory browsers, 
        Serilog.ILogger logger)
    {
        IBrowser browser = await browsers.ProvideBrowser();
        
        for (int i = 0; i < links.Length; i++)
        {
            WorkingParserLink link = links[i];

            IExtractPagedUrlsCommand command = new ExtractPagedUrlsCommandCommand(() => browser.GetPage())
                .UseLogging(logger);
            
            WorkingParserLink updatedLink = await CollectPagedUrlsFromLink(command, link, session, logger);
            links[i] = updatedLink;
            await Task.Delay(TimeSpan.FromSeconds(5)); // forced delay to avoid get blocked.
        }

        await links.UpdateMany(session);    
        await browser.DestroyAsync();
    }

    private static async Task<WorkingParserLink> CollectPagedUrlsFromLink(
        IExtractPagedUrlsCommand command, 
        WorkingParserLink link, 
        NpgSqlSession session,
        Serilog.ILogger logger)
    {
        try
        {
            DromCataloguePage[] pages = [..await command.Extract(link.Url)];
            await pages.PersistMany(session);
            logger.Information("Collected pages for link: {Url}", link.Url);
            return link.MarkProcessed();
        }
        catch (EvaluationFailedException)
        {
            logger.Warning("Failed to extract pages for link: {Url}", link.Url);
            return link;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to extract pages for link: {Url}", link.Url);
            return link.IncreaseRetryCount();
        }
    }
    
    private static async Task FinishTransaction(NpgSqlSession session, Serilog.ILogger logger, CancellationToken ct)
    {
        try
        {
            await session.UnsafeCommit(ct);
            logger.Information("Transaction committed");
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Failed to commit transaction");
        }
    }
    
    private static async Task SwitchNextStage(Maybe<ParserWorkStage> stage, NpgSqlSession session, Serilog.ILogger logger, CancellationToken ct)
    {
        ParserWorkStage catalogueStage = stage.Value.CatalogueStage();
        await catalogueStage.Update(session, ct);
        logger.Information("Switched to stage: {Name}", catalogueStage.StageName);
    }
    
    private static bool CanSwitchNextStage(WorkingParserLink[] links)
    {
        return links.Length == 0;
    }
    
    private static async Task<WorkingParserLink[]> GetParserLinksForPaginationUrlsExtraction(
        NpgSqlSession session,
        CancellationToken ct
    )
    {
        WorkingParserLinkQuery query = new(UnprocessedOnly: true, RetryLimit: 5, WithLock: true);
        WorkingParserLink[] links = await WorkingParserLink.GetMany(session, query, ct);
        return links;
    }
    
    private static async Task<Maybe<ParserWorkStage>> GetPaginationStage(NpgSqlSession session, CancellationToken ct)
    {
        ParserWorkStageStoringImplementation.ParserWorkStageQuery query = new(Name: ParserWorkStageConstants.PAGINATION, WithLock: true);
        Maybe<ParserWorkStage> stage = await ParserWorkStage.FromDb(session, query, ct);
        return stage;
    }

    private static bool StageIsNotPaginationStage(Maybe<ParserWorkStage> stage)
    {
        return !stage.HasValue;
    }
}