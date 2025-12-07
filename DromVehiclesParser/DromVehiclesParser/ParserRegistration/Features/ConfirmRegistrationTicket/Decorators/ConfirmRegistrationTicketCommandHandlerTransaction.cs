using DromVehiclesParser.ParserRegistration.Models;
using RemTech.SharedKernel.Core.Handlers;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.ParserRegistration.Features.ConfirmRegistrationTicket.Decorators;

public sealed class ConfirmRegistrationTicketCommandHandlerTransaction(
    NpgSqlSession session,
    ICommandHandler<ConfirmRegistrationTicketCommand, ParserRegistrationTicket> handler) :
    ICommandHandler<ConfirmRegistrationTicketCommand, ParserRegistrationTicket>
{
    public async Task<ParserRegistrationTicket> Execute(ConfirmRegistrationTicketCommand command)
    {
        await session.UseTransaction();
        ParserRegistrationTicket confirmed = await handler.Execute(command);
        await session.UnsafeCommit(CancellationToken.None);
        return confirmed;
    }
}