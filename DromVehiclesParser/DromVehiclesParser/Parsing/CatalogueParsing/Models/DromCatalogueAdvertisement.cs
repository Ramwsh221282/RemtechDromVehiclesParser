namespace DromVehiclesParser.Parsing.CatalogueParsing.Models;

public sealed record DromCatalogueAdvertisement(
    string Id,
    string Url,
    IReadOnlyList<string> Photos,
    bool Processed,
    int RetryCount
)
{
    public DromCatalogueAdvertisement MarkProcessed()
    {
        return this with { Processed = true };
    }
    
    public DromCatalogueAdvertisement IncrementRetryCount()
    {
        return this with { RetryCount = RetryCount + 1 };
    }
}