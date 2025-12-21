using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace Tests.ParserSubscriptionTests;

public sealed class FakeParserSubscriptionPublisher(RabbitMqConnectionSource rabbitMq, Serilog.ILogger logger)
{
    private Serilog.ILogger Logger { get; } = logger;

    public async Task Publish(Guid parserId, string parserType, string parserDomain)
    {
        Logger.Information("Publishing parser registration message: {ParserId}, {ParserType}, {ParserDomain}", 
            parserId, parserType, parserDomain);
        
        CancellationToken ct = CancellationToken.None;
        IConnection connection = await rabbitMq.GetConnection(ct);
        await using IChannel channel = await connection.CreateChannelAsync();

        string exchange = $"{parserDomain}.{parserType}";
        string queue = $"{exchange}.confirmation";
        string routingKey = $"{exchange}.confirmation";
        
        await channel.ExchangeDeclareAsync(exchange: exchange, durable: true, autoDelete: false, type: "topic");
        await channel.QueueDeclareAsync(queue: queue, durable: false, autoDelete: false, exclusive: false);
        await channel.QueueBindAsync(queue: queue, exchange: exchange, routingKey: routingKey);

        object payload = new
        {
            parser_id = parserId,
            parser_type = parserType,
            parser_domain = parserDomain
        };
        
        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        await channel.BasicPublishAsync(exchange: exchange, routingKey: routingKey, body: body);
        
        Logger.Information("Published parser registration message: {ParserId}, {ParserType}, {ParserDomain}", 
            parserId, parserType, parserDomain);
    }
}