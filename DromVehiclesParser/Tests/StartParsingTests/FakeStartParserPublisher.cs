using System.Text;
using System.Text.Json;
using DromVehiclesParser.Shared;
using RabbitMQ.Client;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace Tests.StartParsingTests;

public sealed class FakeStartParserPublisher(RabbitMqConnectionSource connectionSource, Serilog.ILogger logger)
{
    private const string Queue = ServiceConstants.ParsersQueue;
    private const string Exchange = ServiceConstants.CreateParserExchange;
    private const string Type = "topic";
    private static readonly string RoutingKey = $"start.{ServiceConstants.CurrentServiceDomain}.{ServiceConstants.CurrentServiceType}";
    private readonly Serilog.ILogger _logger = logger.ForContext<FakeStartParserPublisher>();
    
    public async Task Publish(object message)
    {
        string json = JsonSerializer.Serialize(message);
        ReadOnlyMemory<byte> body = Encoding.UTF8.GetBytes(json);
        CancellationToken ct = CancellationToken.None;
        
        IConnection connection = await connectionSource.GetConnection(ct);

        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
            );

        await using IChannel channel = await connection.CreateChannelAsync(options, ct);

        await channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: Type,
            durable: true,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey,
            cancellationToken: ct);

        BasicProperties properties = new() { Persistent = true };

        await channel.BasicPublishAsync(
            exchange: Exchange,
            routingKey: RoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: ct
        );
        
        _logger.Information("Published message to start parser.");
    }
}