using DromVehiclesParser.Commands.ExtractAdvertisementFromItsPage;
using DromVehiclesParser.Parsing.CatalogueParsing.Extensions;
using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Extensions;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;
using DromVehiclesParser.Parsing.ParsingStages.Database;
using DromVehiclesParser.Parsing.ParsingStages.Models;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Parsing.ParsingStages.StageProcessStrategies;

public static class AdvertisementsExtactingFromitsPageStage
{
    extension(ParsingStage)
    {
        public static ParsingStage ExtractAdvertisementsFromItsPage => async (deps, ct) =>
        {
            deps.Deconstruct(out BrowserFactory factory, out NpgSqlConnectionFactory npgSql, out Serilog.ILogger dLogger, out _);
            Serilog.ILogger logger = dLogger.ForContext<ParsingStage>();
            await using NpgSqlSession session = new(npgSql);
            
            Maybe<ParserWorkStage> stage = await GetConcreteStage(session);
            if (!stage.HasValue) return;
            
            DromCatalogueAdvertisement[] advertisements = await GetCatalogueAdvertisementsForProcessing(session);
            if (CanSwitchNextStage(advertisements))
            {
                await SwitchNextStage(stage, session, logger, ct);
                await FinishTransaction(session, logger, ct);
                return;
            }
            
            await ExtractAdvertisementsFromItsPage(advertisements, factory, session, logger);
            await FinishTransaction(session, logger, ct);
        };
    }

    private static async Task ExtractAdvertisementsFromItsPage(
        DromCatalogueAdvertisement[] advertisements, 
        BrowserFactory browsers, 
        NpgSqlSession session, 
        Serilog.ILogger logger)
    {
        IBrowser browser = await browsers.ProvideBrowser();
        List<DromAdvertisementFromPage> results = [];
        for (int i = 0; i < advertisements.Length; i++)
        {
            DromCatalogueAdvertisement advertisement = advertisements[i];

            try
            {
                Func<Task<IPage>> pageSource = () => browser.GetPage();

                IExtractAdvertisementFromItsPageCommand extractCommand =
                    new ExtractAdvertisementFromItsPageCommand(pageSource)
                        .UseLogging(logger);

                DromAdvertisementFromPage result = await extractCommand.Extract(advertisement);
                results.Add(result);
                advertisement = advertisement.MarkProcessed();
                await advertisement.Update(session);
                logger.Information("Extracted advertisement from page {Url}", advertisement.Url);
            }
            catch(WithdrawException)
            {
                logger.Information("Advertisement {Url} withdrawn from sale", advertisement.Url);
                await advertisement.Remove(session);
            }
            catch (EvaluationFailedException ex)
            {
                logger.Fatal(ex, "Failed to extract advertisement from page {Url}", advertisement.Url);
            }
            catch (Exception)
            {
                logger.Fatal("Failed to extract advertisement from page {Url}", advertisement.Url);
                advertisement = advertisement.IncrementRetryCount();
                await advertisement.Update(session);
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(5)); // forced delay to avoid get blocked.
            }
        }
        
        await browser.DestroyAsync();
        await results.PersistMany(session);
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
    
    private static async Task SwitchNextStage(
        Maybe<ParserWorkStage> stage, 
        NpgSqlSession session, 
        Serilog.ILogger logger, 
        CancellationToken ct)
    {
        ParserWorkStage concreteAdvertisementsStage = stage.Value.FinalizationStage();
        await concreteAdvertisementsStage.Update(session, ct);
        logger.Information("Switched to stage: {Name}", concreteAdvertisementsStage.StageName);        
    }
    
    private static bool CanSwitchNextStage(DromCatalogueAdvertisement[] advertisements)
    {
        return advertisements.Length == 0;
    }
    
    private static async Task<DromCatalogueAdvertisement[]> GetCatalogueAdvertisementsForProcessing(NpgSqlSession session)
    {
        DromCatalogueAdvertisementQuery query = new(UnprocessedOnly: true, WithLock: true, RetryLimit: 10, Limit: 20);
        return await DromCatalogueAdvertisement.GetMany(session, query, CancellationToken.None);
    }
    
    private static async Task<Maybe<ParserWorkStage>> GetConcreteStage(NpgSqlSession session)
    {
        ParserWorkStageStoringImplementation.ParserWorkStageQuery query = new(Name: ParserWorkStageConstants.CONCRETE, WithLock: true);
        Maybe<ParserWorkStage> stage = await ParserWorkStage.FromDb(session, query);
        return stage;
    }
}