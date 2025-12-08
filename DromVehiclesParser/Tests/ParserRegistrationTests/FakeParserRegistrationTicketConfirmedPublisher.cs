using System.Text;
using DromVehiclesParser.Shared;
using RabbitMQ.Client;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace Tests.ParserRegistrationTests;

public sealed class FakeParserRegistrationTicketConfirmedPublisher(RabbitMqConnectionSource connectionSource)
{
    private static readonly string Queue = $"confirmation.{ServiceConstants.CurrentServiceDomain}.{ServiceConstants.CurrentServiceType}";
    private static readonly string Exchange = $"{ServiceConstants.CurrentServiceDomain}.{ServiceConstants.CurrentServiceType}";
    private static readonly string RoutingKey = Queue;
    private const string Type = "topic";
    
    public async Task Publish(string message, CancellationToken ct = default)
    {
        IConnection connection = await connectionSource.GetConnection(ct);
        
        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );
        
        await using IChannel channel = await connection.CreateChannelAsync(options);

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
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey,
            cancellationToken: ct);
        
        BasicProperties publishProperties = new() { Persistent = true };
        ReadOnlyMemory<byte> body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: Exchange,
            routingKey: RoutingKey,
            mandatory: true,
            publishProperties,
            body: body,
            cancellationToken: ct);
    }
}