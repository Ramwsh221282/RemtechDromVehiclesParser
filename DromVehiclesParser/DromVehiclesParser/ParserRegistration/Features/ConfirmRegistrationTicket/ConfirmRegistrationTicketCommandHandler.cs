using DromVehiclesParser.ParserRegistration.Database;
using DromVehiclesParser.ParserRegistration.Models;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Core.Handlers;

namespace DromVehiclesParser.ParserRegistration.Features.ConfirmRegistrationTicket;

public sealed class ConfirmRegistrationTicketCommandHandler(
    NpgSqlParserRegistrationTicketsStorage tickets
) 
    : ICommandHandler<ConfirmRegistrationTicketCommand, ParserRegistrationTicket>
{
    public async Task<ParserRegistrationTicket> Execute(ConfirmRegistrationTicketCommand command)
    {
        DateTime finishDate = DateTime.UtcNow;
        ParserRegistrationTicketQuery query = new(Id: command.Id);
        Maybe<ParserRegistrationTicket> ticket = await tickets.GetTicket(query);
        if (!ticket.HasValue) throw new InvalidOperationException("Cannot confirm registration ticket. Ticket was not found.");
        await tickets.Delete(ticket.Value);
        ParserRegistrationTicket finished = ticket.Value.Finished(finishDate);
        return finished;
    }
}