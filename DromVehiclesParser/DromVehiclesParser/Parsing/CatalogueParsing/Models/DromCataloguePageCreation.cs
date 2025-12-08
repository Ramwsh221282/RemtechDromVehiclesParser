using DromVehiclesParser.Parsing.PaginationParsing.Models;
using DromVehiclesParser.WorkStages.CatalogueStage.Models;

namespace DromVehiclesParser.Parsing.CatalogueParsing.Models;

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

        public static DromCataloguePage FromWorkingPage(WorkingCataloguePage page)
        {
            return new DromCataloguePage(
                Id: page.Id,
                PaginationId: Guid.NewGuid(),
                Number: 0,
                Url: page.Url,
                RetryCount: 0,
                Processed: false
                );
        }
    }
}