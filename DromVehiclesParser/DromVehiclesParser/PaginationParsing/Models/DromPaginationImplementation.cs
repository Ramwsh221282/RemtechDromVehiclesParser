using DromVehiclesParser.CatalogueParsing.Models;

namespace DromVehiclesParser.PaginationParsing.Models;

public static class DromPaginationImplementation
{
    extension(DromPagination pagination)
    {
        public IEnumerable<DromCataloguePage> Pages(string catalogueUrl)
        {
            return Enumerable.Range(pagination.CurrentPage, pagination.MaxPage)
                .Select(p => DromCataloguePage.New(pagination, catalogueUrl, p));
        }
    }
}