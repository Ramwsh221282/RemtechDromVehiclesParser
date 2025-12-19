using DromVehiclesParser.Parsing.ConcreteItemParsing.Extensions;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;
using DromVehiclesParser.Stages.Database;
using DromVehiclesParser.Stages.Models;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Parsing.ParsingStages;

public static class FinalizationStage
{
    extension(ParsingStage)
    {
        public static ParsingStage Finalization => async (deps, ct) =>
        {
            deps.Deconstruct(out _, out NpgSqlConnectionFactory npgSql, out Serilog.ILogger dLogger);
            Serilog.ILogger logger = dLogger.ForContext<ParsingStage>();
            await using NpgSqlSession session = new(npgSql);
            
            Maybe<ParserWorkStage> stage = await GetFinalizationStage(session);
            if (!stage.HasValue) return;
            
            DromAdvertisementFromPage[] advertisements = await GetAdvertisementsForFinalization(session);
            if (CanSwitchNextStage(advertisements))
            {
                await SwitchNextStage(stage, session, logger, ct);
                await FinishTransaction(session, logger, ct);
                return;
            }
            
            await FinalizeAdvertisements(session, logger, ct);
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
    
    private static async Task FinalizeAdvertisements(NpgSqlSession session, Serilog.ILogger logger,
        CancellationToken ct)
    {
        DromAdvertisementFromPage[] advertisements = await GetAdvertisementsForFinalization(session);
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
}