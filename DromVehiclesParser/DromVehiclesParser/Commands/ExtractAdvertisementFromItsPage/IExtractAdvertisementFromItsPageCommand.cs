using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;

namespace DromVehiclesParser.Commands.ExtractAdvertisementFromItsPage;

public interface IExtractAdvertisementFromItsPageCommand
{
    Task<DromAdvertisementFromPage> Extract(DromCatalogueAdvertisement catalogueAdvertisement);
}