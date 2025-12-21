using DromVehiclesParser.Commands.HoverCatalogueImages;
using DromVehiclesParser.Parsing.CatalogueParsing.Models;

namespace DromVehiclesParser.Commands.ExtractAdvertisementsFromCatalogue;

public sealed class ExtractAdvertisementsFromCatalogueLogging(Serilog.ILogger logger, IExtractAdvertisementsFromCatalogueCommand inner) : IExtractAdvertisementsFromCatalogueCommand
{
    private Serilog.ILogger Logger { get; } = logger.ForContext<IExtractAdvertisementsFromCatalogueCommand>();

    public async Task<DromCatalogueAdvertisement[]> Extract(DromCataloguePage page, IHoverAdvertisementsCatalogueImagesCommand hovering)
    {
        Logger.Debug("Extracting advertisements from page {Url}", page.Url);

        try
        {
            DromCatalogueAdvertisement[] result = await inner.Extract(page, hovering);
            foreach (DromCatalogueAdvertisement advertisement in result)
            {
                Logger.Information("""
                                   Drom catalogue advertisement info: 
                                   Id: {Id}
                                   Url: {Url}
                                   Photos: {PhotosLength}
                                   """,
                    advertisement.Id,
                    advertisement.Url,
                    advertisement.Photos.Count
                );
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Failed to extract advertisements from page {Url}", page.Url);
            throw;            
        }
    }    
}