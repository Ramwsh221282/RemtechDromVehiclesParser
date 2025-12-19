using DromVehiclesParser.Commands.HoverCatalogueImages;
using DromVehiclesParser.Parsing.CatalogueParsing.Models;

namespace DromVehiclesParser.Commands.ExtractAdvertisementsFromCatalogue;

public interface IExtractAdvertisementsFromCatalogueCommand
{
    Task<DromCatalogueAdvertisement[]> Extract(DromCataloguePage page, IHoverAdvertisementsCatalogueImagesCommand hovering);
}