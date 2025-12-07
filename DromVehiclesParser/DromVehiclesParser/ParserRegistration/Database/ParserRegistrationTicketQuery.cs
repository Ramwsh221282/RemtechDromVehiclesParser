namespace DromVehiclesParser.ParserRegistration.Database;

public sealed record ParserRegistrationTicketQuery(Guid? Id = null, bool SentOnly = false, bool FinishedOnly = false);