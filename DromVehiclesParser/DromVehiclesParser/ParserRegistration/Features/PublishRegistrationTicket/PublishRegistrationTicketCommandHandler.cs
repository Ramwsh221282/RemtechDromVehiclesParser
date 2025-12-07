using DromVehiclesParser.ParserRegistration.Database;
using DromVehiclesParser.ParserRegistration.Models;
using DromVehiclesParser.ParserRegistration.RabbitMq;
using RemTech.SharedKernel.Core.Handlers;

namespace DromVehiclesParser.ParserRegistration.Features.PublishRegistrationTicket;

public sealed class PublishRegistrationTicketCommandHandler(
    NpgSqlParserRegistrationTicketsStorage tickets,
    ParserRegistrationTicketPublisher publisher
)
    : ICommandHandler<PublishRegistrationTicketCommand, ParserRegistrationTicket>
{
    public async Task<ParserRegistrationTicket> Execute(PublishRegistrationTicketCommand command)
    {
        ParserRegistrationTicket ticket = ParserRegistrationTicket.NewlySent();
        await tickets.Save(ticket);
        await publisher.Publish(ticket);
        return ticket;
    }
}