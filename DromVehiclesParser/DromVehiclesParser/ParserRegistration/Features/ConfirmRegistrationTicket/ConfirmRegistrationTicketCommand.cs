using RemTech.SharedKernel.Core.Handlers;

namespace DromVehiclesParser.ParserRegistration.Features.ConfirmRegistrationTicket;

public sealed record ConfirmRegistrationTicketCommand(Guid Id) : ICommand;