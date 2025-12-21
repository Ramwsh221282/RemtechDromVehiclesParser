namespace DromVehiclesParser.Parsers.Database;

public sealed record WorkingParserLinkQuery(
    bool UnprocessedOnly = false, 
    int? RetryLimit = null, 
    bool WithLock = false
);