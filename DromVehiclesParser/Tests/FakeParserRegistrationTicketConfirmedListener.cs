using System.Text;
using System.Text.Json;
using DromVehiclesParser.ParserRegistration.BackgroundServices;
using DromVehiclesParser.ParserRegistration.Features.ConfirmRegistrationTicket;
using DromVehiclesParser.ParserRegistration.Models;
using DromVehiclesParser.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RemTech.SharedKernel.Core.Handlers;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace Tests;

public class FakeParserRegistrationTicketConfirmedListener (
    RabbitMqConnectionSource connectionSource,
    IServiceProvider sp,
    Serilog.ILogger logger) : 
    BackgroundService
{
    private const string Queue = ServiceConstants.CurrentServiceDomain;
    private const string Exchange = ServiceConstants.CurrentServiceType;
    private const string RoutingKey = "confirmation";
    private const string Type = "topic";
    private readonly Serilog.ILogger _logger = logger.ForContext<ParserRegistrationTicketConfirmedListener>();

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection connection = await connectionSource.GetConnection(stoppingToken);
        
        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );

        IChannel channel = await connection.CreateChannelAsync(options);
        
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
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken);
        
        AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += Handler;
        await channel.BasicConsumeAsync(
            queue: Queue,
            autoAck: true,
            cancellationToken: stoppingToken,
            consumer: consumer
        );
    }

    private AsyncEventHandler<BasicDeliverEventArgs> Handler => async (_, @event) =>
    {
        try
        {
            _logger.Information("Receieved parser registration confirmation message.");
            string json = Encoding.UTF8.GetString(@event.Body.ToArray());
            using JsonDocument document = JsonDocument.Parse(json);
            Guid id = document.RootElement.GetProperty("id").GetGuid();
            ConfirmRegistrationTicketCommand command = new(id);
            
            await using AsyncServiceScope scope = sp.CreateAsyncScope();
            ICommandHandler<ConfirmRegistrationTicketCommand, ParserRegistrationTicket> handler =
                scope.ServiceProvider.GetRequiredService<ICommandHandler<ConfirmRegistrationTicketCommand, ParserRegistrationTicket>>();
            await handler.Execute(command);
        }
        catch(Exception ex)
        {
            _logger.Error(ex, "Error at handling registration confirmation message.");
        }
    };
}