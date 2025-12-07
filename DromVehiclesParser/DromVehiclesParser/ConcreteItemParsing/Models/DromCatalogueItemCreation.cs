namespace DromVehiclesParser.ConcreteItemParsing.Models;

public static class DromCatalogueItemCreation
{
    extension(DromCatalogueItem)
    {
        public static DromCatalogueItem New(string id, string url, IReadOnlyList<string> photos)
        {
            return new DromCatalogueItem(id, url, photos, false, 0);
        }
    }
}