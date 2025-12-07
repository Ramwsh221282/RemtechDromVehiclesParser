namespace DromVehiclesParser.ConcreteItemParsing.Models;

public sealed record DromCatalogueItem(
    string Id,
    string Url,
    IReadOnlyList<string> Photos,
    bool Processed,
    int RetryCount
);