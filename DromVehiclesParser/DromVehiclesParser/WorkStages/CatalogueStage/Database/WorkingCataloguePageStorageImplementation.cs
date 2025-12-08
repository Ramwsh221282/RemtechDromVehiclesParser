using System.Data;
using Dapper;
using DromVehiclesParser.Shared.NpgSql;
using DromVehiclesParser.WorkStages.CatalogueStage.Models;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.WorkStages.CatalogueStage.Database;

// CREATE TABLE IF NOT EXISTS drom_vehicles_parser.working_parser_link_pagination
// (
//     id uuid primary key,
//     link_id uuid not null,
//     url text,
//     catalogue_items_fetched boolean not null,
//     retry_count integer not null,
//     CONSTRAINT link_fk FOREIGN KEY(link_id) REFERENCES drom_vehicles_parser.working_parser_links(id)
// );
public static class WorkingCataloguePageStorageImplementation
{
    public sealed record WorkingCataloguePageQuery(
        bool CatalogueItemsFetched = false,
        bool CatalogueItemsNotFetched = false,
        int? RetryCountThreshold = null,
        bool WithLock = false,
        int? Limit = null);
    
    extension(WorkingCataloguePageQuery query)
    {
        private (DynamicParameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.RetryCountThreshold.HasValue)
            {
                filters.Add("retry_count < @retryCountThreshold");
                parameters.Add("@retryCountThreshold", query.RetryCountThreshold.Value, DbType.Int32);
            }
            
            if (query.CatalogueItemsFetched) filters.Add("catalogue_items_fetched is true");
            if (query.CatalogueItemsNotFetched) filters.Add("catalogue_items_fetched is false");
            
            return filters.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filters));
        }

        private string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
        private string LimitClause() => query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
    }
    
    extension(IEnumerable<WorkingCataloguePage>)
    {
        public static async Task<IEnumerable<WorkingCataloguePage>> GetMany(
            NpgSqlSession session,
            WorkingCataloguePageQuery query,
            CancellationToken ct = default
            )
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string limitClause = query.LimitClause();
            string lockClause = query.LockClause();
            string sql = $"""
                          SELECT
                          id as id,
                          url as url,
                          catalogue_items_fetched as catalogue_items_fetched,
                          retry_count as retry_count
                          FROM drom_vehicles_parser.working_parser_link_pagination
                          {filterSql}
                          {lockClause}
                          {limitClause}
                          """;
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            return await session.ReadManyUsingReader(command, reader =>
            {
                return new WorkingCataloguePage
                (
                    Id: reader.GetProperty<Guid>("id"),
                    Url: reader.GetProperty<string>("url"),
                    CatalogueItemsFetched: reader.GetProperty<bool>("catalogue_items_fetched"),
                    RetryCount: reader.GetProperty<int>("retry_count")
                );
            }, ct);
        }
    }

    extension(IEnumerable<WorkingCataloguePage> pages)
    {
        public async Task UpdateMany(NpgSqlSession session)
        {
            const string sql = """
                               UPDATE drom_vehicles_parser.working_parser_link_pagination
                               SET catalogue_items_fetched = @catalogue_items_fetched,
                                   retry_count = @retry_count
                               WHERE id = @id
                               """;
            IEnumerable<object> parameters = pages.Select(p => p.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }
    }

    extension(WorkingCataloguePage page)
    {
        private object ExtractParameters() => new
        {
            id = page.Id,
            url = page.Url,
            catalogue_items_fetched = page.CatalogueItemsFetched,
            retry_count = page.RetryCount
        };
    }
}