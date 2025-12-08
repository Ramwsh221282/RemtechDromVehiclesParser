using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using DromVehiclesParser.WorkStages.Common.Parsers.Models;

namespace DromVehiclesParser.WorkStages.PaginationStage.Models;

public static class WorkingParserLinkPaginationConstruction
{
    extension(WorkingParserLinkPagination)
    {
        public static WorkingParserLinkPagination Create(WorkingParserLink link, DromCataloguePage page)
        {
            return new WorkingParserLinkPagination(
                Id: Guid.NewGuid(),
                LinkId: link.Id,
                Url: page.Url,
                CatalogueItemsFetched: false,
                RetryCount: 0
            );
        }
    }
}