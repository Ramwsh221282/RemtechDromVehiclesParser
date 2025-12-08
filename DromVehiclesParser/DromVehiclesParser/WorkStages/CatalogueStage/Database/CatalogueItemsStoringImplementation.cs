using System.Text.Json;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.WorkStages.CatalogueStage.Database;

// CREATE TABLE IF NOT EXISTS drom_vehicles_parser.catalogue_items
// (
//     id uuid primary key,
//     url text not null,
//     photos jsonb not null,
//     processed boolean not null,
//     retry_count integer not null
// );
public static class CatalogueItemsStoringImplementation
{
    extension(IEnumerable<DromCatalogueItem> items)
    {
        public async Task SaveMany(NpgSqlSession session)
        {
            const string sql = """
                               INSERT INTO drom_vehicles_parser.catalogue_items
                               (id, url, photos, processed, retry_count)
                               VALUES
                               (@id, @url, @photos::jsonb, @processed, @retry_count)
                               """;
            IEnumerable<object> parameters = items.Select(ExtractParameters);
            await session.ExecuteBulk(sql, parameters);
        }
    }

    extension(DromCatalogueItem item)
    {
        private object ExtractParameters() => new
        {
            id = item.Id,
            url = item.Url,
            photos = JsonSerializer.Serialize(item.Photos),
            processed = item.Processed,
            retry_count = item.RetryCount
        };
    }
}