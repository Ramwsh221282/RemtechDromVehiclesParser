using System.Data;
using Dapper;
using DromVehiclesParser.WorkStages.PaginationStage.Models;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.WorkStages.PaginationStage.Database;

public static class WorkingParserLinkPaginationImplementation
{
    public sealed record WorkingParserLinkPaginationQuery(
        bool CatalogueItemsFetched = false,
        bool CatalogueItemsNotFetched = false,
        int? RetryCountThreshold = null,
        bool WithLock = false
        );

    extension(WorkingParserLinkPaginationQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.RetryCountThreshold.HasValue)
            {
                filters.Add("retry_count <= @retryCount");
                parameters.Add("@retryCount", query.RetryCountThreshold.Value, DbType.Int32);
            }
            
            if (query.CatalogueItemsFetched) filters.Add("catalogue_items_fetched is TRUE");
            if (query.CatalogueItemsNotFetched) filters.Add("catalogue_items_fetched is FALSE");

            return filters.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filters));
        }

        private string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
    }
    
    extension(WorkingParserLinkPagination pagination)
    {
        private object ExtractParameters() => new
        {
            id = pagination.Id,
            link_id = pagination.LinkId,
            url = pagination.Url,
            catalogue_items_fetched = pagination.CatalogueItemsFetched,
            retry_count = pagination.RetryCount
        };
    }

    extension(IEnumerable<WorkingParserLinkPagination>)
    {
        public static async Task<IEnumerable<WorkingParserLinkPagination>> GetManyFromDb(
            NpgSqlSession session,
            WorkingParserLinkPaginationQuery query,
            CancellationToken ct = default
            )
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string lockClause = query.LockClause();
            string sql = $"""
                          SELECT
                          id as id,
                          link_id as link_id,
                          url as url,
                          catalogue_items_fetched as catalogue_items_fetched,
                          retry_count as retry_count
                          FROM drom_vehicles_parser.working_parser_link_pagination
                          {filterSql}
                          {lockClause}
                          """;
            
            List<WorkingParserLinkPagination> paginations = [];
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            IDataReader reader = await session.ExecuteReader(command, ct);
            while (reader.Read())
                paginations.Add(reader.ExtractModel());
            return paginations;
        }
    }

    extension(IDataReader reader)
    {
        public WorkingParserLinkPagination ExtractModel()
        {
            return new WorkingParserLinkPagination(
                Id: reader.GetGuid(reader.GetOrdinal("id")),
                LinkId: reader.GetGuid(reader.GetOrdinal("link_id")),
                Url: reader.GetString(reader.GetOrdinal("url")),
                CatalogueItemsFetched: reader.GetBoolean(reader.GetOrdinal("catalogue_items_fetched")),
                RetryCount: reader.GetInt32(reader.GetOrdinal("retry_count")));
        }
    }
    
    extension(IEnumerable<WorkingParserLinkPagination> paginations)
    {
        public async Task SaveMany(NpgSqlSession session)
        {
            const string sql = """
                               INSERT INTO drom_vehicles_parser.working_parser_link_pagination
                               (id, link_id, url, catalogue_items_fetched, retry_count)
                               VALUES
                               (@id, @link_id, @url, @catalogue_items_fetched, @retry_count)
                               """;
            IEnumerable<object> parameters = paginations.Select(p => p.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }
    }
}