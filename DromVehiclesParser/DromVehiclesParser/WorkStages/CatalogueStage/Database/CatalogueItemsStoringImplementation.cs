using System.Data;
using System.Text.Json;
using Dapper;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;
using DromVehiclesParser.Shared.NpgSql;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.WorkStages.CatalogueStage.Database;

public static class CatalogueItemsStoringImplementation
{
    public sealed record DromCatalogueItemQuery(
        bool ProcessedOnly = false,
        bool UnprocessedOnly = false,
        int? RetryCountThreshold = null, 
        bool WithLock = false,
        int? Limit = null);

    extension(IEnumerable<DromCatalogueItem>)
    {
        public static async Task<IEnumerable<DromCatalogueItem>> GetManyFromDb(
            NpgSqlSession session, 
            DromCatalogueItemQuery query, 
            CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filters) = query.WhereClause();
            string lockClause = query.LockClause();
            string limitClause = query.LimitClause();
            string sql = $"""
                          SELECT 
                              id as id, 
                              url as url, 
                              photos as photos, 
                              processed as processed, 
                              retry_count as retry_count
                          FROM drom_vehicles_parser.catalogue_items
                          {filters}
                          {lockClause}
                          {limitClause}
                          """;
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            return await session.ReadManyUsingReader(
                command,
                r =>
                {
                    return new DromCatalogueItem(
                        Id: r.GetProperty<string>("id"),
                        Url: r.GetProperty<string>("url"),
                        Photos: r.GetProperty<string>("photos").FromJsonToList(),
                        Processed: r.GetProperty<bool>("processed"),
                        RetryCount: r.GetProperty<int>("retry_count")
                    );
                },
                ct);
        }
    }

    extension(string photos)
    {
        private IReadOnlyList<string> FromJsonToList()
        {
            using JsonDocument document = JsonDocument.Parse(photos);
            int length = document.RootElement.GetArrayLength();
            List<string> result = new List<string>(length);
            foreach (JsonElement item in document.RootElement.EnumerateArray())
                result.Add(item.GetString()!);
            return result;
        }
    }
    
    extension(DromCatalogueItemQuery query)
    {
        private string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
        private string LimitClause() => query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
        
        private (DynamicParameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.RetryCountThreshold.HasValue)
            {
                filters.Add("retry_count < @retryCount");
                parameters.Add("@retryCount", query.RetryCountThreshold.Value, DbType.Int32);
            }
            
            if (query.ProcessedOnly) filters.Add("processed is TRUE");
            if (query.UnprocessedOnly) filters.Add("processed is FALSE");
            return filters.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filters));
        }
    }
    
    extension(IEnumerable<DromCatalogueItem> items)
    {
        public async Task SaveMany(NpgSqlSession session)
        {
            const string sql = """
                               INSERT INTO drom_vehicles_parser.catalogue_items
                               (id, url, photos, processed, retry_count)
                               VALUES
                               (@id, @url, @photos::jsonb, @processed, @retry_count)
                               ON CONFLICT (id) DO NOTHING 
                               """;
            IEnumerable<object> parameters = items.Select(ExtractParameters);
            await session.ExecuteBulk(sql, parameters);
        }

        public async Task UpdateMany(NpgSqlSession session)
        {
            const string sql = """
                               UPDATE drom_vehicles_parser.catalogue_items
                               SET processed = @processed, retry_count = @retry_count
                               WHERE id = @id;
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