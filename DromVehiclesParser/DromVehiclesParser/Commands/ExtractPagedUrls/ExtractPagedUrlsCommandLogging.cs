using DromVehiclesParser.Parsing.CatalogueParsing.Models;

namespace DromVehiclesParser.Commands.ExtractPagedUrls;

public sealed class ExtractPagedUrlsCommandLogging(Serilog.ILogger logger, IExtractPagedUrlsCommand inner) : IExtractPagedUrlsCommand
{
    private Serilog.ILogger Logger { get; } = logger.ForContext<IExtractPagedUrlsCommand>();
    
    public async Task<IEnumerable<DromCataloguePage>> Extract(string initialUrl)
    {
        Logger.Information("Begin extracting paged urls for {InitialUrl}", initialUrl);
        try
        {
            IEnumerable<DromCataloguePage> pages = await inner.Extract(initialUrl);
            foreach (DromCataloguePage page in pages)
                Logger.Information("Extracted page {PageUrl}", page.Url);
            return pages;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Failed to extract paged urls for {InitialUrl}", initialUrl);
            throw;
        }
    }
}