namespace DromVehiclesParser.Parsing.CatalogueParsing.Extensions;

public sealed record DromCataloguePageQuery(
    bool UnprocessedOnly = false,
    int RetryLimit = 0,
    bool WithLock = false
);