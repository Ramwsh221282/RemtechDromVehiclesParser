using DromVehiclesParser.Commands.ExtractAdvertisementsFromCatalogue;
using DromVehiclesParser.Commands.HoverCatalogueImages;
using DromVehiclesParser.Parsing.CatalogueParsing.Extensions;
using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using DromVehiclesParser.Parsing.ParsingStages.Database;
using DromVehiclesParser.Parsing.ParsingStages.Models;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Parsing.ParsingStages.StageProcessStrategies;

public static class CatalogueAdvertisementsExtractingStage
{
    extension(ParsingStage)
    {
        public static ParsingStage CatalogueAdvertisementsExtraction => async (deps, ct) =>
        {
            deps.Deconstruct(out BrowserFactory factory, out NpgSqlConnectionFactory npgSql, out Serilog.ILogger logger, out _);
            await using NpgSqlSession session = new(npgSql);
            
            Maybe<ParserWorkStage> stage = await GetCatalogueStage(session, ct);
            if (!stage.HasValue) return;
            
            DromCataloguePage[] pages = await GetCataloguePages(session, ct);
            if (CanSwitchNextStage(pages))
            {
                await SwitchNextStage(stage, session, logger, ct);
                await FinishTransaction(session, logger, ct); 
                return;
            }
            
            await ExtractCatalogueAdvertisements(pages, factory, session, logger);
            await FinishTransaction(session, logger, ct);
        };
    }

    private static async Task ExtractCatalogueAdvertisements(
        DromCataloguePage[] pages, 
        BrowserFactory browsers, 
        NpgSqlSession session, 
        Serilog.ILogger logger)
    {
        IBrowser browser = await browsers.ProvideBrowser();
        
        for (int i = 0; i < pages.Length; i++)
        {
            DromCataloguePage page = pages[i];
            
            Func<Task<IPage>> pageSource = () => browser.GetPage();
            
            IExtractAdvertisementsFromCatalogueCommand extractCommand = new ExtractAdvertisementsFromCatalogueCommand(pageSource)
                .UseLogging(logger);
            
            IHoverAdvertisementsCatalogueImagesCommand hoverCommand = new HoverAdvertisementsCatalogueImagesCommand(pageSource)
                .UseLogging(logger);
            
            pages[i] = await ProcessExtraction(extractCommand, hoverCommand, page, session);
            await Task.Delay(TimeSpan.FromSeconds(5)); // forced delay to avoid get blocked.
        }
        
        await browser.DestroyAsync();
        await pages.UpdateMany(session);
    }

    private static async Task<DromCataloguePage> ProcessExtraction(
        IExtractAdvertisementsFromCatalogueCommand extranctCommand,
        IHoverAdvertisementsCatalogueImagesCommand hoverCommand,
        DromCataloguePage page,
        NpgSqlSession session)
    {
        try
        {
            DromCatalogueAdvertisement[] advertisements = await extranctCommand.Extract(page, hoverCommand);
            await advertisements.PersistMany(session);
            return page.MarkProcessed();
        }
        catch (EvaluationFailedException)
        {
            return page;
        }
        catch (Exception)
        {
            return page.IncreaseRetryCount();
        }
    }
    
    private static async Task FinishTransaction(
        NpgSqlSession session, 
        Serilog.ILogger logger, 
        CancellationToken ct)
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
    
    private static async Task SwitchNextStage(
        Maybe<ParserWorkStage> stage, 
        NpgSqlSession session, 
        Serilog.ILogger logger, 
        CancellationToken ct)
    {
        ParserWorkStage concreteAdvertisementsStage = stage.Value.ConcreteStage();
        await concreteAdvertisementsStage.Update(session, ct);
        logger.Information("Switched to stage: {Name}", concreteAdvertisementsStage.StageName);
    }
    
    private static async Task<Maybe<ParserWorkStage>> GetCatalogueStage(NpgSqlSession session, CancellationToken ct)
    {
        ParserWorkStageStoringImplementation.ParserWorkStageQuery query = new(Name: ParserWorkStageConstants.CATALOGUE, WithLock: true);
        Maybe<ParserWorkStage> stage = await ParserWorkStage.FromDb(session, query, ct);
        return stage;
    }

    private static async Task<DromCataloguePage[]> GetCataloguePages(NpgSqlSession session, CancellationToken ct)
    {
        DromCataloguePageQuery query = new(UnprocessedOnly: true, RetryLimit: 10, WithLock: true);
        return await DromCataloguePage.GetMany(session, query, ct);        
    }

    private static bool CanSwitchNextStage(DromCataloguePage[] pages)
    {
        return pages.Length == 0;
    }
}