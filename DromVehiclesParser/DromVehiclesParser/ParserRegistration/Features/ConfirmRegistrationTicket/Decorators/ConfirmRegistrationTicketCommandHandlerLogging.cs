using DromVehiclesParser.ParserRegistration.Models;
using RemTech.SharedKernel.Core.Handlers;

namespace DromVehiclesParser.ParserRegistration.Features.ConfirmRegistrationTicket.Decorators;

public sealed class ConfirmRegistrationTicketCommandHandlerLogging(
    Serilog.ILogger logger,
    ICommandHandler<ConfirmRegistrationTicketCommand, ParserRegistrationTicket> handler) :
    ICommandHandler<ConfirmRegistrationTicketCommand, ParserRegistrationTicket>
{
    private readonly Serilog.ILogger _logger = logger.ForContext<ConfirmRegistrationTicketCommandHandler>();
    
    public async Task<ParserRegistrationTicket> Execute(ConfirmRegistrationTicketCommand command)
    {
        _logger.Information("Confirming parser registration ticket with id: {Id}", command.Id);
        ParserRegistrationTicket confirmed = await handler.Execute(command);
        _logger.Information("""
                            Confirmed parser registration ticket
                            ID: {Id}
                            Was Sent: {WasSent}
                            Confirmation date: {ConfirmationDate}
                            """, confirmed.Id, confirmed.WasSent, confirmed.Finished);
        return confirmed;
    }
}