using DromVehiclesParser.PaginationParsing.Models;

namespace DromVehiclesParser.CatalogueParsing.Models;

public static class DromCataloguePageCreation
{
    extension (DromCataloguePage)
    {
        public static DromCataloguePage New(DromPagination pagination, string catalogueUrl, int page)
        {
            return new(
                Guid.NewGuid(), 
                pagination.Id, 
                page, 
                $"{catalogueUrl}page{page}", 
                RetryCount: 0, 
                Processed: false);
        }
    }
}