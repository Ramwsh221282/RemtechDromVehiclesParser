using DromVehiclesParser.ParserRegistration.Models;
using RemTech.SharedKernel.Core.Handlers;

namespace DromVehiclesParser.ParserRegistration.Features.PublishRegistrationTicket.Decorators;

public sealed class PublishRegistrationTicketCommandHandlerLogging(
    Serilog.ILogger logger,
    ICommandHandler<PublishRegistrationTicketCommand, ParserRegistrationTicket> origin) 
    : ICommandHandler<PublishRegistrationTicketCommand, ParserRegistrationTicket>
{
    private readonly Serilog.ILogger _logger = logger.ForContext<PublishRegistrationTicketCommandHandler>();
    
    public async Task<ParserRegistrationTicket> Execute(PublishRegistrationTicketCommand command)
    {
        _logger.Information("Publishing ticket.");
        ParserRegistrationTicket ticket = await origin.Execute(command);

        _logger.Information("""
                            Published registration ticket:
                            Id {Id}
                            Was sent: {WasSent}
                            """, ticket.Id, ticket.WasSent);

        return ticket;
    }
}