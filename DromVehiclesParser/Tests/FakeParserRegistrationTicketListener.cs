using System.Text;
using System.Text.Json;
using DromVehiclesParser.Shared;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace Tests;

public sealed class FakeParserRegistrationTicketListener(
    RabbitMqConnectionSource rabbitMq,
    Serilog.ILogger logger
    ) : BackgroundService
{
    public static bool HadTicket = false;
    private const string Queue = ServiceConstants.ParsersQueue;
    private const string Exchange = ServiceConstants.CreateParserExchange;
    private const string RoutingKey = ServiceConstants.CreateParserRoutingKey;
    private const string Type = "topic";
    
    private readonly Serilog.ILogger _logger = logger.ForContext<FakeParserRegistrationTicketListener>();
    
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection connection = await rabbitMq.GetConnection(stoppingToken);
        IChannel channel = await connection.CreateChannelAsync();

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
        consumer.ReceivedAsync += Handler;
        await channel.BasicConsumeAsync(
            queue: Queue,
            autoAck: true,
            cancellationToken: stoppingToken,
            consumer: consumer
        );
    }

    private AsyncEventHandler<BasicDeliverEventArgs> Handler => async (sender, @event) =>
    {
        _logger.Information("Recieved parser registration ticket.");
        
        string json = Encoding.UTF8.GetString(@event.Body.ToArray());
        using JsonDocument document = JsonDocument.Parse(json);
        Guid id = document.RootElement.GetProperty("id").GetGuid();
        string domain = document.RootElement.GetProperty("parser_domain").GetString()!;
        string type = document.RootElement.GetProperty("parser_type").GetString()!;

        object body = new
        {
            id
        };
        
        string responseJson = JsonSerializer.Serialize(body);
        FakeParserRegistrationTicketConfirmedPublisher publisher = new(rabbitMq);
        await publisher.Publish(responseJson);
        HadTicket = true;
        _logger.Information("Registered parser: Id: {Id} Domain: {Domain} Type: {Type}", id, domain, type);
    };
}