using DromVehiclesParser.Parsers.Database;
using DromVehiclesParser.Parsers.Models;
using DromVehiclesParser.Parsing.ParsingStages.Database;
using DromVehiclesParser.Parsing.ParsingStages.Models;
using ParsingSDK.ParserInvokingContext;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace DromVehiclesParser.DependencyInjection;

public static class ParsingStartingInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterDromParserStartQueue()
        {
            services.RegisterDromParserStartOptions();
            services.RegisterDromParserStartQueueHandle();
            services.RegisterParserInvokingQueue(RabbitMqProviding);
        }

        private void RegisterDromParserStartOptions()
        {
            services.AddOptions<ParserStartOptions>().BindConfiguration(nameof(ParserStartOptions));
        }

        private void RegisterDromParserStartQueueHandle()
        {
            services.AddSingleton<ParserStartQueueHandle>(sp =>
            {
                NpgSqlConnectionFactory npgSql = sp.GetRequiredService<NpgSqlConnectionFactory>();
                Serilog.ILogger logger = sp.GetRequiredService<Serilog.ILogger>().ForContext<ParserStartQueueHandle>();

                return async (@event) =>
                {
                    CancellationToken ct = CancellationToken.None;
                    logger.Information("Received message to start parser.");
                    await using NpgSqlSession session = new(npgSql);

                    try
                    {
                        if (await WorkingParser.Exists(session))
                        {
                            logger.Warning("""
                                           There is already working parser session.
                                           Cancelling starting new one. 
                                           """);
                            return;
                        }
                        
                        ParserWorkStage stage = ParserWorkStage.FromDeliverEventArgs(@event).PaginationStage();
                        WorkingParser parser = WorkingParser.FromDeliverEventArgs(@event);
                        WorkingParserLink[] links = WorkingParserLink.FromDeliverEventArgs(@event);
                        
                        await stage.Save(session);
                        await parser.Persist(session, ct);
                        await links.PersistMany(session);
                        await session.UnsafeCommit(ct);

                        logger.Information("""
                                           Saved parser work stage:
                                           Id: {Id}
                                           Name: {Name}
                                           Finished: {Finished}
                                           """, stage.Id, stage.StageName, stage.Finished);

                        logger.Information("""
                                           Saved working parser:
                                           Id: {Id}
                                           Domain: {Domain}
                                           Type: {Type}
                                           Links Count: {LinksCount}
                                           """, parser.Id, parser.Domain, parser.Type, links.Length);
                    }
                    catch (Exception ex)
                    {
                        logger.Fatal(ex, "Failed to execute parser start invokation.");
                    }
                };
            });
        }
    }
    
    private static Func<IServiceProvider, ParserStartQueueRabbitMqProvide> RabbitMqProviding => sp =>
    {
        RabbitMqConnectionSource source = sp.GetRequiredService<RabbitMqConnectionSource>();
        return async ct => await source.GetConnection(ct);
    };
}