namespace DromVehiclesParser.Parsing.CatalogueParsing.Models;

public sealed record DromCataloguePage(
    Guid Id,
    Guid PaginationId,
    int Number,
    string Url,
    int RetryCount,
    bool Processed);