namespace DromVehiclesParser.ParserRegistration.Database;

public sealed class NpgSqlParserRegistrationTicket
{
    public required Guid Id { get; init; }
    public required bool WasSent { get; init; }
    public required DateTime? Finished { get; init; }
}