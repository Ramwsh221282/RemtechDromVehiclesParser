using System.Text.Json;
using DromVehiclesParser.Shared;
using DromVehiclesParser.Shared.Extensions;
using DromVehiclesParser.WorkStages.Common.Parsers.Database;
using DromVehiclesParser.WorkStages.Common.Parsers.Models;
using DromVehiclesParser.WorkStages.Common.Stages.Database;
using DromVehiclesParser.WorkStages.Common.Stages.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.WorkStages.StartParser.BackgroundTasks;

public sealed class StartParserWorkStageListener(StartParserWorkStageListenerDependencies dependencies) : BackgroundService
{
    private static readonly string Queue = $"start.{ServiceConstants.CurrentServiceDomain}.{ServiceConstants.CurrentServiceType}";
    private static readonly string Exchange = ServiceConstants.CurrentServiceExchange;
    private const string Type = "topic";
    private static readonly string RoutingKey = Queue;
    private readonly Serilog.ILogger _logger = dependencies.Logger.ForContext<StartParserWorkStageListener>();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection connection = await dependencies.RabbitMq.GetConnection(stoppingToken);
        
        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true, 
            publisherConfirmationTrackingEnabled: true);
        
        IChannel channel = await connection.CreateChannelAsync(options, stoppingToken);

        await channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: Type,
            durable: true,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken);

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += Handler(channel);
        
        await channel.BasicConsumeAsync(
            queue: Queue,
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken
        );
    }

    private AsyncEventHandler<BasicDeliverEventArgs> Handler(IChannel channel) => async (sender, @event) =>
    {
        _logger.Information("Recieved message to start parser.");
        try
        {
            await using NpgSqlSession session = new(dependencies.NpgSql);

            if (await ParserWorkStage.HasAny(session, new ParserWorkStageStoringImplementation.ParserWorkStageQuery()))
            {
                _logger.Error("Drom service already has working parser stage.");
                return;
            }

            using JsonDocument document = JsonDocument.FromBasicDeliverEventArgs(@event);
            ParserWorkStage stage = ParserWorkStage.FromJsonDocument(document).PaginationStage();
            WorkingParser parser = WorkingParser.FromJsonDocument(document);

            await stage.Save(session);
            await parser.Save(session, withLinks: true);

            await session.UnsafeCommit(CancellationToken.None);

            _logger.Information("""
                                Saved parser work stage:
                                Id: {Id}
                                Name: {Name}
                                Finished: {Finished}
                                """, stage.Id, stage.StageName, stage.Finished);

            _logger.Information("""
                                Saved working parser:
                                Id: {Id}
                                Domain: {Domain}
                                Type: {Type}
                                Links Count: {LinksCount}
                                """, parser.Id, parser.Domain, parser.Type, parser.Links.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to invoke parser start invocation");
        }
        finally
        {
            await channel.BasicAckAsync(@event.DeliveryTag, false);
        }
    };
}