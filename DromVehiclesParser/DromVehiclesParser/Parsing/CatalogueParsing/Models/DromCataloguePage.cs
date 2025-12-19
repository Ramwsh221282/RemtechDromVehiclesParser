namespace DromVehiclesParser.Parsing.CatalogueParsing.Models;

public sealed record DromCataloguePage(
    string Url,
    int RetryCount,
    bool Processed
)
{
    public DromCataloguePage MarkProcessed()
    {
        return this with { Processed = true };
    }
    
    public DromCataloguePage IncreaseRetryCount()
    {
        return this with { RetryCount = RetryCount + 1 };
    }
}