namespace DromVehiclesParser.ParserRegistration.Models;

public sealed record ParserRegistrationTicket(Guid Id, bool WasSent, DateTime? Finished);