using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;

namespace DromVehiclesParser.Commands.ExtractAdvertisementFromItsPage;

public sealed class ExtractAdvertisementFromItsPageLogging(IExtractAdvertisementFromItsPageCommand innerCommand, Serilog.ILogger logger)
    : IExtractAdvertisementFromItsPageCommand
{
    private Serilog.ILogger Logger { get; } = logger.ForContext<IExtractAdvertisementFromItsPageCommand>();
    
    public async Task<DromAdvertisementFromPage> Extract(DromCatalogueAdvertisement catalogueAdvertisement)
    {
        Logger.Debug("Extracting advertisement from page {Url}", catalogueAdvertisement.Url);
        try
        {
            return await innerCommand.Extract(catalogueAdvertisement);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to extract advertisement from page {Url}", catalogueAdvertisement.Url);
            throw;
        }
    }
}