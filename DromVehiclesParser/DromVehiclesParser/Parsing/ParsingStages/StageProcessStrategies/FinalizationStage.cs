using Dapper;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Extensions;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;
using DromVehiclesParser.Parsing.ParsingStages.Database;
using DromVehiclesParser.Parsing.ParsingStages.Models;
using DromVehiclesParser.ResultsExporing.TextFileExporting;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Parsing.ParsingStages.StageProcessStrategies;

public static class FinalizationStage
{
    extension(ParsingStage)
    {
        public static ParsingStage Finalization => async (deps, ct) =>
        {
            deps.Deconstruct(out _, out NpgSqlConnectionFactory npgSql, out Serilog.ILogger dLogger, out IExporter<TextFile> exporter);
            Serilog.ILogger logger = dLogger.ForContext<ParsingStage>();
            await using NpgSqlSession session = new(npgSql);
            
            Maybe<ParserWorkStage> stage = await GetFinalizationStage(session);
            if (!stage.HasValue) return;
            
            DromAdvertisementFromPage[] advertisements = await GetAdvertisementsForFinalization(session);
            if (CanSwitchNextStage(advertisements))
            {
                await RemoveWorkingParserInformation(session, ct);
                await RemoveWorkingParserLinksInformation(session, ct);
                await RemoveCataloguePagesInformation(session, ct);
                await RemoveItemsInformation(session, ct); 
                await SwitchNextStage(stage, session, logger, ct);
                await FinishTransaction(session, logger, ct);
                return;
            }
            
            await FinalizeAdvertisements(advertisements, session, logger, exporter, ct);
            await FinishTransaction(session, logger, ct);
        };
    }

    private static async Task SwitchNextStage(Maybe<ParserWorkStage> stage, NpgSqlSession session,
        Serilog.ILogger logger, CancellationToken ct)
    {
        ParserWorkStage sleepStage = stage.Value.SleepStage();
        await sleepStage.Update(session, ct);
        logger.Information("Switched to stage: {Name}", sleepStage.StageName);
    }
    
    private static bool CanSwitchNextStage(DromAdvertisementFromPage[] advertisements)
    {
        return advertisements.Length == 0;
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
    
    private static async Task FinalizeAdvertisements(
        DromAdvertisementFromPage[] advertisements,
        NpgSqlSession session, 
        Serilog.ILogger logger,
        IExporter<TextFile> exporter,
        CancellationToken ct)
    {
        exporter = exporter.UseLogging(logger);
        string resultDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results");
        Directory.CreateDirectory(resultDirPath);
        foreach (DromAdvertisementFromPage advertisement in advertisements)
        {
            string resultFilePath = Path.Combine(resultDirPath, $"{advertisement.Id}.txt");
            TextFile file = TextFile.FromDromAdvertisement(advertisement, resultFilePath);
            await exporter.Export(file, ct);
        }
        
        await advertisements.RemoveMany(session);
        logger.Information("Advertisements finalized: {Count}", advertisements.Length);
    }
    
    private static async Task<DromAdvertisementFromPage[]> GetAdvertisementsForFinalization(NpgSqlSession session)
    {
        QueryDromAdvertisements query = new(Limit: 50, WithLock: true);
        return await DromAdvertisementFromPage.GetMany(session, query, CancellationToken.None);
    }
    
    private static async Task<Maybe<ParserWorkStage>> GetFinalizationStage(NpgSqlSession session)
    {
        ParserWorkStageStoringImplementation.ParserWorkStageQuery query = new(Name: ParserWorkStageConstants.FINALIZATION, WithLock: true);
        return await ParserWorkStage.FromDb(session, query);
    }

    private static async Task RemoveWorkingParserInformation(NpgSqlSession session, CancellationToken ct)
    {
        const string sql = "DELETE FROM drom_vehicles_parser.working_parsers";
        CommandDefinition command = new(sql, transaction: session.Transaction, cancellationToken: ct);
        await session.Execute(command);
    }

    private static async Task RemoveWorkingParserLinksInformation(NpgSqlSession session, CancellationToken ct)
    {
        const string sql = "DELETE FROM drom_vehicles_parser.working_parser_links";
        CommandDefinition command = new(sql, transaction: session.Transaction, cancellationToken: ct);
        await session.Execute(command);
    }
    
    private static async Task RemoveCataloguePagesInformation(NpgSqlSession session, CancellationToken ct)
    {
        const string sql = "DELETE FROM drom_vehicles_parser.catalogue_pages";
        CommandDefinition command = new(sql, transaction: session.Transaction, cancellationToken: ct);
        await session.Execute(command);
    }

    private static async Task RemoveItemsInformation(NpgSqlSession session, CancellationToken ct)
    {
        const string sql = "DELETE FROM drom_vehicles_parser.items";
        CommandDefinition command = new(sql, transaction: session.Transaction, cancellationToken: ct);
        await session.Execute(command);
    }
}