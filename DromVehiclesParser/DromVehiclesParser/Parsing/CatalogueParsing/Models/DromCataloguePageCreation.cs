namespace DromVehiclesParser.Parsing.CatalogueParsing.Models;

public static class DromCataloguePageCreation
{
    extension (DromCataloguePage)
    {
        public static DromCataloguePage New(string pagedUrl)
        {
            return new DromCataloguePage(pagedUrl, RetryCount: 0, Processed: false);
        }
    }
}