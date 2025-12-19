using DromVehiclesParser.Parsing.CatalogueParsing.Models;

namespace DromVehiclesParser.Commands.HoverCatalogueImages;

public sealed class HoverAdvertisementsCatalogueImagesLogging(Serilog.ILogger logger, IHoverAdvertisementsCatalogueImagesCommand inner)
    : IHoverAdvertisementsCatalogueImagesCommand
{
    private Serilog.ILogger Logger { get; } = logger.ForContext<IHoverAdvertisementsCatalogueImagesCommand>();
    
    public async Task Hover(DromCataloguePage page)
    {
        Logger.Information("Hovering catalogue images for page {Url}", page.Url);
        try
        {
            await inner.Hover(page);
            Logger.Information("Catalogue images for page {Url} hovered", page.Url);
        }
        catch(Exception ex)
        {
            Logger.Error(ex, "Failed to hover catalogue images for page {Url}", page.Url);
            throw;
        }
    }
}