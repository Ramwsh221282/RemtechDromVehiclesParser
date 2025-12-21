namespace DromVehiclesParser.Parsers.Models;

public sealed record WorkingParserLink(
    Guid Id,
    string Url, 
    bool Processed,
    int RetryCount);