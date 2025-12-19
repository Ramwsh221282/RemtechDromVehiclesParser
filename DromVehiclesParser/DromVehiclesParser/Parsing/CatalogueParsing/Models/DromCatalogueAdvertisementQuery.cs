namespace DromVehiclesParser.Parsing.CatalogueParsing.Models;

public sealed record DromCatalogueAdvertisementQuery(
    bool UnprocessedOnly = false,
    bool WithLock = false,
    int? RetryLimit = null,
    int? Limit = null
);