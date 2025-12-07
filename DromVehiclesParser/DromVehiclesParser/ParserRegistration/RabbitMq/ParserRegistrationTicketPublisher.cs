using System.Text;
using System.Text.Json;
using DromVehiclesParser.ParserRegistration.Models;
using DromVehiclesParser.Shared;
using RabbitMQ.Client;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace DromVehiclesParser.ParserRegistration.RabbitMq;

public sealed class ParserRegistrationTicketPublisher(RabbitMqConnectionSource rabbitMq)
{
    private const string Queue = ServiceConstants.ParsersQueue;
    private const string Exchange = ServiceConstants.ParsersExchange;
    private const string RoutingKey = ServiceConstants.CreateParserRoutingKey;
    private const string Type = "topic";

    public async Task Publish(ParserRegistrationTicket ticket, CancellationToken ct = default)
    {
        IConnection connection = await rabbitMq.GetConnection(ct);
        CreateChannelOptions channelOptions = new(
            publisherConfirmationsEnabled: true, 
            publisherConfirmationTrackingEnabled: true);
        
        await using IChannel channel = await connection.CreateChannelAsync(options: channelOptions, cancellationToken: ct);

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

        object payload = new
        {
            id = ticket.Id,
            parser_domain = ServiceConstants.CurrentServiceDomain,
            parser_type = ServiceConstants.CurrentServiceType
        };

        string jsonPayload = JsonSerializer.Serialize(payload);
        ReadOnlyMemory<byte> body = Encoding.UTF8.GetBytes(jsonPayload);

        await channel.BasicPublishAsync(
            exchange: Exchange,
            routingKey: RoutingKey,
            mandatory: true,
            body: body,
            cancellationToken: ct);
    }
}