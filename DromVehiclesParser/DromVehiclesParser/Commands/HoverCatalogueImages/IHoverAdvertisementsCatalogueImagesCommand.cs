using DromVehiclesParser.Parsing.CatalogueParsing.Models;

namespace DromVehiclesParser.Commands.HoverCatalogueImages;

public interface IHoverAdvertisementsCatalogueImagesCommand
{
    Task Hover(DromCataloguePage page);
}